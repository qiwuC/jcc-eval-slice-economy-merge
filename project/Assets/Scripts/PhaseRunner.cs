using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// Play Mode 验收驱动：阶段1~3全部断言，结果追加写入 <项目根>/results/playmode_log.txt。
public class PhaseRunner : MonoBehaviour
{
    public static bool finished;
    public static int passedCount;
    public static int failedCount;
    public static string currentPhase = "";

    [SerializeField] public List<string> assertionResults = new List<string>();

    EconomyManager economy;
    ShopManager shop;
    BenchManager bench;
    EconomyUI ui;
    string logPath;

    void Start()
    {
        economy = FindObjectOfType<EconomyManager>();
        shop = FindObjectOfType<ShopManager>();
        bench = FindObjectOfType<BenchManager>();
        ui = FindObjectOfType<EconomyUI>();
        var rootDir = Path.GetDirectoryName(Application.dataPath);
        var resultsDir = Path.Combine(rootDir, "results");
        Directory.CreateDirectory(resultsDir);
        logPath = Path.Combine(resultsDir, "playmode_log.txt");
        currentPhase = "phase1";
        File.AppendAllText(logPath, $"=== PlayMode verification | {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
        StartCoroutine(RunAll());
    }

    IEnumerator RunAll()
    {
        yield return null;
        File.AppendAllText(logPath, "--- phase1 start ---\n");
        RunPhase1Checks();
        File.AppendAllText(logPath, $"--- phase1 done | cumulative passed={passedCount} failed={failedCount} ---\n");

        currentPhase = "phase2";
        yield return StartCoroutine(RunPhase2());
        File.AppendAllText(logPath, $"--- phase2 done | cumulative passed={passedCount} failed={failedCount} ---\n");

        currentPhase = "phase3";
        yield return StartCoroutine(RunPhase3());
        File.AppendAllText(logPath, $"--- phase3 done | cumulative passed={passedCount} failed={failedCount} ---\n");

        File.AppendAllText(logPath, $"=== all phases done | passed={passedCount} failed={failedCount} ===\n");
        finished = true;
    }

    void Check(string caseId, string name, bool pass, string detail)
    {
        if (pass) passedCount++; else failedCount++;
        var line = $"{(pass ? "PASS" : "FAIL")}  {caseId}  {name}  | {detail}";
        assertionResults.Add(line);
        File.AppendAllText(logPath, line + "\n");
    }

    void ResetEconomyDefaults()
    {
        economy.gold = 50;
        economy.winStreak = 0;
        economy.lossStreak = 0;
        economy.simulateWinThisRound = true;
        economy.interestThisRound = economy.InterestFor(50);
        shop.lastRefreshMessage = "";
        shop.lastRefreshMessageEn = "";
    }

    // ---------------- 阶段1：经济循环 A1~E1 ----------------

    void RunPhase1Checks()
    {
        ResetEconomyDefaults();

        // A1: 初始金币50，利息显示5
        Check("A1", "initial gold == 50", economy.gold == 50, $"gold={economy.gold}");
        Check("A1", "initial interestThisRound == 5", economy.interestThisRound == 5, $"interest={economy.interestThisRound}");

        // B1: gold=49 结算利息4；gold=50 结算利息5
        economy.gold = 49;
        economy.EndRound();
        Check("B1", "gold=49 EndRound interest == 4", economy.interestThisRound == 4, $"interest={economy.interestThisRound}");
        Check("B1", "gold=49 EndRound gold == 49+5+4", economy.gold == 58, $"gold={economy.gold}");
        economy.gold = 50;
        economy.EndRound();
        Check("B1", "gold=50 EndRound interest == 5", economy.interestThisRound == 5, $"interest={economy.interestThisRound}");

        // C1: gold=1 刷新拒绝且不扣费；gold=2 刷新扣费且槽位为5
        economy.gold = 1;
        bool refreshed = shop.TryRefreshShop();
        Check("C1", "gold=1 refresh rejected", refreshed == false, $"ret={refreshed}");
        Check("C1", "gold=1 no charge", economy.gold == 1, $"gold={economy.gold}");
        Check("C1", "insufficient-gold message contains 金币不足", shop.lastRefreshMessage.Contains("金币不足"), $"msg={shop.lastRefreshMessage}");
        economy.gold = 2;
        refreshed = shop.TryRefreshShop();
        Check("C1", "gold=2 refresh succeeds and charges", refreshed && economy.gold == 0, $"gold={economy.gold} ret={refreshed}");
        Check("C1", "shopSlots count == 5 after refresh", shop.shopSlots.Count == 5, $"count={shop.shopSlots.Count}");

        // D1: 连胜从0到2，结算金变化 = 5 + 利息 + 1
        economy.gold = 50;
        economy.winStreak = 0;
        economy.lossStreak = 0;
        economy.simulateWinThisRound = true;
        economy.EndRound();
        Check("D1", "winStreak becomes 1 after first win", economy.winStreak == 1, $"winStreak={economy.winStreak}");
        int goldAfterFirst = economy.gold;
        economy.EndRound();
        int delta = economy.gold - goldAfterFirst;
        int expected = 5 + economy.interestThisRound + 1;
        Check("D1", "second win EndRound delta == 5 + interest + 1", delta == expected && economy.winStreak == 2,
            $"delta={delta} expected={expected} interest={economy.interestThisRound} winStreak={economy.winStreak}");

        // E1: 连续结束回合5次，无 NaN、无负数
        bool stable = true;
        var e1Detail = new StringBuilder();
        for (int i = 0; i < 5; i++)
        {
            economy.simulateWinThisRound = i % 2 == 0;
            economy.EndRound();
            bool okGold = !float.IsNaN(economy.gold) && !float.IsInfinity(economy.gold) && economy.gold >= 0;
            bool okWin = economy.winStreak >= 0;
            bool okLoss = economy.lossStreak >= 0;
            bool okInterest = economy.interestThisRound >= 0 && economy.interestThisRound <= 5;
            if (!(okGold && okWin && okLoss && okInterest))
            {
                stable = false;
                e1Detail.Append($"[round{i + 1} gold={economy.gold} win={economy.winStreak} loss={economy.lossStreak} interest={economy.interestThisRound}] ");
            }
        }
        Check("E1", "5 consecutive EndRounds stay finite and non-negative", stable,
            stable ? $"final gold={economy.gold} win={economy.winStreak} loss={economy.lossStreak}" : e1Detail.ToString());
    }

    // ---------------- 阶段2：三连升星 A2~F2 ----------------

    IEnumerator RunPhase2()
    {
        // 状态重置到等价 Inspector 默认值
        ResetEconomyDefaults();
        bench.ResetBench();
        var meleeCfg = bench.GetConfig(ShopManager.PieceIdMelee);
        var rangerCfg = bench.GetConfig(ShopManager.PieceIdRanger);
        meleeCfg.cost = 1; meleeCfg.baseHp = 101; meleeCfg.baseAtk = 11;
        rangerCfg.cost = 2; rangerCfg.baseHp = 202; rangerCfg.baseAtk = 22;
        File.AppendAllText(logPath, "--- phase2 start ---\n");

        // A2: 连买3张同名1费 -> 1星=0、2星=1、gold=50-3
        shop.ForceSetSlot(0, ShopManager.PieceIdMelee);
        shop.ForceSetSlot(1, ShopManager.PieceIdMelee);
        shop.ForceSetSlot(2, ShopManager.PieceIdMelee);
        bool buy1 = bench.TryBuyShopSlot(0);
        bool buy2 = bench.TryBuyShopSlot(1);
        bool buy3 = bench.TryBuyShopSlot(2);
        Check("A2", "three 1-cost buys succeed", buy1 && buy2 && buy3, $"rets={buy1},{buy2},{buy3}");
        Check("A2", "melee 1-star count == 0 after immediate merge", bench.CountStars(ShopManager.PieceIdMelee, 1) == 0,
            $"ones={bench.CountStars(ShopManager.PieceIdMelee, 1)}");
        Check("A2", "melee 2-star count == 1", bench.CountStars(ShopManager.PieceIdMelee, 2) == 1,
            $"twos={bench.CountStars(ShopManager.PieceIdMelee, 2)}");
        Check("A2", "gold == 50 - 3*1 == 47", economy.gold == 47, $"gold={economy.gold}");
        Check("A2", "bench count == 1", bench.bench.Count == 1, $"bench={bench.bench.Count}");

        // B2: 2星属性 == floor(1星*1.8)，1星 hp=101 atk=11 -> hp=181 atk=19
        var two = bench.bench.Find(p => p.pieceId == ShopManager.PieceIdMelee && p.star == 2);
        Check("B2", "2-star baseHp == floor(101*1.8) == 181", two != null && two.baseHp == 181, $"hp={(two != null ? two.baseHp.ToString() : "null")}");
        Check("B2", "2-star baseAtk == floor(11*1.8) == 19", two != null && two.baseAtk == 19, $"atk={(two != null ? two.baseAtk.ToString() : "null")}");

        // C2: 第4张同名1星 -> 1星=1、2星=1，不误合成
        shop.ForceSetSlot(0, ShopManager.PieceIdMelee);
        bench.TryBuyShopSlot(0);
        Check("C2", "after 4th buy melee 1-star == 1", bench.CountStars(ShopManager.PieceIdMelee, 1) == 1,
            $"ones={bench.CountStars(ShopManager.PieceIdMelee, 1)}");
        Check("C2", "melee 2-star stays 1", bench.CountStars(ShopManager.PieceIdMelee, 2) == 1,
            $"twos={bench.CountStars(ShopManager.PieceIdMelee, 2)}");
        Check("C2", "gold == 47 - 1 == 46", economy.gold == 46, $"gold={economy.gold}");

        // D2: 买3张2费 -> 合成只影响 ranger，melee 不变
        shop.ForceSetSlot(1, ShopManager.PieceIdRanger);
        shop.ForceSetSlot(2, ShopManager.PieceIdRanger);
        shop.ForceSetSlot(3, ShopManager.PieceIdRanger);
        bench.TryBuyShopSlot(1);
        bench.TryBuyShopSlot(2);
        bench.TryBuyShopSlot(3);
        Check("D2", "ranger 2-star == 1", bench.CountStars(ShopManager.PieceIdRanger, 2) == 1,
            $"twos={bench.CountStars(ShopManager.PieceIdRanger, 2)}");
        Check("D2", "melee unchanged (ones=1 twos=1)", bench.CountStars(ShopManager.PieceIdMelee, 1) == 1 && bench.CountStars(ShopManager.PieceIdMelee, 2) == 1,
            $"ones={bench.CountStars(ShopManager.PieceIdMelee, 1)} twos={bench.CountStars(ShopManager.PieceIdMelee, 2)}");
        Check("D2", "gold == 46 - 3*2 == 40", economy.gold == 40, $"gold={economy.gold}");

        // E2: Inspector 式修改 ranger 1星 baseHp 202->200，重新合成 -> 新2星=360，旧2星=363 保持
        rangerCfg.baseHp = 200;
        shop.ForceSetSlot(1, ShopManager.PieceIdRanger);
        shop.ForceSetSlot(2, ShopManager.PieceIdRanger);
        shop.ForceSetSlot(3, ShopManager.PieceIdRanger);
        bench.TryBuyShopSlot(1);
        bench.TryBuyShopSlot(2);
        bench.TryBuyShopSlot(3);
        var rangerTwos = bench.bench.FindAll(p => p.pieceId == ShopManager.PieceIdRanger && p.star == 2);
        Check("E2", "new 2-star uses new config: floor(200*1.8) == 360", rangerTwos.Exists(p => p.baseHp == 360),
            $"hps={string.Join(",", rangerTwos.ConvertAll(p => p.baseHp).ToArray())}");
        Check("E2", "old 2-star keeps floor(202*1.8) == 363", rangerTwos.Exists(p => p.baseHp == 363),
            $"hps={string.Join(",", rangerTwos.ConvertAll(p => p.baseHp).ToArray())}");
        Check("E2", "ranger ones == 0 twos == 2", bench.CountStars(ShopManager.PieceIdRanger, 1) == 0 && bench.CountStars(ShopManager.PieceIdRanger, 2) == 2,
            $"ones={bench.CountStars(ShopManager.PieceIdRanger, 1)} twos={bench.CountStars(ShopManager.PieceIdRanger, 2)}");
        Check("E2", "gold == 40 - 3*2 == 34", economy.gold == 34, $"gold={economy.gold}");

        // F2: gold 不足 -> 拒绝购买，bench 与 gold 均不变
        economy.gold = 0;
        int benchCountBefore = bench.bench.Count;
        shop.ForceSetSlot(0, ShopManager.PieceIdMelee);
        bool bought = bench.TryBuyShopSlot(0);
        Check("F2", "buy rejected when gold insufficient", bought == false, $"ret={bought}");
        Check("F2", "bench count unchanged", bench.bench.Count == benchCountBefore, $"before={benchCountBefore} after={bench.bench.Count}");
        Check("F2", "gold unchanged (== 0)", economy.gold == 0, $"gold={economy.gold}");
        Check("F2", "insufficient purchase message contains 金币不足", bench.lastPurchaseMessage.Contains("金币不足"), $"msg={bench.lastPurchaseMessage}");
        yield break;
    }

    // ---------------- 阶段3：交叉回归 A3~D3 ----------------

    IEnumerator RunPhase3()
    {
        File.AppendAllText(logPath, "--- A3: re-run phase1 checks (cross regression) ---\n");
        int failsBeforeA3 = failedCount;
        RunPhase1Checks();
        int newFailures = failedCount - failsBeforeA3;
        Check("A3", "re-run of A1~E1 after merge logic: all still pass", newFailures == 0, $"newFailures={newFailures}");

        // ---- B3: 多轮 刷新→购买→合成→再购买，账本逐步核对 ----
        // 期望模型：每步维护 expectedGold/ones/twos，断言实际状态逐步吻合。
        File.AppendAllText(logPath, "--- B3: multi-round ledger consistency ---\n");
        ResetEconomyDefaults();
        bench.ResetBench();
        var meleeCfg = bench.GetConfig(ShopManager.PieceIdMelee);
        meleeCfg.cost = 1; meleeCfg.baseHp = 101; meleeCfg.baseAtk = 11;

        int expectedGold = 50;
        int expectedOnes = 0;
        int expectedTwos = 0;

        void AssertLedger(string step)
        {
            bool goldOk = economy.gold == expectedGold;
            bool onesOk = bench.CountStars(ShopManager.PieceIdMelee, 1) == expectedOnes;
            bool twosOk = bench.CountStars(ShopManager.PieceIdMelee, 2) == expectedTwos;
            bool countOk = bench.bench.Count == expectedOnes + expectedTwos;
            Check("B3", $"ledger after {step}", goldOk && onesOk && twosOk && countOk,
                $"gold={economy.gold}/{expectedGold} ones={bench.CountStars(ShopManager.PieceIdMelee, 1)}/{expectedOnes} twos={bench.CountStars(ShopManager.PieceIdMelee, 2)}/{expectedTwos} bench={bench.bench.Count}");
        }

        // 第1轮：刷新→购买
        shop.TryRefreshShop(); expectedGold -= shop.refreshCost;
        AssertLedger("round1 refresh");
        shop.ForceSetSlot(0, ShopManager.PieceIdMelee);
        bench.TryBuyShopSlot(0); expectedOnes += 1; expectedGold -= 1;
        AssertLedger("round1 buy");

        // 第2轮：EndRound→刷新→购买
        economy.simulateWinThisRound = true;
        economy.EndRound(); expectedGold += 5 + Mathf.Min(expectedGold / 10, 5) + 0; // winStreak 0->1 奖励0
        AssertLedger("round2 endround");
        shop.TryRefreshShop(); expectedGold -= shop.refreshCost;
        AssertLedger("round2 refresh");
        shop.ForceSetSlot(0, ShopManager.PieceIdMelee);
        bench.TryBuyShopSlot(0); expectedOnes += 1; expectedGold -= 1;
        AssertLedger("round2 buy");

        // 第3轮：刷新→购买（ones 达 3 触发合成 3->1）
        shop.TryRefreshShop(); expectedGold -= shop.refreshCost;
        AssertLedger("round3 refresh");
        shop.ForceSetSlot(0, ShopManager.PieceIdMelee);
        int mergesBefore = bench.mergeCount;
        bench.TryBuyShopSlot(0);
        expectedOnes += 1;
        expectedGold -= 1;
        if (expectedOnes >= 3) { expectedOnes -= 3; expectedTwos += 1; }
        Check("B3", "merge fired in round3", bench.mergeCount == mergesBefore + 1, $"merges={bench.mergeCount} before={mergesBefore}");
        AssertLedger("round3 buy(merge)");

        // 第4轮：合成后再购买
        shop.TryRefreshShop(); expectedGold -= shop.refreshCost;
        AssertLedger("round4 refresh");
        shop.ForceSetSlot(1, ShopManager.PieceIdMelee);
        bench.TryBuyShopSlot(1); expectedOnes += 1; expectedGold -= 1;
        AssertLedger("round4 buy-after-merge");
        economy.simulateWinThisRound = false;
        economy.EndRound(); expectedGold += 5 + Mathf.Min(expectedGold / 10, 5) + 0; // lossStreak 1 奖励0
        AssertLedger("round4 endround");
        Check("B3", "final gold equals ledger", economy.gold == expectedGold, $"gold={economy.gold} expected={expectedGold}");
        Check("B3", "bench within capacity", bench.bench.Count <= BenchManager.BenchSize, $"bench={bench.bench.Count}");

        // ---- C3: 反硬编码：改刷新费用与1星属性，结果联动 ----
        File.AppendAllText(logPath, "--- C3: anti-hardcode parameter linkage ---\n");
        ResetEconomyDefaults();
        bench.ResetBench();
        shop.refreshCost = 4;
        economy.gold = 4;
        bool refresh4 = shop.TryRefreshShop();
        Check("C3", "refreshCost=4: gold 4 refresh succeeds and charges 4", refresh4 && economy.gold == 0, $"gold={economy.gold} ret={refresh4}");
        economy.gold = 3;
        bool refreshReject = shop.TryRefreshShop();
        Check("C3", "refreshCost=4: gold 3 refresh rejected", refreshReject == false && economy.gold == 3, $"gold={economy.gold} ret={refreshReject}");
        Check("C3", "rejected message contains 金币不足", shop.lastRefreshMessage.Contains("金币不足"), $"msg={shop.lastRefreshMessage}");
        shop.refreshCost = 2;

        meleeCfg.baseHp = 300;
        economy.gold = 100;
        shop.ForceSetSlot(0, ShopManager.PieceIdMelee);
        shop.ForceSetSlot(1, ShopManager.PieceIdMelee);
        shop.ForceSetSlot(2, ShopManager.PieceIdMelee);
        bench.TryBuyShopSlot(0); bench.TryBuyShopSlot(1); bench.TryBuyShopSlot(2);
        var merged300 = bench.bench.Find(p => p.pieceId == ShopManager.PieceIdMelee && p.star == 2);
        Check("C3", "1-star baseHp=300 -> merged 2-star hp == floor(300*1.8) == 540", merged300 != null && merged300.baseHp == 540,
            $"hp={(merged300 != null ? merged300.baseHp.ToString() : "null")}");
        meleeCfg.baseHp = 101;

        // ---- D3: UI 显示与内部值一致 ----
        File.AppendAllText(logPath, "--- D3: UI vs internal state ---\n");
        bench.ResetBench();
        economy.gold = 77;
        economy.interestThisRound = 5;
        economy.winStreak = 3;
        economy.lossStreak = 1;
        bench.bench.Add(new ChessPiece { pieceId = ShopManager.PieceIdMelee, star = 2, baseHp = 181, baseAtk = 19 });
        shop.lastRefreshMessageEn = "ui-consistency-check";
        yield return null; // 等 EconomyUI.Update 刷新文本

        if (ui == null || ui.statusText == null)
        {
            Check("D3", "UI texts exist", false, "ui or statusText missing");
        }
        else
        {
            string status = ui.statusText.text;
            int uiGold = ParseInt(status, @"Gold:\s*(-?\d+)");
            int uiInterest = ParseInt(status, @"Interest:\s*(-?\d+)");
            int uiWin = ParseInt(status, @"WinStreak:\s*(-?\d+)");
            int uiLoss = ParseInt(status, @"LossStreak:\s*(-?\d+)");
            Check("D3", "UI gold matches internal", uiGold == economy.gold, $"ui={uiGold} internal={economy.gold} text={status}");
            Check("D3", "UI interest matches internal", uiInterest == economy.interestThisRound, $"ui={uiInterest} internal={economy.interestThisRound}");
            Check("D3", "UI winStreak matches internal", uiWin == economy.winStreak, $"ui={uiWin} internal={economy.winStreak}");
            Check("D3", "UI lossStreak matches internal", uiLoss == economy.lossStreak, $"ui={uiLoss} internal={economy.lossStreak}");
            int uiBench = ParseInt(ui.benchText.text, @"Bench:\s*(\d+)");
            int uiMerges = ParseInt(ui.benchText.text, @"Merges:\s*(\d+)");
            Check("D3", "UI bench count matches internal", uiBench == bench.bench.Count, $"ui={uiBench} internal={bench.bench.Count}");
            Check("D3", "UI merges matches internal", uiMerges == bench.mergeCount, $"ui={uiMerges} internal={bench.mergeCount}");
            Check("D3", "UI message matches internal", ui.messageText.text == shop.lastRefreshMessageEn,
                $"ui={ui.messageText.text} internal={shop.lastRefreshMessageEn}");
            int uiSlots = Regex.Matches(ui.shopText.text, @"\[\d+\]").Count;
            Check("D3", "UI shop slot count == 5", uiSlots == 5, $"uiSlots={uiSlots}");
        }
        ResetEconomyDefaults();
        bench.ResetBench();
    }

    static int ParseInt(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        return m.Success ? int.Parse(m.Groups[1].Value) : int.MinValue;
    }
}
