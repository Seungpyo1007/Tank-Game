using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHP = 1000f;
    [SerializeField] private float currentHP;
    [SerializeField] private bool destroyOnDeath = false;

    [Header("Death FX (optional)")]
    [SerializeField] private Material flashCoreMaterial;
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Material smokeMaterial;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public float HPRatio => Mathf.Clamp01(currentHP / Mathf.Max(0.01f, maxHP));
    public bool IsDead { get; private set; }

    public System.Action<Health> OnDied;
    public System.Action<Health, float, Vector3> OnDamaged;

    void Awake()
    {
        if (currentHP <= 0f) currentHP = maxHP;
    }

    public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 direction)
    {
        if (IsDead || amount <= 0f) return;
        currentHP -= amount;
        OnDamaged?.Invoke(this, amount, hitPoint);
        if (GameStats.Instance != null && this.CompareTag("Player"))
            GameStats.Instance.damageTaken += amount;
        if (currentHP <= 0f) Die(hitPoint);
    }

    public void Kill(Vector3 hitPoint, Vector3 direction)
    {
        if (IsDead) return;
        OnDamaged?.Invoke(this, currentHP, hitPoint);
        currentHP = 0f;
        Die(hitPoint);
    }

    void Die(Vector3 deathPoint)
    {
        IsDead = true;
        OnDied?.Invoke(this);
        SpawnDeathExplosion(deathPoint);

        var hull = GetComponent<HullController>();
        if (hull != null) hull.enabled = false;
        var turret = GetComponentInChildren<TurretController>();
        if (turret != null) turret.enabled = false;
        var gun = GetComponentInChildren<GunController>();
        if (gun != null) gun.enabled = false;
        var gunner = GetComponent<Gunner>();
        if (gunner != null) gunner.enabled = false;
        var ai = GetComponent<EnemyTankAI>();
        if (ai != null) ai.enabled = false;
        var fx = GetComponent<TankEffects>();
        if (fx != null) fx.enabled = false;

        var deathSmoke = GetComponent<TankDeathSmoke>();
        if (deathSmoke == null) deathSmoke = gameObject.AddComponent<TankDeathSmoke>();
        deathSmoke.smokeMaterial = smokeMaterial;
        deathSmoke.fireMaterial = flashCoreMaterial;
        deathSmoke.Begin();

        if (destroyOnDeath) Destroy(gameObject, 6f);
    }

    void SpawnDeathExplosion(Vector3 pos)
    {
        if (flashCoreMaterial == null || smokeMaterial == null) return;
        var go = new GameObject("DeathExplosion");
        go.transform.position = pos;
        var fx = go.AddComponent<MuzzleFlashFX>();
        fx.flashCoreMaterial = flashCoreMaterial;
        fx.sparkMaterial = sparkMaterial;
        fx.smokeMaterial = smokeMaterial;
        fx.flashColor = new Color(1f, 0.5f, 0.1f);
        fx.lightIntensity = 120f;
        fx.lightRange = 50f;
        fx.lightDuration = 0.4f;
        fx.coreStartRadius = 1f;
        fx.coreEndRadius = 6f;
        fx.coreDuration = 0.4f;
        fx.sparkCount = 200;
        fx.sparkSpeed = 28f;
        fx.sparkSize = 0.3f;
        fx.sparkLifetime = 1.2f;
        fx.sparkConeAngle = 65f;
        fx.smokeCount = 100;
        fx.smokeSpeed = 8f;
        fx.smokeStartSize = 2.5f;
        fx.smokeEndSize = 14f;
        fx.smokeLifetime = 8f;
        fx.dustCount = 60;
        fx.dustStartSize = 1f;
        fx.dustEndSize = 5f;
        fx.dustLifetime = 3f;
        fx.shakeAmplitude = 0.6f;
        fx.shakeDuration = 0.9f;
    }
}
