using UnityEngine;
using UnityEngine.InputSystem;

public class TurretController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera aimCamera;

    [Header("Aim")]
    [SerializeField] private float turnSpeedDeg = 30f;
    [SerializeField] private LayerMask aimMask = ~0;
    [SerializeField] private float maxAimDistance = 2000f;
    [SerializeField] private float fallbackAimDistance = 400f;

    [Header("Control")]
    [SerializeField] private bool aiControlled = false;

    public Vector3 AimPoint { get; private set; }
    public Vector3 ExternalAimPoint { get; set; }
    public bool AiControlled { get => aiControlled; set => aiControlled = value; }

    void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        AimPoint = aiControlled ? ExternalAimPoint : ComputeAimPointFromMouse();

        Vector3 toTarget = AimPoint - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.001f) return;

        Quaternion desired = Quaternion.LookRotation(toTarget, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeedDeg * Time.deltaTime);
    }

    Vector3 ComputeAimPointFromMouse()
    {
        if (aimCamera == null) return transform.position + transform.forward * fallbackAimDistance;
        var mouse = Mouse.current;
        Vector2 sp = mouse != null ? mouse.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray ray = aimCamera.ScreenPointToRay(sp);
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * fallbackAimDistance;
    }
}
