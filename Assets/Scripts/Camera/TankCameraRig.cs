using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class TankCameraRig : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private HullController hull;

    [Header("Orbit")]
    [SerializeField] private float distance = 9f;
    [SerializeField] private float height = 3.2f;
    [SerializeField] private float lookAheadHeight = 1.5f;
    [SerializeField] private float pitch = 14f;

    [Header("Trailing")]
    [SerializeField] private float yawFollow = 2.5f;
    [SerializeField] private float positionFollow = 6f;
    [SerializeField] private float rotationFollow = 10f;

    [Header("FOV / Zoom")]
    [SerializeField] private float baseFov = 60f;
    [SerializeField] private float fovBoost = 10f;
    [SerializeField] private float fovFollow = 3f;
    [SerializeField] private float referenceSpeed = 13f;
    [SerializeField] private float[] scopeFovSteps = new[] { 28f, 14f, 7f, 4f, 2.5f };
    [SerializeField] private int scopeLevel = 1;
    [SerializeField] private float zoomFollow = 12f;

    [Header("Zoom Camera Offset")]
    [SerializeField] private float zoomDistance = 5f;
    [SerializeField] private float zoomHeight = 2.4f;
    [SerializeField] private float zoomPitch = 6f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionPadding = 0.4f;

    private Camera cam;
    private float currentYaw;
    public bool IsZoomed { get; private set; }
    public Camera Cam => cam;
    public int ScopeLevel => scopeLevel;
    public int ScopeLevelCount => scopeFovSteps != null ? scopeFovSteps.Length : 1;
    public float CurrentZoomRatio => (scopeFovSteps != null && IsZoomed && scopeLevel < scopeFovSteps.Length) ? (baseFov / scopeFovSteps[scopeLevel]) : 1f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (target != null) currentYaw = target.eulerAngles.y;
    }

    void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        var mouse = Mouse.current;
        IsZoomed = mouse != null && mouse.rightButton.isPressed;
        if (IsZoomed && mouse != null && scopeFovSteps != null && scopeFovSteps.Length > 0)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.5f) scopeLevel = Mathf.Min(scopeLevel + 1, scopeFovSteps.Length - 1);
            else if (scroll < -0.5f) scopeLevel = Mathf.Max(scopeLevel - 1, 0);
        }

        float dt = Time.deltaTime;
        float targetYaw = target.eulerAngles.y;
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, 1f - Mathf.Exp(-yawFollow * dt));

        float useDistance = IsZoomed ? zoomDistance : distance;
        float useHeight = IsZoomed ? zoomHeight : height;
        float usePitch = IsZoomed ? zoomPitch : pitch;

        Vector3 focus = target.position + Vector3.up * lookAheadHeight;
        Quaternion orbit = Quaternion.Euler(usePitch, currentYaw, 0f);
        Vector3 backOffset = orbit * Vector3.back * useDistance + Vector3.up * useHeight;
        Vector3 desiredPos = focus + backOffset - Vector3.up * lookAheadHeight;

        Vector3 fromFocus = desiredPos - focus;
        float dist = fromFocus.magnitude;
        if (dist > 0.001f && Physics.SphereCast(focus, collisionPadding, fromFocus.normalized, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredPos = focus + fromFocus.normalized * Mathf.Max(0.5f, hit.distance - collisionPadding);
        }

        float posSpeed = IsZoomed ? positionFollow * 2.5f : positionFollow;
        float rotSpeed = IsZoomed ? rotationFollow * 2f : rotationFollow;
        transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-posSpeed * dt));
        Quaternion desiredRot = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-rotSpeed * dt));

        if (cam != null)
        {
            float speedFactor = hull != null ? Mathf.Clamp01(Mathf.Abs(hull.ForwardSpeed) / Mathf.Max(0.01f, referenceSpeed)) : 0f;
            float scopeFov = (scopeFovSteps != null && scopeFovSteps.Length > 0)
                ? scopeFovSteps[Mathf.Clamp(scopeLevel, 0, scopeFovSteps.Length - 1)]
                : 14f;
            float targetFov = IsZoomed ? scopeFov : baseFov + fovBoost * speedFactor;
            float fovSpeed = IsZoomed ? zoomFollow : fovFollow;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-fovSpeed * dt));
        }
    }
}
