using UnityEngine;

public class TankDeathSmoke : MonoBehaviour
{
    public Material smokeMaterial;
    public Material fireMaterial;
    public Vector3 localOffset = new Vector3(0f, 1.6f, -0.3f);

    private ParticleSystem smokePS;
    private ParticleSystem firePS;
    private bool started;

    public void Begin()
    {
        if (started) return;
        started = true;
        BuildSmoke();
        BuildFire();
    }

    void BuildSmoke()
    {
        var go = new GameObject("DeathSmoke");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localOffset;
        smokePS = go.AddComponent<ParticleSystem>();
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (smokeMaterial != null) r.sharedMaterial = smokeMaterial;
        r.renderMode = ParticleSystemRenderMode.Billboard;

        var main = smokePS.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startColor = new Color(0.2f, 0.18f, 0.18f, 0.95f);
        main.maxParticles = 200;
        main.loop = true;
        main.playOnAwake = true;
        main.gravityModifier = -0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var em = smokePS.emission;
        em.enabled = true;
        em.rateOverTime = 14f;

        var shape = smokePS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.6f;

        var col = smokePS.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.4f, 0.32f, 0.28f), 0f), new GradientColorKey(new Color(0.15f, 0.13f, 0.13f), 0.4f), new GradientColorKey(new Color(0.25f, 0.24f, 0.24f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var sz = smokePS.sizeOverLifetime;
        sz.enabled = true;
        var c = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.4f, 1.2f), new Keyframe(1f, 2.5f));
        sz.size = new ParticleSystem.MinMaxCurve(1f, c);
    }

    void BuildFire()
    {
        var go = new GameObject("DeathFire");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localOffset + new Vector3(0f, -0.3f, 0f);
        firePS = go.AddComponent<ParticleSystem>();
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (fireMaterial != null) r.sharedMaterial = fireMaterial;
        r.renderMode = ParticleSystemRenderMode.Billboard;

        var main = firePS.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startColor = new Color(1f, 0.6f, 0.2f);
        main.maxParticles = 120;
        main.loop = true;
        main.playOnAwake = true;
        main.gravityModifier = -0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = firePS.emission;
        em.enabled = true;
        em.rateOverTime = 25f;

        var shape = firePS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.4f;

        var col = firePS.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.5f), new GradientColorKey(new Color(0.4f, 0.15f, 0.1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var sz = firePS.sizeOverLifetime;
        sz.enabled = true;
        var c = new AnimationCurve(new Keyframe(0f, 1.2f), new Keyframe(1f, 0.3f));
        sz.size = new ParticleSystem.MinMaxCurve(1f, c);
    }
}
