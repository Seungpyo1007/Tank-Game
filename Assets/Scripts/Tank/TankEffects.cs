using UnityEngine;

public class TankEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody hullRb;
    [SerializeField] private Material smokeMaterial;
    [SerializeField] private Material dustMaterial;

    [Header("Exhaust")]
    [SerializeField] private Vector3 exhaustLocalOffset = new Vector3(0.5f, 1.6f, -2.2f);
    [SerializeField] private float exhaustBaseRate = 4f;
    [SerializeField] private float exhaustMaxRate = 30f;
    [SerializeField] private float exhaustSpeed = 1.2f;
    [SerializeField] private float exhaustStartSize = 0.4f;
    [SerializeField] private float exhaustEndSize = 1.8f;
    [SerializeField] private float exhaustLifetime = 2.5f;

    [Header("Track Dust")]
    [SerializeField] private Vector3 dustLeftLocalOffset = new Vector3(-1.4f, 0.1f, -1.0f);
    [SerializeField] private Vector3 dustRightLocalOffset = new Vector3(1.4f, 0.1f, -1.0f);
    [SerializeField] private float dustRateAtSpeed = 18f;
    [SerializeField] private float dustReferenceSpeed = 10f;
    [SerializeField] private float dustStartSize = 0.5f;
    [SerializeField] private float dustEndSize = 1.4f;
    [SerializeField] private float dustLifetime = 1.4f;

    private ParticleSystem exhaustPS;
    private ParticleSystem dustLeftPS;
    private ParticleSystem dustRightPS;

    void Start()
    {
        BuildExhaust();
        BuildDust(out dustLeftPS, dustLeftLocalOffset, "TrackDust_L");
        BuildDust(out dustRightPS, dustRightLocalOffset, "TrackDust_R");
    }

    void Update()
    {
        float speed = hullRb != null ? new Vector2(hullRb.linearVelocity.x, hullRb.linearVelocity.z).magnitude : 0f;
        float speedNorm = Mathf.Clamp01(speed / Mathf.Max(0.01f, dustReferenceSpeed));

        if (exhaustPS != null)
        {
            var em = exhaustPS.emission;
            em.rateOverTime = Mathf.Lerp(exhaustBaseRate, exhaustMaxRate, speedNorm);
        }
        if (dustLeftPS != null)
        {
            var em = dustLeftPS.emission;
            em.rateOverTime = dustRateAtSpeed * speedNorm;
        }
        if (dustRightPS != null)
        {
            var em = dustRightPS.emission;
            em.rateOverTime = dustRateAtSpeed * speedNorm;
        }
    }

    void BuildExhaust()
    {
        var go = new GameObject("Exhaust");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = exhaustLocalOffset;
        go.transform.localRotation = Quaternion.Euler(-30f, 180f, 0f);

        exhaustPS = go.AddComponent<ParticleSystem>();
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (smokeMaterial != null) r.sharedMaterial = smokeMaterial;
        r.renderMode = ParticleSystemRenderMode.Billboard;

        var main = exhaustPS.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(exhaustSpeed * 0.6f, exhaustSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(exhaustStartSize * 0.6f, exhaustStartSize);
        main.startLifetime = new ParticleSystem.MinMaxCurve(exhaustLifetime * 0.7f, exhaustLifetime);
        main.startColor = new Color(0.45f, 0.42f, 0.38f, 0.7f);
        main.maxParticles = 80;
        main.loop = true;
        main.playOnAwake = true;
        main.gravityModifier = -0.04f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var em = exhaustPS.emission;
        em.enabled = true;
        em.rateOverTime = exhaustBaseRate;

        var shape = exhaustPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.06f;

        var col = exhaustPS.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.55f, 0.5f, 0.45f), 0f), new GradientColorKey(new Color(0.32f, 0.3f, 0.28f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.35f, 0.4f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = exhaustPS.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(1f, exhaustEndSize / Mathf.Max(0.01f, exhaustStartSize)));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    void BuildDust(out ParticleSystem ps, Vector3 localOffset, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Quaternion.Euler(-80f, 0f, 0f);

        ps = go.AddComponent<ParticleSystem>();
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (dustMaterial != null) r.sharedMaterial = dustMaterial;
        r.renderMode = ParticleSystemRenderMode.Billboard;

        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(dustStartSize * 0.6f, dustStartSize);
        main.startLifetime = new ParticleSystem.MinMaxCurve(dustLifetime * 0.7f, dustLifetime);
        main.startColor = new Color(0.78f, 0.7f, 0.6f, 0.65f);
        main.maxParticles = 60;
        main.loop = true;
        main.playOnAwake = true;
        main.gravityModifier = 0.06f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.15f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.85f, 0.78f, 0.6f), 0f), new GradientColorKey(new Color(0.45f, 0.42f, 0.4f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(1f, dustEndSize / Mathf.Max(0.01f, dustStartSize)));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }
}
