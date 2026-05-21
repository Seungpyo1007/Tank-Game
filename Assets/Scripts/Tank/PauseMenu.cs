using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class PauseMenu : MonoBehaviour
{
    private Canvas canvas;
    private GameObject panel;
    private bool paused;
    private Font font;

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        Build();
        Show(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle() { Show(!paused); }

    public void Show(bool on)
    {
        paused = on;
        if (panel != null && panel.transform.parent != null)
            panel.transform.parent.gameObject.SetActive(on);
        Time.timeScale = on ? 0f : 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Build()
    {
        var canvasGO = new GameObject("Pause_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();

        panel = NewRect("Panel_Root", canvasGO.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var bgImg = panel.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.75f);

        var panelBox = NewRect("Panel", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520, 480), Vector2.zero);
        panelBox.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.06f, 0.95f);
        var panelTransform = panel.transform;
        panel = panelBox;
        var accent = NewRect("Accent", panel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 3), Vector2.zero);
        accent.AddComponent<Image>().color = new Color(1f, 0.78f, 0.22f);

        var title = AddText(NewRect("Title", panel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 60), new Vector2(0, -30)), "PAUSED", 44, FontStyle.Bold, TextAnchor.MiddleCenter);
        title.color = new Color(1f, 0.78f, 0.22f);

        BuildBtn("RESUME", -50, () => Show(false));
        BuildBtn("RESTART", 30, () => { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
        BuildBtn("MAIN MENU", 110, () => { Time.timeScale = 1f; SceneManager.LoadScene(0); });
        BuildBtn("QUIT", 190, () => {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    void BuildBtn(string label, float yOffsetFromTop, System.Action onClick)
    {
        var go = NewRect("Btn_" + label, panel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(380, 60), new Vector2(0, -150 - yOffsetFromTop));
        var img = go.AddComponent<Image>();
        img.color = new Color(0.14f, 0.14f, 0.13f, 0.95f);
        var btn = go.AddComponent<Button>();
        var col = btn.colors;
        col.normalColor = new Color(0.14f, 0.14f, 0.13f, 0.95f);
        col.highlightedColor = new Color(0.35f, 0.28f, 0.12f, 1f);
        col.pressedColor = new Color(0.55f, 0.42f, 0.12f, 1f);
        col.selectedColor = col.highlightedColor;
        btn.colors = col;
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
        var lbl = AddText(NewRect("Label", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), label, 26, FontStyle.Bold, TextAnchor.MiddleCenter);
        lbl.color = Color.white;
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    GameObject NewRect(string n, Transform p, Vector2 amin, Vector2 amax, Vector2 piv, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(p, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax; rt.pivot = piv; rt.sizeDelta = size; rt.anchoredPosition = pos;
        return go;
    }

    Text AddText(GameObject go, string s, int sz, FontStyle style, TextAnchor a)
    {
        var t = go.AddComponent<Text>();
        t.text = s; t.fontSize = sz; t.fontStyle = style; t.alignment = a; t.font = font; t.color = Color.white; t.raycastTarget = false;
        return t;
    }
}
