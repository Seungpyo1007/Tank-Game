using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 3.6f, 0);
    [SerializeField] private float width = 2.5f;
    [SerializeField] private float height = 0.32f;
    [SerializeField] private float maxVisibleRange = 200f;

    private Canvas canvas;
    private Image fill;
    private Image bg;
    private Text label;

    void Start()
    {
        BuildBar();
    }

    void LateUpdate()
    {
        if (canvas == null) return;
        if (health == null || health.IsDead)
        {
            canvas.gameObject.SetActive(false);
            return;
        }

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 pos = transform.position + worldOffset;
        float dist = Vector3.Distance(cam.transform.position, pos);
        if (dist > maxVisibleRange)
        {
            canvas.gameObject.SetActive(false);
            return;
        }
        canvas.gameObject.SetActive(true);

        canvas.transform.position = pos;
        canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cam.transform.position, Vector3.up);

        float scale = Mathf.Lerp(0.012f, 0.030f, Mathf.Clamp01(dist / 100f));
        canvas.transform.localScale = Vector3.one * scale;

        float r = health.HPRatio;
        if (fill != null)
        {
            fill.fillAmount = r;
            fill.color = r > 0.6f ? new Color(0.35f, 0.95f, 0.4f) : r > 0.3f ? new Color(0.95f, 0.75f, 0.2f) : new Color(0.95f, 0.25f, 0.15f);
        }
        if (label != null) label.text = $"{Mathf.CeilToInt(health.CurrentHP)}";
    }

    void BuildBar()
    {
        var canvasGO = new GameObject("HPBar_Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width * 100f, height * 100f);
        canvasGO.transform.localScale = Vector3.one * 0.02f;

        var bgGO = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        bg = bgGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        bg.raycastTarget = false;

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(canvasGO.transform, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0.03f, 0.18f);
        fillRT.anchorMax = new Vector2(0.97f, 0.82f);
        fillRT.sizeDelta = Vector2.zero;
        fill = fillGO.GetComponent<Image>();
        fill.color = new Color(0.95f, 0.25f, 0.15f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.raycastTarget = false;

        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(canvasGO.transform, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.sizeDelta = Vector2.zero;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        label = lblGO.AddComponent<Text>();
        label.font = font;
        label.text = "1000";
        label.color = Color.white;
        label.fontSize = 20;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
    }
}
