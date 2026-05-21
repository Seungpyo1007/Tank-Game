using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class MenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string startSceneName = "MainScene";

    [Header("Style")]
    [SerializeField] private Color bgColor = new Color(0.07f, 0.08f, 0.07f, 1f);
    [SerializeField] private Color titleColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.18f, 0.18f, 0.16f, 0.95f);
    [SerializeField] private Color buttonHoverColor = new Color(0.35f, 0.28f, 0.12f, 1f);
    [SerializeField] private Color buttonTextColor = new Color(0.95f, 0.95f, 0.9f, 1f);

    private Font font;

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        EnsureEventSystem();
        BuildUI();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("Menu_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = NewRect("BG", canvasGO.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;

        var titleGO = NewRect("Title", canvasGO.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900, 140), new Vector2(0, -160));
        var title = AddText(titleGO, "T-55  BATTLEGROUNDS", 92, FontStyle.Bold, TextAnchor.MiddleCenter);
        title.color = titleColor;

        var subGO = NewRect("Subtitle", canvasGO.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900, 40), new Vector2(0, -250));
        var sub = AddText(subGO, "War Thunder style — Single player vs AI", 26, FontStyle.Normal, TextAnchor.MiddleCenter);
        sub.color = new Color(buttonTextColor.r, buttonTextColor.g, buttonTextColor.b, 0.65f);

        BuildButton(canvasGO.transform, "Start", new Vector2(0, -40), () =>
        {
            SceneManager.LoadScene(startSceneName);
        });

        BuildButton(canvasGO.transform, "Quit", new Vector2(0, -140), () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        var hintGO = NewRect("Hint", canvasGO.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(900, 24), new Vector2(0, 40));
        var hint = AddText(hintGO, "WASD: move    Mouse: aim    Left-click: fire    Tab: switch shell", 18, FontStyle.Normal, TextAnchor.MiddleCenter);
        hint.color = new Color(buttonTextColor.r, buttonTextColor.g, buttonTextColor.b, 0.55f);
    }

    void BuildButton(Transform parent, string label, Vector2 anchoredPos, System.Action onClick)
    {
        var go = NewRect("Btn_" + label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360, 80), anchoredPos);
        var img = go.AddComponent<Image>();
        img.color = buttonColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = new Color(0.5f, 0.4f, 0.12f, 1f);
        colors.selectedColor = buttonHoverColor;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.7f);
        btn.colors = colors;
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var labelGO = NewRect("Label", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var t = AddText(labelGO, label, 36, FontStyle.Bold, TextAnchor.MiddleCenter);
        t.color = buttonTextColor;
    }

    void EnsureEventSystem()
    {
        var existing = FindAnyObjectByType<EventSystem>();
        if (existing != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    GameObject NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    Text AddText(GameObject go, string content, int fontSize, FontStyle style, TextAnchor anchor)
    {
        var t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = anchor;
        t.color = buttonTextColor;
        t.font = font;
        t.raycastTarget = false;
        return t;
    }
}
