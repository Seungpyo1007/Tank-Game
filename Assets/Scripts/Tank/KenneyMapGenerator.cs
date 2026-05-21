using UnityEngine;

public class KenneyMapGenerator : MonoBehaviour
{
    [SerializeField] private GameObject[] buildingPrefabs;
    [SerializeField] private GameObject[] fencePrefabs;
    [SerializeField] private GameObject[] treePrefabs;
    [SerializeField] private GameObject[] pathPrefabs;
    [SerializeField] private Material buildingMaterial;
    [SerializeField] private Material fenceMaterial;
    [SerializeField] private Material treeMaterial;

    [Header("Spawn")]
    [SerializeField] private int seed = 42;
    [SerializeField] private int buildingCount = 14;
    [SerializeField] private int fenceCount = 8;
    [SerializeField] private int treeCount = 8;
    [SerializeField] private int pathCount = 5;
    [SerializeField] private float minRadius = 18f;
    [SerializeField] private float maxRadius = 80f;
    [SerializeField] private float buildingScaleMin = 5f;
    [SerializeField] private float buildingScaleMax = 9f;
    [SerializeField] private float fenceScaleMin = 4f;
    [SerializeField] private float fenceScaleMax = 6f;
    [SerializeField] private float treeScaleMin = 4f;
    [SerializeField] private float treeScaleMax = 7f;

    [SerializeField] private bool spawnOnAwake = true;

    void Awake()
    {
        if (spawnOnAwake) Regenerate();
    }

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        ClearChildren();
        var s = Random.state;
        Random.InitState(seed);

        for (int i = 0; i < buildingCount; i++)
            SpawnOne(buildingPrefabs, buildingScaleMin, buildingScaleMax, true, buildingMaterial);
        for (int i = 0; i < fenceCount; i++)
            SpawnOne(fencePrefabs, fenceScaleMin, fenceScaleMax, true, fenceMaterial);
        for (int i = 0; i < treeCount; i++)
            SpawnOne(treePrefabs, treeScaleMin, treeScaleMax, false, treeMaterial);
        for (int i = 0; i < pathCount; i++)
            SpawnOne(pathPrefabs, 3f, 5f, false, buildingMaterial);

        Random.state = s;
    }

    [ContextMenu("Clear")]
    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
    }

    void SpawnOne(GameObject[] prefabs, float minS, float maxS, bool addCollider, Material overrideMat)
    {
        if (prefabs == null || prefabs.Length == 0) return;
        var prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return;

        var go = Instantiate(prefab, transform);
        go.transform.position = PickPosition();
        go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        go.transform.localScale = Vector3.one * Random.Range(minS, maxS);

        if (addCollider)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<MeshCollider>() != null) continue;
                var col = mf.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = mf.sharedMesh;
            }
        }

        if (overrideMat != null)
        {
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                int n = r.sharedMaterials != null ? r.sharedMaterials.Length : 1;
                var mats = new Material[Mathf.Max(1, n)];
                for (int i = 0; i < mats.Length; i++) mats[i] = overrideMat;
                r.sharedMaterials = mats;
            }
        }
    }

    Vector3 PickPosition()
    {
        float ang = Random.Range(0f, Mathf.PI * 2f);
        float r = Random.Range(minRadius, maxRadius);
        float x = Mathf.Cos(ang) * r;
        float z = Mathf.Sin(ang) * r;
        if (Physics.Raycast(new Vector3(x, 200f, z), Vector3.down, out RaycastHit hit, 400f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point;
        return new Vector3(x, 0f, z);
    }
}
