using System.Collections.Generic;
using UnityEngine;

/// 商店：固定 5 槽位随机填充；本切片不实现卡池扣减。
public class ShopManager : MonoBehaviour
{
    public const string PieceIdMelee = "p1_melee";   // 1 费棋子
    public const string PieceIdRanger = "p2_ranger"; // 2 费棋子

    [SerializeField] public int refreshCost = 2;
    [SerializeField] public int shopSize = 5;
    [SerializeField] public List<string> shopSlots = new List<string>();
    [SerializeField] public string lastRefreshMessage = "";
    [SerializeField] public string lastRefreshMessageEn = "";

    EconomyManager economy;

    void Awake()
    {
        FillSlots();
    }

    public bool TryRefreshShop()
    {
        if (economy == null) economy = GetComponent<EconomyManager>();
        if (economy.gold < refreshCost)
        {
            lastRefreshMessage = $"金币不足：刷新需要 {refreshCost} 金币，当前 {economy.gold} 金币";
            lastRefreshMessageEn = $"Not enough gold: refresh costs {refreshCost}, have {economy.gold}";
            return false;
        }
        economy.TrySpend(refreshCost);
        FillSlots();
        lastRefreshMessage = $"刷新成功，消耗 {refreshCost} 金币";
        lastRefreshMessageEn = $"Shop refreshed for {refreshCost} gold";
        return true;
    }

    /// 随机填充槽位；不查询也不扣减卡池。
    public void FillSlots()
    {
        shopSlots.Clear();
        for (int i = 0; i < shopSize; i++)
            shopSlots.Add(Random.Range(0, 2) == 0 ? PieceIdMelee : PieceIdRanger);
    }

    /// 仅供测试装配：强制设置某个槽位的棋子，绕过随机性。
    public void ForceSetSlot(int index, string pieceId)
    {
        while (shopSlots.Count < shopSize) shopSlots.Add("");
        if (index >= 0 && index < shopSize) shopSlots[index] = pieceId;
    }
}
