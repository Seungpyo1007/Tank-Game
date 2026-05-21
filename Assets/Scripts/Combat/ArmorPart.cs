using UnityEngine;

public class ArmorPart : MonoBehaviour
{
    [Header("Hull Armor (mm)")]
    public float hullFrontMm = 100f;
    public float hullSideMm = 80f;
    public float hullRearMm = 60f;
    public float hullTopMm = 30f;

    [Header("Turret Armor (mm)")]
    public float turretFrontMm = 200f;
    public float turretSideMm = 130f;
    public float turretRearMm = 60f;
    public float turretTopMm = 30f;

    [Header("Zoning")]
    [Tooltip("Hit points with local Y above this (in this transform's space) count as turret zone.")]
    public float turretLocalY = 80f;
    public Transform turretRef;

    [Header("Routing")]
    public Health health;
    public TankModules modules;

    [Header("Damage")]
    [Tooltip("Damage fraction applied when shot does NOT penetrate.")]
    public float nonPenDamageMul = 0.12f;

    public enum HitResult { Miss, NonPenetration, Penetration, CriticalAmmo, CriticalCrew, CriticalEngine }

    [Header("Critical Hits")]
    public float turretCritChance = 0.42f;
    public float hullRearCritChance = 0.55f;
    public float hullCenterCritChance = 0.22f;
    public float hullFrontCritChance = 0.15f;

    public HitResult ProcessHit(ShellData shell, Vector3 hitPoint, Vector3 hitNormal, Vector3 shellForward, out float dealtDamage, out float effectiveMm, out string zoneName)
    {
        dealtDamage = 0f;
        effectiveMm = 0f;
        zoneName = "";
        if (shell == null) return HitResult.Miss;

        Vector3 localPoint = transform.InverseTransformPoint(hitPoint);
        bool isTurret = localPoint.y > turretLocalY;
        Transform refT = (isTurret && turretRef != null) ? turretRef : transform;

        Vector3 localNormal = refT.InverseTransformDirection(hitNormal);
        Vector3 localShell = refT.InverseTransformDirection(shellForward);

        float thickness = isTurret
            ? FaceThickness(localNormal, turretFrontMm, turretSideMm, turretRearMm, turretTopMm)
            : FaceThickness(localNormal, hullFrontMm, hullSideMm, hullRearMm, hullTopMm);

        float impactAngle = Vector3.Angle(-localShell, localNormal);
        impactAngle = Mathf.Clamp(impactAngle, 0f, 88f);
        float effective = thickness / Mathf.Max(0.05f, Mathf.Cos(impactAngle * Mathf.Deg2Rad));
        effectiveMm = effective;

        bool isHE = shell.type == ShellType.HE;
        bool penetrates = shell.penetrationMm >= effective;
        zoneName = isTurret ? "Turret" : "Hull";

        if (penetrates)
        {
            ApplyModuleDamage(isTurret, localPoint, shell.damage);
            HitResult crit = RollCritical(isTurret, localPoint);
            if (crit != HitResult.Penetration)
            {
                dealtDamage = (health != null ? health.CurrentHP : shell.damage);
                if (health != null) health.Kill(hitPoint, shellForward);
                return crit;
            }
            dealtDamage = shell.damage;
            if (health != null) health.ApplyDamage(dealtDamage, hitPoint, shellForward);
            return HitResult.Penetration;
        }
        else
        {
            float partial = (isHE ? 0.4f : nonPenDamageMul) * shell.damage;
            dealtDamage = partial;
            if (health != null && partial > 0f) health.ApplyDamage(partial, hitPoint, shellForward);
            return HitResult.NonPenetration;
        }
    }

    void ApplyModuleDamage(bool isTurret, Vector3 localPoint, float baseDamage)
    {
        if (modules == null) return;
        float moduleAmount = baseDamage * 0.6f;
        if (isTurret)
        {
            modules.DamageGun(moduleAmount);
        }
        else
        {
            if (localPoint.z < -40f)
            {
                modules.DamageEngine(moduleAmount);
            }
            else if (Mathf.Abs(localPoint.x) > 80f)
            {
                if (localPoint.x < 0f) modules.DamageLeftTrack(moduleAmount);
                else modules.DamageRightTrack(moduleAmount);
            }
            else if (Random.value < 0.4f)
            {
                if (Random.value < 0.5f) modules.DamageLeftTrack(moduleAmount * 0.6f);
                else modules.DamageRightTrack(moduleAmount * 0.6f);
            }
        }
    }

    HitResult RollCritical(bool isTurret, Vector3 localHitPoint)
    {
        float roll = Random.value;
        if (isTurret)
        {
            if (roll < turretCritChance * 0.5f) return HitResult.CriticalCrew;
            if (roll < turretCritChance) return HitResult.CriticalAmmo;
        }
        else
        {
            float chance = hullCenterCritChance;
            HitResult type = HitResult.CriticalAmmo;
            if (localHitPoint.z < -50f) { chance = hullRearCritChance; type = HitResult.CriticalEngine; }
            else if (localHitPoint.z > 50f) { chance = hullFrontCritChance; type = HitResult.CriticalCrew; }
            if (roll < chance) return type;
        }
        return HitResult.Penetration;
    }

    static float FaceThickness(Vector3 localNormal, float front, float side, float rear, float top)
    {
        float ax = Mathf.Abs(localNormal.x);
        float ay = Mathf.Abs(localNormal.y);
        float az = Mathf.Abs(localNormal.z);
        if (ay > ax && ay > az) return top;
        if (az > ax) return localNormal.z > 0f ? front : rear;
        return side;
    }
}
