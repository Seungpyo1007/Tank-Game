using UnityEngine;

[ExecuteAlways]
public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Material buildingMat;
    [SerializeField] private Material concreteMat;
    [SerializeField] private Material rockMat;

    [Header("Layout")]
    [SerializeField] private int seed = 42;
    [SerializeField] private float radius = 80f;
    [SerializeField] private float minDistFromCenter = 12f;
    [SerializeField] private int buildingCount = 8;
    [SerializeField] private int wallCount = 6;
    [SerializeField] private int rockCount = 10;

    [SerializeField] private bool regenerateOnAwake = true;

    void Awake()
    {
        if (regenerateOnAwake) Regenerate();
    }

    [ContextMenu("Regenerate Map")]
    public void Regenerate()
    {
        ClearChildren();
        var prevState = Random.state;
        Random.InitState(seed);

        for (int i = 0; i < buildingCount; i++)
        {
            Vector3 pos = PickPosition();
            float w = Random.Range(5f, 10f);
            float h = Random.Range(5f, 9f);
            float d = Random.Range(5f, 10f);
            float yaw = Random.Range(0f, 360f);
            SpawnBox($"Building_{i}", pos, new Vector3(w, h, d), yaw, buildingMat);
        }
        for (int i = 0; i < wallCount; i++)
        {
            Vector3 pos = PickPosition();
            float l = Random.Range(7f, 13f);
            float h = Random.Range(1.8f, 2.8f);
            float yaw = Random.Range(0f, 360f);
            SpawnBox($"Wall_{i}", pos, new Vector3(l, h, 0.7f), yaw, concreteMat);
        }
        for (int i = 0; i < rockCount; i++)
        {
            Vector3 pos = PickPosition();
            float s = Random.Range(0.9f, 2.4f);
            Vector3 size = new Vector3(s * Random.Range(0.85f, 1.3f), s * Random.Range(0.6f, 1.1f), s * Random.Range(0.85f, 1.3f));
            float yaw = Random.Range(0f, 360f);
            SpawnBox($"Rock_{i}", pos, size, yaw, rockMat);
        }

        Random.state = prevState;
    }

    [ContextMenu("Clear Map")]
    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
    }

    Vector3 PickPosition()
    {
        for (int i = 0; i < 50; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(Random.value) * radius;
            float x = Mathf.Cos(ang) * r;
            float z = Mathf.Sin(ang) * r;
            if (x * x + z * z >= minDistFromCenter * minDistFromCenter)
                return new Vector3(x, 0f, z);
        }
        return new Vector3(20f, 0f, 20f);
    }

    GameObject SpawnBox(string name, Vector3 basePos, Vector3 size, float yawDeg, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.position = basePos + Vector3.up * size.y * 0.5f;
        go.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}
