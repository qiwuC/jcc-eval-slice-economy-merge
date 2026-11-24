using System;
using UnityEngine;

/// 回合经济结算：基础收入 -> 利息 -> 连胜/连败奖励。
/// 利息按结算前的金币计算（金铲铲规则：回合开始时的存款决定利息）。
public class EconomyManager : MonoBehaviour
{
    [SerializeField] public int gold = 50;
    [SerializeField] public int baseIncome = 5;
    [SerializeField] public int interestCap = 5;
    [SerializeField] public int interestThisRound;
    [SerializeField] public int winStreak;
    [SerializeField] public int lossStreak;
    [SerializeField] public bool simulateWinThisRound = true;

    public event Action EconomyChanged;

    void Awake()
    {
        interestThisRound = InterestFor(gold);
    }

    public int InterestFor(int currentGold)
    {
        return Mathf.Min(Mathf.FloorToInt(currentGold / 10f), interestCap);
    }

    public int StreakBonusFor(int streak)
    {
        if (streak >= 5) return 3;
        if (streak == 4) return 2;
        if (streak >= 2) return 1;
        return 0;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || gold < amount) return false;
        gold -= amount;
        EconomyChanged?.Invoke();
        return true;
    }

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        EconomyChanged?.Invoke();
    }

    /// 结算一回合，返回本回合利息（供验收/展示）。
    public int EndRound()
    {
        int interest = InterestFor(gold);

        if (simulateWinThisRound)
        {
            winStreak += 1;
            lossStreak = 0;
        }
        else
        {
            lossStreak += 1;
            winStreak = 0;
        }
        int streakBonus = StreakBonusFor(simulateWinThisRound ? winStreak : lossStreak);

        AddGold(baseIncome);
        interestThisRound = interest;
        AddGold(interest);
        AddGold(streakBonus);
        return interest;
    }
}
