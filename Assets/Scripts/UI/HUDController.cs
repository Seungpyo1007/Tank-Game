using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Gunner gunner;
    [SerializeField] private Health playerHealth;
    [SerializeField] private TankCameraRig cameraRig;
    [SerializeField] private HullController playerHull;

    static readonly Color C_Frame = new Color(0.06f, 0.07f, 0.06f, 0.78f);
    static readonly Color C_Track = new Color(0.13f, 0.13f, 0.13f, 0.92f);
    static readonly Color C_Accent = new Color(1f, 0.78f, 0.22f, 1f);
    static readonly Color C_AccentDim = new Color(1f, 0.78f, 0.22f, 0.4f);
    static readonly Color C_Text = new Color(0.94f, 0.94f, 0.88f, 1f);
    static readonly Color C_Subtle = new Color(0.78f, 0.78f, 0.72f, 0.78f);
    static readonly Color C_Dim = new Color(1f, 1f, 1f, 0.28f);
    static readonly Color C_Hp_Hi = new Color(0.36f, 0.95f, 0.42f);
    static readonly Color C_Hp_Mid = new Color(0.98f, 0.78f, 0.22f);
    static readonly Color C_Hp_Low = new Color(0.96f, 0.28f, 0.18f);
    static readonly Color C_Danger = new Color(0.95f, 0.26f, 0.16f, 0.95f);
    static readonly Color C_Ready = new Color(0.33f, 0.95f, 0.42f, 0.95f);
    static readonly Color C_Scope = new Color(0.2f, 0.95f, 0.5f, 0.9f);

    private Font font;
    private Image reloadFill;
    private Image apBox; private Image heBox;
    private Text apLabel; private Text heLabel;
    private Text shellNameText;
    private Text reloadTimeText;
    private Image hpFill;
    private Text hpText;
    private GameObject destroyedBanner;
    private Text destroyedText;
    private Text destroyedStatsText;
    private Text victoryStatsText;
    private GameObject crosshair;
    private GameObject scopeReticle;
    private Text rangeText;
    private Text feedbackText;
    private Outline feedbackOutline;
    private float feedbackEndTime = -1f;
    private Color feedbackStartColor;
    private Image damageFlash;
    private float damageFlashEnd;
    private Text enemyCounterText;
    private Image enemyDot;
    private GameObject victoryBanner;
    private bool victoryShown;
    private Text speedText;
    private Image speedFill;
    private RectTransform canvasRT;
    private RectTransform crosshairRT;
    private Text scopeZoomText;
    private Text scopeRangeText;
    private GameObject scopeVignette;
    private RectTransform compassContent;
    private Text compassHeadingText;
    private GameObject hitIndicator;
    private RectTransform hitIndicatorRT;
    private Image hitIndicatorImage;
    private float hitIndicatorEnd;
    private Vector3 lastHitDirection;

    void Awake()
    {
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        if (cameraRig == null && Camera.main != null) cameraRig = Camera.main.GetComponent<TankCameraRig>();
        if (playerHull == null && playerHealth != null) playerHull = playerHealth.GetComponent<HullController>();
        BuildHUD();
    }

    void Start()
    {
        if (playerHealth != null) playerHealth.OnDamaged += OnPlayerDamaged;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (playerHealth != null) playerHealth.OnDamaged -= OnPlayerDamaged;
    }

    void OnPlayerDamaged(Health h, float amount, Vector3 hitPos)
    {
        damageFlashEnd = Time.time + 0.55f;
        if (playerHealth != null)
            lastHitDirection = (hitPos - playerHealth.transform.position).normalized;
        hitIndicatorEnd = Time.time + 1.5f;
    }

    void Update()
    {
        UpdateGunnerHUD();
        UpdatePlayerHP();
        UpdateScope();
        UpdateRange();
        UpdateFeedback();
        UpdateDamageFlash();
        UpdateEnemyCounter();
        UpdateSpeed();
        UpdateCrosshairPosition();
        UpdateCompass();
        UpdateHitIndicator();
    }

    void UpdateCompass()
    {
        if (compassContent == null || playerHealth == null) return;
        float yaw = playerHealth.transform.eulerAngles.y;
        float pixelsPerDeg = 8f;
        compassContent.anchoredPosition = new Vector2(-yaw * pixelsPerDeg, 0f);
        if (compassHeadingText != null)
            compassHeadingText.text = $"{Mathf.RoundToInt(yaw):000}°";
    }

    void UpdateHitIndicator()
    {
        if (hitIndicator == null || playerHealth == null) return;
        float remain = hitIndicatorEnd - Time.time;
        if (remain <= 0f)
        {
            if (hitIndicator.activeSelf) hitIndicator.SetActive(false);
            return;
        }
        hitIndicator.SetActive(true);

        float t = Mathf.Clamp01(remain / 1.5f);
        Color c = new Color(0.95f, 0.15f, 0.05f, t * 0.8f);
        if (hitIndicatorImage != null) hitIndicatorImage.color = c;

        var fwd = playerHealth.transform.forward;
        Vector3 flatDir = new Vector3(lastHitDirection.x, 0f, lastHitDirection.z).normalized;
        Vector3 flatFwd = new Vector3(fwd.x, 0f, fwd.z).normalized;
        float angle = Vector3.SignedAngle(flatFwd, flatDir, Vector3.up);
        if (hitIndicatorRT != null)
            hitIndicatorRT.localEulerAngles = new Vector3(0f, 0f, -angle);
    }

    void UpdateCrosshairPosition()
    {
        if (crosshair == null || canvasRT == null || crosshairRT == null) return;
        bool zoomed = cameraRig != null && cameraRig.IsZoomed;
        if (zoomed) return;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;
        Vector2 sp = mouse.position.ReadValue();
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sp, null, out local);
        crosshairRT.anchoredPosition = local;
    }

    void UpdateGunnerHUD()
    {
        if (gunner == null) return;
        bool ready = !gunner.IsReloading;
        float p = gunner.ReloadProgress01;

        if (reloadFill != null)
        {
            reloadFill.fillAmount = p;
            reloadFill.color = ready ? C_Ready : C_Danger;
        }
        if (reloadTimeText != null)
        {
            if (ready) { reloadTimeText.text = "READY"; reloadTimeText.color = C_Ready; }
            else { reloadTimeText.text = $"{gunner.ReloadRemaining:F1}s"; reloadTimeText.color = C_Text; }
        }
        var shell = gunner.CurrentShell;
        if (shellNameText != null && shell != null)
            shellNameText.text = shell.displayName.ToUpperInvariant();
        int idx = gunner.CurrentShellIndex;
        if (apBox != null) apBox.color = idx == 0 ? C_Accent : C_Dim;
        if (heBox != null) heBox.color = idx == 1 ? C_Accent : C_Dim;
        if (apLabel != null) apLabel.color = idx == 0 ? Color.black : C_Text;
        if (heLabel != null) heLabel.color = idx == 1 ? Color.black : C_Text;
    }

    void UpdatePlayerHP()
    {
        if (playerHealth == null) return;
        float r = playerHealth.HPRatio;
        if (hpFill != null)
        {
            hpFill.fillAmount = r;
            hpFill.color = r > 0.6f ? C_Hp_Hi : r > 0.3f ? C_Hp_Mid : C_Hp_Low;
        }
        if (hpText != null) hpText.text = $"{Mathf.CeilToInt(playerHealth.CurrentHP)} / {Mathf.CeilToInt(playerHealth.MaxHP)}";
        if (playerHealth.IsDead && destroyedBanner != null && !destroyedBanner.activeSelf)
        {
            destroyedBanner.SetActive(true);
            if (destroyedText != null) destroyedText.text = "YOU ARE DESTROYED";
            if (destroyedStatsText != null) destroyedStatsText.text = BuildStatsText();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0.3f;
        }
    }

    string BuildStatsText()
    {
        var s = GameStats.Instance;
        if (s == null) return "";
        int min = Mathf.FloorToInt(s.ElapsedTime / 60f);
        int sec = Mathf.FloorToInt(s.ElapsedTime % 60f);
        return $"KILLS         {s.kills}\n" +
               $"SHOTS         {s.shotsHit} / {s.shotsFired}\n" +
               $"ACCURACY      {s.Accuracy * 100f:F0}%\n" +
               $"PENETRATIONS  {s.penetrations}\n" +
               $"DAMAGE DEALT  {Mathf.CeilToInt(s.damageDealt)}\n" +
               $"DAMAGE TAKEN  {Mathf.CeilToInt(s.damageTaken)}\n" +
               $"TIME          {min:00}:{sec:00}";
    }

    void UpdateScope()
    {
        bool zoomed = cameraRig != null && cameraRig.IsZoomed;
        if (crosshair != null) crosshair.SetActive(!zoomed);
        if (scopeReticle != null) scopeReticle.SetActive(zoomed);
        if (scopeVignette != null) scopeVignette.SetActive(zoomed);
        if (zoomed && cameraRig != null && scopeZoomText != null)
            scopeZoomText.text = $"×{cameraRig.CurrentZoomRatio:F1}  ·  LEVEL {cameraRig.ScopeLevel + 1}/{cameraRig.ScopeLevelCount}";
    }

    void UpdateRange()
    {
        if (cameraRig == null || cameraRig.Cam == null) return;
        var cam = cameraRig.Cam;
        bool zoomed = cameraRig.IsZoomed;
        string rangeStr = "";
        bool hasHit = Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 4000f, ~(1 << 8), QueryTriggerInteraction.Ignore);
        if (hasHit) rangeStr = $"{Mathf.RoundToInt(hit.distance)} m";

        if (rangeText != null)
        {
            if (!zoomed && hasHit) { rangeText.text = rangeStr; rangeText.gameObject.SetActive(true); }
            else rangeText.gameObject.SetActive(false);
        }
        if (scopeRangeText != null)
        {
            if (zoomed && hasHit) { scopeRangeText.text = "RNG  " + rangeStr; scopeRangeText.gameObject.SetActive(true); }
            else scopeRangeText.gameObject.SetActive(false);
        }
    }

    void UpdateFeedback()
    {
        if (feedbackText == null) return;
        if (Time.time >= feedbackEndTime)
        {
            if (feedbackText.gameObject.activeSelf) feedbackText.gameObject.SetActive(false);
            return;
        }
        float remain = feedbackEndTime - Time.time;
        float fadeT = Mathf.Clamp01(remain / 0.6f);
        var c = feedbackStartColor; c.a = fadeT;
        feedbackText.color = c;
        if (feedbackOutline != null) { var oc = Color.black; oc.a = fadeT * 0.9f; feedbackOutline.effectColor = oc; }
    }

    void UpdateDamageFlash()
    {
        if (damageFlash == null) return;
        float remain = damageFlashEnd - Time.time;
        if (remain > 0f)
        {
            float t = Mathf.Clamp01(remain / 0.55f);
            damageFlash.color = new Color(0.95f, 0.1f, 0.05f, t * 0.55f);
        }
        else if (damageFlash.color.a > 0f)
        {
            damageFlash.color = new Color(0.95f, 0.1f, 0.05f, 0f);
        }
    }

    void UpdateEnemyCounter()
    {
        if (enemyCounterText == null) return;
        var ais = FindObjectsByType<EnemyTankAI>(FindObjectsSortMode.None);
        int total = ais.Length;
        int alive = 0;
        foreach (var ai in ais)
        {
            var h = ai.GetComponent<Health>();
            if (h != null && !h.IsDead) alive++;
        }
        enemyCounterText.text = $"ENEMIES   {alive} / {total}";
        enemyCounterText.color = alive == 0 ? C_Hp_Hi : C_Text;
        if (enemyDot != null) enemyDot.color = alive == 0 ? C_Hp_Hi : C_Danger;

        if (alive == 0 && total > 0 && !victoryShown && playerHealth != null && !playerHealth.IsDead)
        {
            victoryShown = true;
            if (victoryBanner != null) victoryBanner.SetActive(true);
            if (victoryStatsText != null) victoryStatsText.text = BuildStatsText();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void UpdateSpeed()
    {
        if (playerHull == null) return;
        float speedMs = Mathf.Abs(playerHull.ForwardSpeed);
        float kmh = speedMs * 3.6f;
        if (speedText != null) speedText.text = $"{Mathf.RoundToInt(kmh)} KM/H";
        if (speedFill != null) speedFill.fillAmount = Mathf.Clamp01(speedMs / 13f);
    }

    public void ShowHitFeedback(string text, Color color, float duration = 1.4f)
    {
        if (feedbackText == null) return;
        feedbackText.text = text;
        feedbackText.gameObject.SetActive(true);
        feedbackStartColor = color;
        feedbackText.color = color;
        feedbackEndTime = Time.time + duration;
    }

    void BuildHUD()
    {
        var canvasGO = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        canvasRT = canvasGO.GetComponent<RectTransform>();
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();

        BuildDamageFlash(canvasGO.transform);
        BuildCompass(canvasGO.transform);
        BuildGunnerPanel(canvasGO.transform);
        BuildHPPanel(canvasGO.transform);
        BuildSpeedPanel(canvasGO.transform);
        BuildCrosshair(canvasGO.transform);
        BuildHitIndicator(canvasGO.transform);
        BuildScopeReticle(canvasGO.transform);
        BuildRangeText(canvasGO.transform);
        BuildFeedback(canvasGO.transform);
        BuildEnemyCounter(canvasGO.transform);
        BuildDestroyedBanner(canvasGO.transform);
        BuildVictoryBanner(canvasGO.transform);
    }

    void BuildCompass(Transform parent)
    {
        var bar = NewRect("Compass", parent, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(560, 44), new Vector2(0, -22));
        bar.AddComponent<Image>().color = C_Frame;
        var accent = NewRect("CompassAccent", bar.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 2), Vector2.zero);
        accent.AddComponent<Image>().color = C_Accent;

        var mask = NewRect("Mask", bar.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-20, -10), Vector2.zero);
        mask.AddComponent<RectMask2D>();

        compassContent = NewRect("Content", mask.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(2880, 30), Vector2.zero).GetComponent<RectTransform>();

        string[] cardinals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        for (int i = 0; i < 360; i += 5)
        {
            bool major = (i % 30 == 0);
            float xPos = i * 8f - 1440f;
            AddBox(compassContent.transform, $"Tick_{i}", new Vector2(2, major ? 18 : 8), new Vector2(xPos, major ? -2 : -7), C_Text);
            if (i % 45 == 0)
            {
                var lbl = AddText(NewRect($"Lbl_{i}", compassContent.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40, 16), new Vector2(xPos, 8)), cardinals[i / 45], 14, FontStyle.Bold, TextAnchor.MiddleCenter);
                lbl.color = C_Accent;
            }
            else if (major)
            {
                var lbl = AddText(NewRect($"Deg_{i}", compassContent.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40, 14), new Vector2(xPos, 8)), $"{i}", 10, FontStyle.Normal, TextAnchor.MiddleCenter);
                lbl.color = C_Subtle;
            }
        }

        AddBox(bar.transform, "CenterMarker", new Vector2(2, 28), new Vector2(0, -2), C_Accent);
        compassHeadingText = AddText(NewRect("Heading", bar.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(60, 18), new Vector2(0, -10)), "000°", 12, FontStyle.Bold, TextAnchor.MiddleCenter);
        compassHeadingText.color = C_Accent;
    }

    void BuildHitIndicator(Transform parent)
    {
        hitIndicator = NewRect("HitIndicator", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 280), Vector2.zero);
        hitIndicatorRT = hitIndicator.GetComponent<RectTransform>();

        var arc = NewRect("Arc", hitIndicator.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(120, 14), new Vector2(0, 140));
        hitIndicatorImage = arc.AddComponent<Image>();
        hitIndicatorImage.color = new Color(0.95f, 0.15f, 0.05f, 0f);
        hitIndicatorImage.raycastTarget = false;

        var l = NewRect("ArcL", arc.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 14), new Vector2(-30, 0));
        var li = l.AddComponent<Image>(); li.color = new Color(0.95f, 0.15f, 0.05f, 0.7f); li.raycastTarget = false;
        var r = NewRect("ArcR", arc.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(30, 14), new Vector2(30, 0));
        var ri = r.AddComponent<Image>(); ri.color = new Color(0.95f, 0.15f, 0.05f, 0.7f); ri.raycastTarget = false;

        hitIndicator.SetActive(false);
    }

    void BuildDamageFlash(Transform parent)
    {
        var go = NewRect("DamageFlash", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        damageFlash = go.AddComponent<Image>();
        damageFlash.color = new Color(0.95f, 0.1f, 0.05f, 0f);
        damageFlash.raycastTarget = false;
    }

    void BuildPanel(GameObject panel, string headerText)
    {
        var img = panel.AddComponent<Image>();
        img.color = C_Frame;
        img.raycastTarget = false;

        var accent = NewRect("Accent", panel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 2), Vector2.zero);
        accent.AddComponent<Image>().color = C_Accent;

        if (!string.IsNullOrEmpty(headerText))
        {
            var hGO = NewRect("Header", panel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-24, 22), new Vector2(0, -8));
            var h = AddText(hGO, headerText, 13, FontStyle.Bold, TextAnchor.MiddleLeft);
            h.color = C_Accent;
        }
    }

    void BuildGunnerPanel(Transform parent)
    {
        var panel = NewRect("GunnerPanel", parent, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(340, 220), new Vector2(-26, 26));
        BuildPanel(panel, "GUN  ·  100mm");

        var shellGO = NewRect("ShellName", panel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-30, 44), new Vector2(0, -38));
        shellNameText = AddText(shellGO, "AP", 30, FontStyle.Bold, TextAnchor.MiddleLeft);

        var barTrack = NewRect("BarTrack", panel.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(22, -80), new Vector2(-16, -10));
        barTrack.AddComponent<Image>().color = C_Track;
        var fillGO = NewRect("BarFill", barTrack.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        reloadFill = fillGO.AddComponent<Image>();
        reloadFill.color = C_Danger;
        reloadFill.type = Image.Type.Filled;
        reloadFill.fillMethod = Image.FillMethod.Vertical;
        reloadFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        reloadFill.fillAmount = 0f;

        var timeGO = NewRect("ReloadTime", panel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-50, 42), new Vector2(-14, 18));
        reloadTimeText = AddText(timeGO, "0.0s", 32, FontStyle.Bold, TextAnchor.MiddleCenter);

        var apGO = NewRect("AP_Box", panel.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(58, 36), new Vector2(18, 80));
        apBox = apGO.AddComponent<Image>();
        apBox.color = C_Accent;
        apLabel = AddText(NewRect("Label", apGO.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "AP", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        apLabel.color = Color.black;

        var heGO = NewRect("HE_Box", panel.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(58, 36), new Vector2(82, 80));
        heBox = heGO.AddComponent<Image>();
        heBox.color = C_Dim;
        heLabel = AddText(NewRect("Label", heGO.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "HE", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        heLabel.color = C_Text;

        var hint = AddText(NewRect("Hint", panel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-50, 18), new Vector2(-14, 56)), "TAB  switch   ·   RMB  scope", 11, FontStyle.Normal, TextAnchor.MiddleCenter);
        hint.color = C_Subtle;
    }

    void BuildHPPanel(Transform parent)
    {
        var panel = NewRect("HPPanel", parent, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(340, 90), new Vector2(26, 26));
        BuildPanel(panel, "HULL  ·  T-55");

        var trackGO = NewRect("Track", panel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-24, 16), new Vector2(0, 14));
        trackGO.AddComponent<Image>().color = C_Track;

        var fillGO = NewRect("Fill", trackGO.transform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        hpFill = fillGO.AddComponent<Image>();
        hpFill.color = C_Hp_Hi;
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hpFill.fillAmount = 1f;

        hpText = AddText(NewRect("Value", panel.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(140, 22), new Vector2(-14, -8)), "1000 / 1000", 14, FontStyle.Bold, TextAnchor.MiddleRight);
        hpText.color = C_Text;
    }

    void BuildSpeedPanel(Transform parent)
    {
        var panel = NewRect("SpeedPanel", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(220, 84), new Vector2(26, -26));
        BuildPanel(panel, "SPEED");

        speedText = AddText(NewRect("Text", panel.transform, new Vector2(0, 0), new Vector2(1, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(-20, 0), new Vector2(0, 8)), "0 KM/H", 26, FontStyle.Bold, TextAnchor.MiddleCenter);

        var trackGO = NewRect("Track", panel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-24, 6), new Vector2(0, 8));
        trackGO.AddComponent<Image>().color = C_Track;
        var fillGO = NewRect("Fill", trackGO.transform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        speedFill = fillGO.AddComponent<Image>();
        speedFill.color = C_Accent;
        speedFill.type = Image.Type.Filled;
        speedFill.fillMethod = Image.FillMethod.Horizontal;
        speedFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        speedFill.fillAmount = 0f;
    }

    void BuildCrosshair(Transform parent)
    {
        crosshair = NewRect("Crosshair", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60, 60), Vector2.zero);
        crosshairRT = crosshair.GetComponent<RectTransform>();
        AddBox(crosshair.transform, "Up",    new Vector2(2, 12), new Vector2(0, 14), C_Accent);
        AddBox(crosshair.transform, "Down",  new Vector2(2, 12), new Vector2(0, -14), C_Accent);
        AddBox(crosshair.transform, "Left",  new Vector2(12, 2), new Vector2(-14, 0), C_Accent);
        AddBox(crosshair.transform, "Right", new Vector2(12, 2), new Vector2(14, 0), C_Accent);
        AddBox(crosshair.transform, "Dot",   new Vector2(3, 3), Vector2.zero, C_Accent);
    }

    void BuildScopeReticle(Transform parent)
    {
        BuildScopeVignette(parent);

        scopeReticle = NewRect("ScopeReticle", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000, 1000), Vector2.zero);
        scopeReticle.SetActive(false);

        AddBox(scopeReticle.transform, "VLineU", new Vector2(1, 220), new Vector2(0, 240), C_Scope);
        AddBox(scopeReticle.transform, "VLineD", new Vector2(1, 220), new Vector2(0, -240), C_Scope);
        AddBox(scopeReticle.transform, "HLineL", new Vector2(220, 1), new Vector2(-240, 0), C_Scope);
        AddBox(scopeReticle.transform, "HLineR", new Vector2(220, 1), new Vector2(240, 0), C_Scope);

        for (int i = 1; i <= 8; i++)
        {
            int yDown = -i * 35;
            float w = 14f + i * 2f;
            AddBox(scopeReticle.transform, $"D{i}", new Vector2(w, 1), new Vector2(0, yDown), C_Scope);
            if (i % 2 == 0)
            {
                var lbl = AddText(NewRect($"DLbl{i}", scopeReticle.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 18), new Vector2(w * 0.5f + 14, yDown)), $"{i * 100}", 11, FontStyle.Bold, TextAnchor.MiddleLeft);
                lbl.color = C_Scope;
            }
        }
        for (int i = 1; i <= 3; i++)
        {
            AddBox(scopeReticle.transform, $"U{i}", new Vector2(10f + i * 2f, 1), new Vector2(0, i * 35), C_Scope);
        }
        for (int i = 1; i <= 6; i++)
        {
            AddBox(scopeReticle.transform, $"L{i}", new Vector2(1, 10f + i * 2f), new Vector2(-i * 35, 0), C_Scope);
            AddBox(scopeReticle.transform, $"R{i}", new Vector2(1, 10f + i * 2f), new Vector2(i * 35, 0), C_Scope);
        }

        AddBox(scopeReticle.transform, "CenterChev_Up", new Vector2(1, 14), new Vector2(0, 8), C_Scope);
        AddBox(scopeReticle.transform, "Dot", new Vector2(3, 3), Vector2.zero, C_Scope);

        AddBox(scopeReticle.transform, "BoundTL", new Vector2(80, 2), new Vector2(-380, 380), C_Scope);
        AddBox(scopeReticle.transform, "BoundTL2", new Vector2(2, 80), new Vector2(-420, 340), C_Scope);
        AddBox(scopeReticle.transform, "BoundTR", new Vector2(80, 2), new Vector2(380, 380), C_Scope);
        AddBox(scopeReticle.transform, "BoundTR2", new Vector2(2, 80), new Vector2(420, 340), C_Scope);
        AddBox(scopeReticle.transform, "BoundBL", new Vector2(80, 2), new Vector2(-380, -380), C_Scope);
        AddBox(scopeReticle.transform, "BoundBL2", new Vector2(2, 80), new Vector2(-420, -340), C_Scope);
        AddBox(scopeReticle.transform, "BoundBR", new Vector2(80, 2), new Vector2(380, -380), C_Scope);
        AddBox(scopeReticle.transform, "BoundBR2", new Vector2(2, 80), new Vector2(420, -340), C_Scope);

        scopeZoomText = AddText(NewRect("Zoom", scopeReticle.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(360, 22), new Vector2(0, 70)), "×4.3  ·  LEVEL 2/5", 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        scopeZoomText.color = C_Scope;

        scopeRangeText = AddText(NewRect("Range", scopeReticle.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(220, 22), new Vector2(0, -70)), "RNG  0 m", 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        scopeRangeText.color = C_Scope;

        var ammoLabel = AddText(NewRect("Ammo", scopeReticle.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(180, 22), new Vector2(-420, -390)), "100mm GUN", 12, FontStyle.Bold, TextAnchor.MiddleLeft);
        ammoLabel.color = C_Scope;

        var hintLbl = AddText(NewRect("Hint", scopeReticle.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(220, 22), new Vector2(420, -390)), "SCROLL  ZOOM", 12, FontStyle.Bold, TextAnchor.MiddleRight);
        hintLbl.color = C_Scope;
    }

    void BuildScopeVignette(Transform parent)
    {
        scopeVignette = NewRect("ScopeVignette", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var bg = scopeVignette.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);
        bg.raycastTarget = false;

        AddBox(scopeVignette.transform, "Top", new Vector2(4000, 200), new Vector2(0, 540), new Color(0, 0, 0, 0.95f));
        AddBox(scopeVignette.transform, "Bottom", new Vector2(4000, 200), new Vector2(0, -540), new Color(0, 0, 0, 0.95f));
        AddBox(scopeVignette.transform, "Left", new Vector2(400, 4000), new Vector2(-1080, 0), new Color(0, 0, 0, 0.95f));
        AddBox(scopeVignette.transform, "Right", new Vector2(400, 4000), new Vector2(1080, 0), new Color(0, 0, 0, 0.95f));
        scopeVignette.SetActive(false);
    }

    void AddBox(Transform parent, string name, Vector2 size, Vector2 pos, Color color)
    {
        var go = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, pos);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    void BuildRangeText(Transform parent)
    {
        rangeText = AddText(NewRect("RangeText", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200, 26), new Vector2(110, -28)), "0 m", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        rangeText.color = C_Scope;
    }

    void BuildFeedback(Transform parent)
    {
        var go = NewRect("Feedback", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 60), new Vector2(0, 220));
        feedbackText = AddText(go, "", 40, FontStyle.Bold, TextAnchor.MiddleCenter);
        feedbackOutline = go.AddComponent<Outline>();
        feedbackOutline.effectColor = new Color(0, 0, 0, 0.85f);
        feedbackOutline.effectDistance = new Vector2(2, -2);
        feedbackText.gameObject.SetActive(false);
    }

    void BuildEnemyCounter(Transform parent)
    {
        var panel = NewRect("EnemyCounter", parent, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(280, 56), new Vector2(-26, -26));
        BuildPanel(panel, "ENEMY  CONTACTS");

        var dotGO = NewRect("Dot", panel.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0.5f), new Vector2(10, 10), new Vector2(16, 18));
        enemyDot = dotGO.AddComponent<Image>();
        enemyDot.color = C_Danger;

        enemyCounterText = AddText(NewRect("Text", panel.transform, new Vector2(0, 0), new Vector2(1, 0.65f), new Vector2(0.5f, 0.5f), new Vector2(-46, 0), new Vector2(8, 8)), "ENEMIES  3 / 3", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
    }

    void BuildDestroyedBanner(Transform parent)
    {
        destroyedBanner = NewRect("DestroyedBanner", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000, 620), Vector2.zero);
        destroyedBanner.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.06f, 0.94f);

        var topAccent = NewRect("Accent", destroyedBanner.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 5), Vector2.zero);
        topAccent.AddComponent<Image>().color = new Color(0.85f, 0.18f, 0.12f);

        destroyedBanner.SetActive(false);

        var titleBg = NewRect("TitleBg", destroyedBanner.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 120), new Vector2(0, -60));
        titleBg.AddComponent<Image>().color = new Color(0.7f, 0.1f, 0.07f, 0.9f);

        var titleGO = NewRect("Title", titleBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        destroyedText = AddText(titleGO, "YOU ARE DESTROYED", 56, FontStyle.Bold, TextAnchor.MiddleCenter);
        destroyedText.color = Color.white;
        var titleOutline = titleGO.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0, 0, 0, 0.6f);
        titleOutline.effectDistance = new Vector2(2, -2);

        var statsLabel = AddText(NewRect("StatsHeader", destroyedBanner.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-60, 28), new Vector2(0, -200)), "BATTLE STATISTICS", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        statsLabel.color = C_Accent;

        destroyedStatsText = AddText(NewRect("Stats", destroyedBanner.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-100, -260), new Vector2(0, 30)), "", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        destroyedStatsText.color = C_Text;

        BuildBigButton(destroyedBanner.transform, "PLAY AGAIN", () => { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
    }

    void BuildVictoryBanner(Transform parent)
    {
        victoryBanner = NewRect("VictoryBanner", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000, 620), Vector2.zero);
        victoryBanner.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.06f, 0.94f);
        var topAccent = NewRect("Accent", victoryBanner.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 5), Vector2.zero);
        topAccent.AddComponent<Image>().color = C_Accent;
        victoryBanner.SetActive(false);

        var titleBg = NewRect("TitleBg", victoryBanner.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 120), new Vector2(0, -60));
        titleBg.AddComponent<Image>().color = new Color(0.18f, 0.55f, 0.22f, 0.92f);

        var titleGO = NewRect("Title", titleBg.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var title = AddText(titleGO, "VICTORY", 72, FontStyle.Bold, TextAnchor.MiddleCenter);
        title.color = Color.white;
        var titleOutline = titleGO.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0, 0, 0, 0.55f);
        titleOutline.effectDistance = new Vector2(2, -2);

        var statsLabel = AddText(NewRect("StatsHeader", victoryBanner.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-60, 28), new Vector2(0, -200)), "BATTLE STATISTICS", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        statsLabel.color = C_Accent;

        victoryStatsText = AddText(NewRect("Stats", victoryBanner.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-100, -260), new Vector2(0, 30)), "", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        victoryStatsText.color = C_Text;

        BuildBigButton(victoryBanner.transform, "PLAY AGAIN", () => { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
    }

    void BuildBigButton(Transform parent, string label, System.Action onClick)
    {
        var btnGO = NewRect("Btn_" + label, parent, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(320, 68), new Vector2(0, 30));
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.12f, 0.12f, 0.1f, 0.96f);
        var btn = btnGO.AddComponent<Button>();
        var col = btn.colors;
        col.normalColor = new Color(0.12f, 0.12f, 0.1f, 0.96f);
        col.highlightedColor = new Color(0.4f, 0.32f, 0.12f, 1f);
        col.pressedColor = new Color(0.6f, 0.45f, 0.1f, 1f);
        col.selectedColor = col.highlightedColor;
        btn.colors = col;
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() => onClick?.Invoke());
        var accent = NewRect("BtnAccent", btnGO.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 2), Vector2.zero);
        accent.AddComponent<Image>().color = C_Accent;
        var lblGO = NewRect("Label", btnGO.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var lbl = AddText(lblGO, label, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
        lbl.color = Color.white;
    }

    void EnsureEventSystem()
    {
        var es = FindAnyObjectByType<EventSystem>();
        if (es != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
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
        t.color = C_Text;
        t.font = font;
        t.raycastTarget = false;
        return t;
    }
}
