using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// 只读展示经济/商店状态；刷新按钮可用性随金币联动。
public class EconomyUI : MonoBehaviour
{
    public EconomyManager economy;
    public ShopManager shop;
    public BenchManager bench;
    public TMP_Text statusText;
    public TMP_Text shopText;
    public TMP_Text messageText;
    public TMP_Text benchText;
    public Button refreshButton;
    public Button endRoundButton;

    void Update()
    {
        if (economy == null || shop == null) return;
        if (statusText != null)
            statusText.text = $"Gold: {economy.gold} | Interest: {economy.interestThisRound} | WinStreak: {economy.winStreak} | LossStreak: {economy.lossStreak}";
        if (shopText != null)
        {
            var sb = new System.Text.StringBuilder("Shop: ");
            for (int i = 0; i < shop.shopSlots.Count; i++)
                sb.Append('[').Append(i + 1).Append("] ").Append(shop.shopSlots[i]).Append("  ");
            shopText.text = sb.ToString();
        }
        if (messageText != null)
            messageText.text = shop.lastRefreshMessageEn;
        if (benchText != null && bench != null)
            benchText.text = $"Bench: {bench.bench.Count}/{BenchManager.BenchSize} | Merges: {bench.mergeCount}";
        if (refreshButton != null)
            refreshButton.interactable = economy.gold >= shop.refreshCost;
    }
}
