using UnityEngine;

public class GunController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurretController turret;
    [SerializeField] private Transform muzzle;

    [Header("Pitch")]
    [SerializeField] private float pitchSpeedDeg = 22f;
    [SerializeField] private float maxElevation = 18f;
    [SerializeField] private float maxDepression = 5f;

    public float CurrentPitchDeg { get; private set; }
    public Transform Muzzle => muzzle;

    void LateUpdate()
    {
        if (turret == null || transform.parent == null) return;

        Vector3 toTarget = turret.AimPoint - transform.position;
        if (toTarget.sqrMagnitude < 0.01f) return;

        Vector3 local = transform.parent.InverseTransformDirection(toTarget.normalized);
        float horiz = Mathf.Sqrt(local.x * local.x + local.z * local.z);
        float desiredPitch = -Mathf.Atan2(local.y, horiz) * Mathf.Rad2Deg;
        desiredPitch = Mathf.Clamp(desiredPitch, -maxElevation, maxDepression);

        CurrentPitchDeg = Mathf.MoveTowardsAngle(CurrentPitchDeg, desiredPitch, pitchSpeedDeg * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(CurrentPitchDeg, 0f, 0f);
    }
}
