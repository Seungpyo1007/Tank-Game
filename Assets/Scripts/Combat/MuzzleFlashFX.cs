using UnityEngine;

public class MuzzleFlashFX : MonoBehaviour
{
    [Header("Materials")]
    public Material flashCoreMaterial;
    public Material sparkMaterial;
    public Material smokeMaterial;

    [Header("Light")]
    public Color flashColor = new Color(1f, 0.55f, 0.18f);
    public float lightIntensity = 80f;
    public float lightRange = 35f;
    public float lightDuration = 0.22f;

    [Header("Flash Core")]
    public float coreStartRadius = 0.5f;
    public float coreEndRadius = 3.5f;
    public float coreDuration = 0.14f;

    [Header("Sparks")]
    public int sparkCount = 80;
    public float sparkSpeed = 22f;
    public float sparkSize = 0.18f;
    public float sparkLifetime = 0.5f;
    public float sparkConeAngle = 18f;

    [Header("Smoke")]
    public int smokeCount = 45;
    public float smokeSpeed = 5f;
    public float smokeStartSize = 1.2f;
    public float smokeEndSize = 5.5f;
    public float smokeLifetime = 3.5f;

    [Header("Ground Dust")]
    public int dustCount = 25;
    public float dustSpeed = 4f;
    public float dustStartSize = 0.4f;
    public float dustEndSize = 2.0f;
    public float dustLifetime = 1.6f;

    [Header("Camera Shake")]
    public bool shakeCamera = true;
    public float shakeAmplitude = 0.18f;
    public float shakeDuration = 0.35f;

    private Light flashLight;
    private Transform coreT;
    private Material instCore;
    private float t;
    private float maxLife;

    void Start()
    {
        maxLife = Mathf.Max(lightDuration, coreDuration, sparkLifetime, smokeLifetime, dustLifetime) + 0.3f;

        flashLight = gameObject.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = flashColor;
        flashLight.range = lightRange;
        flashLight.shadows = LightShadows.None;
        flashLight.intensity = lightIntensity;

        BuildCore();
        BuildGasJet();
        BuildSparks();
        BuildSmoke();
        BuildGroundDust();

        if (shakeCamera) TriggerCameraShake();

        Destroy(gameObject, maxLife);
    }

    void Update()
    {
        t += Time.deltaTime;

        if (flashLight != null)
        {
            float u = Mathf.Clamp01(t / lightDuration);
            float fall = 1f - u;
            flashLight.intensity = lightIntensity * fall * fall;
        }

        if (coreT != null && instCore != null)
        {
            float u = Mathf.Clamp01(t / coreDuration);
            if (u < 1f)
            {
                coreT.localScale = Vector3.one * Mathf.Lerp(coreStartRadius, coreEndRadius, u);
                var c = flashColor;
                c.a = (1f - u) * 0.9f;
                SetMatColor(instCore, c);
            }
            else if (coreT.gameObject.activeSelf)
            {
                coreT.gameObject.SetActive(false);
            }
        }
    }

    void BuildCore()
    {
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var col = core.GetComponent<Collider>();
        if (col != null) Destroy(col);
        core.name = "FlashCore";
        core.transform.SetParent(transform, false);
        core.transform.localPosition = Vector3.forward * 0.3f;
        core.transform.localScale = Vector3.one * coreStartRadius;
        coreT = core.transform;
        if (flashCoreMaterial != null)
        {
            instCore = new Material(flashCoreMaterial);
            SetMatColor(instCore, flashColor);
            core.GetComponent<Renderer>().sharedMaterial = instCore;
        }
    }

    void BuildGasJet()
    {
        var go = new GameObject("GasJet");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (flashCoreMaterial != null) r.sharedMaterial = flashCoreMaterial;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 2.5f;
        r.velocityScale = 0f;

        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 16f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.startColor = new Color(1f, 0.9f, 0.55f);
        main.maxParticles = 60;
        main.loop = false;
        main.playOnAwake = true;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 40));

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.08f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.4f), new GradientColorKey(new Color(0.4f, 0.15f, 0.05f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 1.5f), new Keyframe(1f, 0.4f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    void BuildSparks()
    {
        var go = new GameObject("Sparks");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (sparkMaterial != null) r.sharedMaterial = sparkMaterial;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 4.0f;
        r.velocityScale = 0.05f;

        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkSpeed * 0.5f, sparkSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(sparkSize * 0.4f, sparkSize);
        main.startLifetime = new ParticleSystem.MinMaxCurve(sparkLifetime * 0.5f, sparkLifetime);
        main.startColor = new Color(1f, 0.95f, 0.6f);
        main.maxParticles = sparkCount + 20;
        main.loop = false;
        main.playOnAwake = true;
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, sparkCount));

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = sparkConeAngle;
        shape.radius = 0.08f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.05f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.1f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    void BuildSmoke()
    {
        var go = new GameObject("Smoke");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (smokeMaterial != null) r.sharedMaterial = smokeMaterial;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortMode = ParticleSystemSortMode.Distance;

        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(smokeSpeed * 0.4f, smokeSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(smokeStartSize * 0.6f, smokeStartSize);
        main.startLifetime = new ParticleSystem.MinMaxCurve(smokeLifetime * 0.6f, smokeLifetime);
        main.startColor = new Color(0.82f, 0.78f, 0.72f, 0.95f);
        main.maxParticles = smokeCount + 20;
        main.loop = false;
        main.playOnAwake = true;
        main.gravityModifier = -0.04f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, smokeCount / 2),
            new ParticleSystem.Burst(0.05f, smokeCount / 3),
            new ParticleSystem.Burst(0.12f, smokeCount / 5)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.2f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.95f, 0.85f, 0.65f), 0f), new GradientColorKey(new Color(0.6f, 0.55f, 0.5f), 0.3f), new GradientColorKey(new Color(0.35f, 0.33f, 0.32f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.7f, 0.2f), new GradientAlphaKey(0.3f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.35f, 1.2f), new Keyframe(1f, smokeEndSize / Mathf.Max(0.01f, smokeStartSize)));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        rot.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        rot.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    void BuildGroundDust()
    {
        var go = new GameObject("GroundDust");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.down * 0.5f;
        var ps = go.AddComponent<ParticleSystem>();
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (smokeMaterial != null) r.sharedMaterial = smokeMaterial;
        r.renderMode = ParticleSystemRenderMode.Billboard;

        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(dustSpeed * 0.5f, dustSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(dustStartSize * 0.6f, dustStartSize);
        main.startLifetime = new ParticleSystem.MinMaxCurve(dustLifetime * 0.6f, dustLifetime);
        main.startColor = new Color(0.78f, 0.72f, 0.62f, 0.85f);
        main.maxParticles = dustCount + 10;
        main.loop = false;
        main.playOnAwake = true;
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, dustCount));

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.3f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.88f, 0.78f, 0.62f), 0f), new GradientColorKey(new Color(0.55f, 0.52f, 0.48f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0.5f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.4f, 1f), new Keyframe(1f, dustEndSize / Mathf.Max(0.01f, dustStartSize)));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    void TriggerCameraShake()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var shake = cam.GetComponent<CameraShake>();
        if (shake == null) shake = cam.gameObject.AddComponent<CameraShake>();
        shake.Shake(shakeAmplitude, shakeDuration);
    }

    static void SetMatColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        else m.color = c;
    }
}
