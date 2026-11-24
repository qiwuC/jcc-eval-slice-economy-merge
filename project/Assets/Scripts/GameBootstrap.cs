using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// 单场景原型装配：Play Mode 一启动就创建 GameRoot 与 UI，无需手工搭场景。
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        var root = GameObject.Find("GameRoot");
        if (root == null) root = new GameObject("GameRoot");
        var economy = root.GetComponent<EconomyManager>();
        if (economy == null) economy = root.AddComponent<EconomyManager>();
        var shop = root.GetComponent<ShopManager>();
        if (shop == null) shop = root.AddComponent<ShopManager>();
        var bench = root.GetComponent<BenchManager>();
        if (bench == null) bench = root.AddComponent<BenchManager>();
        bench.economy = economy;
        bench.shop = shop;

        var uiGo = GameObject.Find("EvalUI");
        if (uiGo == null) uiGo = BuildUi();
        var ui = uiGo.GetComponent<EconomyUI>();
        ui.economy = economy;
        ui.shop = shop;
        ui.bench = bench;

        ui.refreshButton.onClick.RemoveAllListeners();
        ui.refreshButton.onClick.AddListener(() => shop.TryRefreshShop());
        ui.endRoundButton.onClick.RemoveAllListeners();
        ui.endRoundButton.onClick.AddListener(() => economy.EndRound());
    }

    GameObject BuildUi()
    {
        var canvasGo = new GameObject("EvalUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
        if (GameObject.Find("EventSystem") == null)
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.StandaloneInputModule));

        var panel = new GameObject("Panel", typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(canvasGo.transform, false);
        var panelImage = panel.GetComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);
        panelImage.raycastTarget = false;
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(0f, 190f);
        panelRect.anchoredPosition = new Vector2(0f, 0f);

        var status = CreateText(panel.transform, "StatusText", 0f, -46f, 620f, 36f, 26);
        var shopLabel = CreateText(panel.transform, "ShopText", 0f, -86f, 900f, 32f, 22);
        var message = CreateText(panel.transform, "MessageText", 0f, -122f, 900f, 30f, 20);
        message.color = new Color(1f, 0.85f, 0.3f);
        var benchLabel = CreateText(panel.transform, "BenchText", 460f, -46f, 500f, 36f, 26);

        var refreshBtn = CreateButton(panel.transform, "RefreshButton", "Refresh Shop (2)", -260f, -160f, 200f, 44f);
        var endBtn = CreateButton(panel.transform, "EndRoundButton", "End Round", -480f, -160f, 160f, 44f);

        var ui = canvasGo.AddComponent<EconomyUI>();
        ui.statusText = status;
        ui.shopText = shopLabel;
        ui.messageText = message;
        ui.benchText = benchLabel;
        ui.refreshButton = refreshBtn;
        ui.endRoundButton = endBtn;
        return canvasGo;
    }

    TMP_Text CreateText(Transform parent, string name, float x, float y, float w, float h, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x, y);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        return text;
    }

    Button CreateButton(Transform parent, string name, string label, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x, y);

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return go.GetComponent<Button>();
    }
}
