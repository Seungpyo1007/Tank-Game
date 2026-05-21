using UnityEngine;
using UnityEngine.UI;

public class LeadIndicator : MonoBehaviour
{
    [SerializeField] private GunController gun;
    [SerializeField] private Gunner gunner;
    [SerializeField] private TankCameraRig cameraRig;
    [SerializeField] private LayerMask hitMask = ~(1 << 8);

    private RectTransform indicator;
    private Image dot;

    void Awake()
    {
        BuildIndicator();
        if (cameraRig == null && Camera.main != null) cameraRig = Camera.main.GetComponent<TankCameraRig>();
    }

    void LateUpdate()
    {
        if (indicator == null || gunner == null || gun == null || gun.Muzzle == null) return;
        bool zoomed = cameraRig != null && cameraRig.IsZoomed;
        indicator.gameObject.SetActive(zoomed);
        if (!zoomed) return;

        var shell = gunner.CurrentShell;
        if (shell == null) return;

        Vector3 pos = gun.Muzzle.position;
        Vector3 vel = gun.Muzzle.forward * shell.muzzleVelocity;
        float dt = 0.05f;
        Vector3 impact = pos + gun.Muzzle.forward * 500f;
        for (int i = 0; i < 60; i++)
        {
            Vector3 next = pos + vel * dt + 0.5f * Physics.gravity * dt * dt;
            Vector3 delta = next - pos;
            if (Physics.Raycast(pos, delta.normalized, out RaycastHit hit, delta.magnitude, hitMask, QueryTriggerInteraction.Ignore))
            {
                impact = hit.point;
                break;
            }
            vel += Physics.gravity * dt;
            pos = next;
        }

        var cam = cameraRig != null ? cameraRig.Cam : Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(impact);
        if (sp.z < 0f) { indicator.gameObject.SetActive(false); return; }

        var canvas = indicator.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var canvasRT = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, new Vector2(sp.x, sp.y), null, out Vector2 local);
        indicator.anchoredPosition = local;
    }

    void BuildIndicator()
    {
        var hud = HUDController.Instance;
        Transform parent = transform;
        if (hud != null)
        {
            var canvas = hud.GetComponentInChildren<Canvas>();
            if (canvas != null) parent = canvas.transform;
        }
        var go = new GameObject("LeadDot", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        indicator = go.GetComponent<RectTransform>();
        indicator.anchorMin = indicator.anchorMax = new Vector2(0.5f, 0.5f);
        indicator.pivot = new Vector2(0.5f, 0.5f);
        indicator.sizeDelta = new Vector2(18, 18);
        dot = go.AddComponent<Image>();
        dot.color = new Color(1f, 0.3f, 0.15f, 0.95f);
        dot.raycastTarget = false;
    }
}
