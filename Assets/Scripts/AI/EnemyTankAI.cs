using UnityEngine;

public class EnemyTankAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private TurretController turret;
    [SerializeField] private GunController gun;
    [SerializeField] private Gunner gunner;
    [SerializeField] private Rigidbody hullRb;
    [SerializeField] private Health selfHealth;
    [SerializeField] private HullController hull;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 80f;
    [SerializeField] private float losRange = 120f;
    [SerializeField] private LayerMask losBlockers = ~0;

    [Header("Aim")]
    [SerializeField] private float aimToleranceDeg = 3.5f;
    [SerializeField] private float aimYOffset = 1.0f;
    [SerializeField] private float firstShotDelay = 6f;
    [SerializeField] private float aimNoiseRadius = 0.8f;

    private float spawnTime;
    private Vector3 currentAimNoise;
    private float nextNoiseUpdate;

    [Header("Hull Movement")]
    [SerializeField] private float hullTurnSpeed = 18f;
    [SerializeField] private float idealDistance = 35f;
    [SerializeField] private float forwardSpeed = 6f;
    [SerializeField] private float reverseSpeed = 3f;
    [SerializeField] private float distanceHysteresis = 4f;
    [SerializeField] private float maxClimbAngle = 25f;
    [SerializeField] private LayerMask groundMask = ~(1 << 8);

    void Awake()
    {
        if (gunner != null) gunner.PlayerControlled = false;
        if (turret != null) turret.AiControlled = true;
        if (hull == null) hull = GetComponent<HullController>();
        if (hull != null)
        {
            hull.ReadKeyboard = false;
            hull.enabled = true;
        }
    }

    void Start()
    {
        spawnTime = Time.time;
        if (target == null)
        {
            var t = GameObject.Find("t55");
            if (t != null && t.transform != transform) target = t.transform;
        }
    }

    void Update()
    {
        if (selfHealth != null && selfHealth.IsDead) return;
        if (target == null || turret == null || gunner == null) return;

        if (Time.time >= nextNoiseUpdate)
        {
            currentAimNoise = Random.insideUnitSphere * aimNoiseRadius;
            currentAimNoise.y *= 0.4f;
            nextNoiseUpdate = Time.time + 1.5f;
        }

        Vector3 aimTarget = target.position + Vector3.up * aimYOffset + currentAimNoise;
        turret.ExternalAimPoint = aimTarget;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > detectionRange) return;

        bool combatReady = (Time.time - spawnTime) >= firstShotDelay;
        if (combatReady && IsAimedAtTarget(aimTarget) && HasLineOfSight(aimTarget) && !gunner.IsReloading)
        {
            gunner.TryFire();
        }

        DriveHull(dist);
    }

    bool IsAimedAtTarget(Vector3 aimTarget)
    {
        if (gun == null || gun.Muzzle == null) return false;
        Vector3 muzzleForward = gun.Muzzle.forward;
        Vector3 toTarget = (aimTarget - gun.Muzzle.position).normalized;
        return Vector3.Angle(muzzleForward, toTarget) <= aimToleranceDeg;
    }

    bool HasLineOfSight(Vector3 aimTarget)
    {
        if (gun == null || gun.Muzzle == null) return true;
        Vector3 origin = gun.Muzzle.position;
        Vector3 dir = aimTarget - origin;
        float distance = dir.magnitude;
        if (distance > losRange) return false;
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, distance, losBlockers, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }
        return true;
    }

    bool IsGroundSteep()
    {
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2.5f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return Vector3.Angle(hit.normal, Vector3.up) > maxClimbAngle;
        }
        return false;
    }

    void DriveHull(float currentDistance)
    {
        if (hull == null) { if (hullRb != null) hullRb.linearVelocity *= 0.9f; return; }

        Vector3 flatToTarget = target.position - transform.position;
        flatToTarget.y = 0f;
        if (flatToTarget.sqrMagnitude < 0.01f) { hull.SetInput(0f, 0f); return; }

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) { hull.SetInput(0f, 0f); return; }
        fwd.Normalize();
        Vector3 dir = flatToTarget.normalized;
        float angle = Vector3.SignedAngle(fwd, dir, Vector3.up);
        float steer = Mathf.Clamp(angle / 25f, -1f, 1f);

        bool steep = IsGroundSteep();
        float diff = currentDistance - idealDistance;
        float throttle = 0f;
        if (Mathf.Abs(angle) < 70f)
        {
            if (diff > distanceHysteresis && !steep) throttle = 1f;
            else if (diff < -distanceHysteresis) throttle = -0.6f;
        }

        hull.SetInput(throttle, steer);
    }
}
