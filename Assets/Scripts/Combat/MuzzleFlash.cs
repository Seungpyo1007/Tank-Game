using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public Material flashMaterial;
    public float duration = 0.15f;
    public float startIntensity = 30f;
    public float lightRange = 25f;
    public float visualStartRadius = 0.3f;
    public float visualEndRadius = 1.4f;
    public Color flashColor = new Color(1f, 0.55f, 0.15f);

    private Light flashLight;
    private Transform visualT;
    private Material instMat;
    private float t;

    void Start()
    {
        flashLight = gameObject.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = flashColor;
        flashLight.range = lightRange;
        flashLight.shadows = LightShadows.None;
        flashLight.intensity = startIntensity;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var col = visual.GetComponent<Collider>();
        if (col != null) Destroy(col);
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * visualStartRadius;
        visualT = visual.transform;
        if (flashMaterial != null)
        {
            var r = visual.GetComponent<Renderer>();
            instMat = new Material(flashMaterial);
            SetMatColor(instMat, flashColor);
            r.sharedMaterial = instMat;
        }
    }

    void Update()
    {
        if (flashLight == null) return;
        t += Time.deltaTime;
        float u = t / duration;
        if (u >= 1f) { Destroy(gameObject); return; }

        float fall = 1f - u;
        flashLight.intensity = startIntensity * fall * fall;
        if (visualT != null)
            visualT.localScale = Vector3.one * Mathf.Lerp(visualStartRadius, visualEndRadius, u);
        if (instMat != null)
        {
            var c = flashColor;
            c.a = fall;
            SetMatColor(instMat, c);
        }
    }

    static void SetMatColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        else m.color = c;
    }

    void OnDestroy() { if (instMat != null) Destroy(instMat); }
}
