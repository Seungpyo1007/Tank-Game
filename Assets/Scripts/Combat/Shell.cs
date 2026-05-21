using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Shell : MonoBehaviour
{
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private ShellData defaultData;

    [Header("Impact FX Materials")]
    [SerializeField] private Material flashCoreMaterial;
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Material smokeMaterial;

    private ShellData data;
    private Rigidbody rb;
    private float spawnTime;
    private bool hit;
    private bool isPlayerShell;

    public ShellData Data => data;
    public bool IsPlayerShell { get => isPlayerShell; set => isPlayerShell = value; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (data == null) data = defaultData;
    }

    public void Initialize(ShellData shellData, Vector3 muzzlePos, Vector3 muzzleForward, Vector3 inheritedVelocity, Collider[] ignoreColliders = null)
    {
        data = shellData;
        transform.position = muzzlePos;
        transform.rotation = Quaternion.LookRotation(muzzleForward);
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.mass = data != null ? data.mass : 15f;
        rb.useGravity = true;
        float speed = data != null ? data.muzzleVelocity : 900f;
        rb.linearVelocity = inheritedVelocity + muzzleForward * speed;
        spawnTime = Time.time;

        if (ignoreColliders != null)
        {
            var myCol = GetComponent<Collider>();
            if (myCol != null)
                foreach (var c in ignoreColliders)
                    if (c != null) Physics.IgnoreCollision(myCol, c, true);
        }
    }

    void Update()
    {
        if (rb == null) return;
        if (!hit && rb.linearVelocity.sqrMagnitude > 1f)
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        if (Time.time - spawnTime > lifetime)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        if (hit) return;
        hit = true;

        Vector3 hitPoint = col.contactCount > 0 ? col.GetContact(0).point : transform.position;
        Vector3 hitNormal = col.contactCount > 0 ? col.GetContact(0).normal : Vector3.up;
        Vector3 shellForward = transform.forward;

        var armor = col.collider.GetComponentInParent<ArmorPart>();
        if (armor != null)
        {
            var result = armor.ProcessHit(data, hitPoint, hitNormal, shellForward, out float dealtDamage, out _, out _);
            SpawnArmorImpact(hitPoint, hitNormal, result);

            if (isPlayerShell && GameStats.Instance != null)
            {
                GameStats.Instance.shotsHit++;
                GameStats.Instance.damageDealt += dealtDamage;
                if (result == ArmorPart.HitResult.Penetration ||
                    result == ArmorPart.HitResult.CriticalAmmo ||
                    result == ArmorPart.HitResult.CriticalCrew ||
                    result == ArmorPart.HitResult.CriticalEngine)
                    GameStats.Instance.penetrations++;
                if (armor.health != null && armor.health.IsDead)
                    GameStats.Instance.kills++;
            }

            if (isPlayerShell && HUDController.Instance != null)
            {
                bool killed = armor.health != null && armor.health.IsDead;
                switch (result)
                {
                    case ArmorPart.HitResult.CriticalAmmo:
                        HUDController.Instance.ShowHitFeedback("AMMO RACK - KILL!", new Color(1f, 0.4f, 0.1f), 2.4f);
                        break;
                    case ArmorPart.HitResult.CriticalCrew:
                        HUDController.Instance.ShowHitFeedback("CREW KILL!", new Color(1f, 0.95f, 0.35f), 2.4f);
                        break;
                    case ArmorPart.HitResult.CriticalEngine:
                        HUDController.Instance.ShowHitFeedback("ENGINE DESTROYED - KILL!", new Color(1f, 0.5f, 0.1f), 2.4f);
                        break;
                    case ArmorPart.HitResult.Penetration:
                        if (killed)
                            HUDController.Instance.ShowHitFeedback("KILL!", new Color(1f, 0.95f, 0.35f), 2.4f);
                        else
                            HUDController.Instance.ShowHitFeedback("PENETRATION", new Color(0.35f, 0.95f, 0.4f), 1.4f);
                        break;
                    default:
                        HUDController.Instance.ShowHitFeedback("NON-PENETRATION", new Color(0.95f, 0.65f, 0.2f), 1.4f);
                        break;
                }
            }
        }
        else
        {
            if (data != null && data.type == ShellType.HE && data.explosionRadius > 0f)
            {
                ApplyExplosion(hitPoint, data);
                SpawnHEGroundFX(hitPoint, hitNormal);
            }
            else
            {
                SpawnAPGroundFX(hitPoint, hitNormal);
            }
        }

        Destroy(gameObject);
    }

    void ApplyExplosion(Vector3 center, ShellData d)
    {
        var around = Physics.OverlapSphere(center, d.explosionRadius);
        foreach (var c in around)
        {
            if (d.explosionForce > 0f)
            {
                var hitRb = c.attachedRigidbody;
                if (hitRb != null)
                    hitRb.AddExplosionForce(d.explosionForce, center, d.explosionRadius, 0.3f, ForceMode.Impulse);
            }

            var armor = c.GetComponentInParent<ArmorPart>();
            if (armor != null && armor.health != null && !armor.health.IsDead)
            {
                float dist = Vector3.Distance(center, c.ClosestPoint(center));
                float falloff = Mathf.Clamp01(1f - dist / d.explosionRadius);
                float splashDmg = d.damage * 0.5f * falloff;
                armor.health.ApplyDamage(splashDmg, center, (c.transform.position - center).normalized);
            }
        }
    }

    void SpawnArmorImpact(Vector3 pos, Vector3 normal, ArmorPart.HitResult result)
    {
        if (flashCoreMaterial == null) return;
        var go = new GameObject("ArmorImpact");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(normal);
        var fx = go.AddComponent<MuzzleFlashFX>();
        fx.flashCoreMaterial = flashCoreMaterial;
        fx.sparkMaterial = sparkMaterial;
        fx.smokeMaterial = smokeMaterial;

        if (result == ArmorPart.HitResult.Penetration)
        {
            fx.flashColor = new Color(1f, 0.45f, 0.1f);
            fx.lightIntensity = 60f;
            fx.lightRange = 25f;
            fx.lightDuration = 0.25f;
            fx.coreStartRadius = 0.6f;
            fx.coreEndRadius = 3.2f;
            fx.coreDuration = 0.2f;
            fx.sparkCount = 90;
            fx.sparkSpeed = 18f;
            fx.sparkLifetime = 0.7f;
            fx.sparkConeAngle = 55f;
            fx.smokeCount = 40;
            fx.smokeStartSize = 1.4f;
            fx.smokeEndSize = 5f;
            fx.smokeLifetime = 4f;
            fx.dustCount = 15;
            fx.shakeAmplitude = 0.12f;
            fx.shakeDuration = 0.25f;
        }
        else
        {
            fx.flashColor = new Color(1f, 0.9f, 0.6f);
            fx.lightIntensity = 25f;
            fx.lightRange = 12f;
            fx.lightDuration = 0.1f;
            fx.coreStartRadius = 0.25f;
            fx.coreEndRadius = 1.0f;
            fx.coreDuration = 0.08f;
            fx.sparkCount = 70;
            fx.sparkSpeed = 28f;
            fx.sparkLifetime = 0.55f;
            fx.sparkConeAngle = 80f;
            fx.smokeCount = 5;
            fx.smokeStartSize = 0.4f;
            fx.smokeEndSize = 1.2f;
            fx.smokeLifetime = 1.2f;
            fx.dustCount = 0;
            fx.shakeAmplitude = 0.04f;
            fx.shakeDuration = 0.1f;
        }
    }

    void SpawnHEGroundFX(Vector3 pos, Vector3 normal)
    {
        if (flashCoreMaterial == null) return;
        var go = new GameObject("HEGroundImpact");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(normal);
        var fx = go.AddComponent<MuzzleFlashFX>();
        fx.flashCoreMaterial = flashCoreMaterial;
        fx.sparkMaterial = sparkMaterial;
        fx.smokeMaterial = smokeMaterial;
        fx.flashColor = new Color(1f, 0.5f, 0.12f);
        fx.lightIntensity = 70f;
        fx.lightRange = 30f;
        fx.lightDuration = 0.3f;
        fx.coreStartRadius = 0.7f;
        fx.coreEndRadius = 4f;
        fx.coreDuration = 0.25f;
        fx.sparkCount = 120;
        fx.sparkSpeed = 16f;
        fx.sparkLifetime = 0.9f;
        fx.sparkConeAngle = 75f;
        fx.smokeCount = 55;
        fx.smokeStartSize = 1.8f;
        fx.smokeEndSize = 7f;
        fx.smokeLifetime = 5f;
        fx.dustCount = 40;
        fx.dustStartSize = 0.8f;
        fx.dustEndSize = 4f;
        fx.dustLifetime = 2.5f;
        fx.shakeAmplitude = 0.3f;
        fx.shakeDuration = 0.5f;
    }

    void SpawnAPGroundFX(Vector3 pos, Vector3 normal)
    {
        if (flashCoreMaterial == null) return;
        var go = new GameObject("APGroundImpact");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(normal);
        var fx = go.AddComponent<MuzzleFlashFX>();
        fx.flashCoreMaterial = flashCoreMaterial;
        fx.sparkMaterial = sparkMaterial;
        fx.smokeMaterial = smokeMaterial;
        fx.flashColor = new Color(0.9f, 0.75f, 0.5f);
        fx.lightIntensity = 18f;
        fx.lightRange = 12f;
        fx.lightDuration = 0.1f;
        fx.coreStartRadius = 0.2f;
        fx.coreEndRadius = 1.2f;
        fx.coreDuration = 0.1f;
        fx.sparkCount = 40;
        fx.sparkSpeed = 12f;
        fx.sparkLifetime = 0.5f;
        fx.sparkConeAngle = 60f;
        fx.smokeCount = 20;
        fx.smokeStartSize = 0.7f;
        fx.smokeEndSize = 2.5f;
        fx.smokeLifetime = 2f;
        fx.dustCount = 25;
        fx.dustStartSize = 0.5f;
        fx.dustEndSize = 2.2f;
        fx.dustLifetime = 1.8f;
        fx.shakeAmplitude = 0.05f;
        fx.shakeDuration = 0.12f;
    }
}
