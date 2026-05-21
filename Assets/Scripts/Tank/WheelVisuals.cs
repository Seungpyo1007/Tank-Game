using UnityEngine;

[ExecuteAlways]
public class WheelVisuals : MonoBehaviour
{
    [SerializeField] private HullController hull;
    [SerializeField] private Material wheelMaterial;
    [SerializeField] private float wheelDiameterWorld = 0.7f;
    [SerializeField] private float wheelWidthWorld = 0.3f;
    [SerializeField] private LayerMask groundMask = ~(1 << 8);

    private Transform[] leftVisuals;
    private Transform[] rightVisuals;
    private float spinAngle;

    void Start()
    {
        if (hull == null) hull = GetComponent<HullController>();
        if (hull == null) return;
        Build();
    }

    void Build()
    {
        ClearOld();
        leftVisuals = MakeAll("L", hull.LeftWheels);
        rightVisuals = MakeAll("R", hull.RightWheels);
    }

    Transform[] MakeAll(string side, Vector3[] mounts)
    {
        var arr = new Transform[mounts.Length];
        for (int i = 0; i < mounts.Length; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }
            go.name = $"WheelVis_{side}_{i}";
            go.transform.SetParent(transform, false);
            go.transform.localPosition = mounts[i] + new Vector3(0f, -50f, 0f);
            float scaleComp = 1f / Mathf.Max(0.0001f, transform.lossyScale.x);
            float diameter = wheelDiameterWorld * scaleComp;
            float height = wheelWidthWorld * scaleComp;
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            if (wheelMaterial != null) go.GetComponent<Renderer>().sharedMaterial = wheelMaterial;
            arr[i] = go.transform;
        }
        return arr;
    }

    void ClearOld()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != null && c.name.StartsWith("WheelVis_"))
            {
                if (Application.isPlaying) Destroy(c.gameObject); else DestroyImmediate(c.gameObject);
            }
        }
    }

    void Update()
    {
        if (hull == null || leftVisuals == null) return;
        float radiusWorld = wheelDiameterWorld * 0.5f;
        spinAngle += (hull.ForwardSpeed / Mathf.Max(0.01f, radiusWorld)) * Time.deltaTime * Mathf.Rad2Deg;
        UpdateSide(leftVisuals, hull.LeftWheels);
        UpdateSide(rightVisuals, hull.RightWheels);
    }

    void UpdateSide(Transform[] vis, Vector3[] mounts)
    {
        if (vis == null) return;
        for (int i = 0; i < vis.Length; i++)
        {
            if (vis[i] == null) continue;
            Vector3 mountWorld = transform.TransformPoint(mounts[i]);
            float castLen = (hull.SuspensionRestLength + hull.WheelRadius);
            if (Physics.Raycast(mountWorld, -transform.up, out RaycastHit hit, castLen, groundMask, QueryTriggerInteraction.Ignore))
            {
                vis[i].position = hit.point + transform.up * (wheelDiameterWorld * 0.5f);
            }
            else
            {
                vis[i].position = mountWorld - transform.up * castLen + transform.up * (wheelDiameterWorld * 0.5f);
            }
            vis[i].rotation = transform.rotation * Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(spinAngle, 0f, 0f);
        }
    }
}
