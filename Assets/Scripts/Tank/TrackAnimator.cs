using UnityEngine;

[ExecuteAlways]
public class TrackAnimator : MonoBehaviour
{
    [SerializeField] private HullController hull;
    [SerializeField] private MeshRenderer chainRenderer;
    [SerializeField] private MeshRenderer wheelsRenderer;
    [SerializeField] private float trackScrollFactor = 0.6f;
    [SerializeField] private float wheelSpinFactor = 0.4f;
    [SerializeField] private string textureProperty = "_BaseMap";

    private Material chainInstance;
    private Material wheelsInstance;
    private float chainOffset;
    private float wheelOffset;

    void Start()
    {
        if (hull == null) hull = GetComponent<HullController>();
        if (chainRenderer == null)
        {
            var t = transform.Find("__55_Chain");
            if (t != null) chainRenderer = t.GetComponent<MeshRenderer>();
        }
        if (wheelsRenderer == null)
        {
            var t = transform.Find("__55_Wheels");
            if (t != null) wheelsRenderer = t.GetComponent<MeshRenderer>();
        }
        if (Application.isPlaying)
        {
            if (chainRenderer != null) chainInstance = chainRenderer.material;
            if (wheelsRenderer != null) wheelsInstance = wheelsRenderer.material;
        }
    }

    void Update()
    {
        if (hull == null) return;
        float dt = Time.deltaTime;
        chainOffset += hull.ForwardSpeed * trackScrollFactor * dt;
        wheelOffset += hull.ForwardSpeed * wheelSpinFactor * dt;
        ApplyOffset(chainInstance, chainOffset);
        ApplyOffset(wheelsInstance, wheelOffset);
    }

    void ApplyOffset(Material m, float value)
    {
        if (m == null) return;
        Vector2 off = new Vector2(value, 0f);
        if (m.HasProperty(textureProperty))
            m.SetTextureOffset(textureProperty, off);
        if (m.HasProperty("_MainTex"))
            m.SetTextureOffset("_MainTex", off);
    }
}
