using System;
using System.Collections.Generic;
using UnityEngine;

/// 备战席：8 槽；购买扣费入座；同名 1 星满 3 立即合成 1 张 2 星。
public class BenchManager : MonoBehaviour
{
    public const int BenchSize = 8;

    [SerializeField] public List<PieceConfig> pieceConfigs = new List<PieceConfig>
    {
        new PieceConfig { pieceId = "p1_melee",  cost = 1, baseHp = 101, baseAtk = 11 },
        new PieceConfig { pieceId = "p2_ranger", cost = 2, baseHp = 202, baseAtk = 22 },
    };

    [SerializeField] public List<ChessPiece> bench = new List<ChessPiece>();
    [SerializeField] public string lastPurchaseMessage = "";
    [SerializeField] public string lastPurchaseMessageEn = "";
    [SerializeField] public int mergeCount;

    public EconomyManager economy;
    public ShopManager shop;

    public PieceConfig GetConfig(string pieceId)
    {
        return pieceConfigs.Find(c => c.pieceId == pieceId);
    }

    public int CountStars(string pieceId, int star)
    {
        return bench.FindAll(p => p.pieceId == pieceId && p.star == star).Count;
    }

    public bool TryBuyShopSlot(int slotIndex)
    {
        if (economy == null) economy = GetComponent<EconomyManager>();
        if (shop == null) shop = GetComponent<ShopManager>();

        if (slotIndex < 0 || slotIndex >= shop.shopSlots.Count)
        {
            lastPurchaseMessage = "无效槽位";
            lastPurchaseMessageEn = "Invalid shop slot";
            return false;
        }
        string pieceId = shop.shopSlots[slotIndex];
        var config = GetConfig(pieceId);
        if (config == null)
        {
            lastPurchaseMessage = $"槽位为空或未知棋子：{pieceId}";
            lastPurchaseMessageEn = "Slot is empty or unknown piece";
            return false;
        }
        if (economy.gold < config.cost)
        {
            lastPurchaseMessage = $"金币不足：购买 {pieceId} 需要 {config.cost} 金币，当前 {economy.gold} 金币";
            lastPurchaseMessageEn = $"Not enough gold: {pieceId} costs {config.cost}, have {economy.gold}";
            return false;
        }
        if (bench.Count >= BenchSize)
        {
            lastPurchaseMessage = "备战席已满";
            lastPurchaseMessageEn = "Bench is full";
            return false;
        }

        economy.TrySpend(config.cost);
        bench.Add(new ChessPiece { pieceId = config.pieceId, star = 1, baseHp = config.baseHp, baseAtk = config.baseAtk });
        shop.shopSlots[slotIndex] = "";
        lastPurchaseMessage = $"购买成功 {pieceId}（1星）";
        lastPurchaseMessageEn = $"Bought {pieceId} (1-star)";

        TryMerge(pieceId);
        return true;
    }

    /// 同名 1 星达 3 立即合成：移除 3 张 1 星，2 星放入第一张的位置。
    /// 用 double 计算 floor(x*1.8)，避免 float epsilon（如 200*1.8f 落到 359.99..）。
    public bool TryMerge(string pieceId)
    {
        var ones = bench.FindAll(p => p.pieceId == pieceId && p.star == 1);
        if (ones.Count < 3) return false;

        int insertIndex = bench.IndexOf(ones[0]);
        bench.Remove(ones[0]);
        bench.Remove(ones[1]);
        bench.Remove(ones[2]);

        var config = GetConfig(pieceId);
        var merged = new ChessPiece
        {
            pieceId = pieceId,
            star = 2,
            baseHp = (int)Math.Floor(config.baseHp * 1.8),
            baseAtk = (int)Math.Floor(config.baseAtk * 1.8),
        };
        insertIndex = Mathf.Clamp(insertIndex, 0, bench.Count);
        bench.Insert(insertIndex, merged);
        mergeCount++;
        return true;
    }

    /// 测试辅助：清空并重置（验收之间隔离状态）。
    public void ResetBench()
    {
        bench.Clear();
        mergeCount = 0;
        lastPurchaseMessage = "";
        lastPurchaseMessageEn = "";
    }
}
