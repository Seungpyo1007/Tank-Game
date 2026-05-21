using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainMeshGenerator : MonoBehaviour
{
    [Header("Size")]
    [SerializeField] private float worldSize = 240f;
    [SerializeField] private int subdivisions = 160;

    [Header("Height")]
    [SerializeField] private float heightScale = 2.8f;
    [SerializeField] private float noiseScale = 0.025f;
    [SerializeField] private int octaves = 3;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private int seed = 7;

    [Header("Flatten")]
    [SerializeField] private float flattenRadius = 20f;
    [SerializeField] private float flattenFalloff = 12f;

    [Header("Material")]
    [SerializeField] private Material material;

    [SerializeField] private bool generateOnAwake = true;
    [SerializeField] private bool addCollider = true;

    void Awake()
    {
        if (generateOnAwake) Generate();
    }

    [ContextMenu("Generate Terrain")]
    public void Generate()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();

        var mesh = new Mesh { name = "ProceduralTerrain" };
        mesh.indexFormat = IndexFormat.UInt32;

        int grid = subdivisions + 1;
        var verts = new Vector3[grid * grid];
        var uvs = new Vector2[grid * grid];
        var tris = new int[subdivisions * subdivisions * 6];

        float step = worldSize / subdivisions;
        float half = worldSize * 0.5f;

        var rng = new System.Random(seed);
        float ox = (float)(rng.NextDouble() * 1000.0);
        float oz = (float)(rng.NextDouble() * 1000.0);

        for (int z = 0; z < grid; z++)
        {
            for (int x = 0; x < grid; x++)
            {
                float wx = -half + x * step;
                float wz = -half + z * step;
                float h = SampleHeight(wx, wz, ox, oz);
                verts[z * grid + x] = new Vector3(wx, h, wz);
                uvs[z * grid + x] = new Vector2(x / (float)subdivisions, z / (float)subdivisions);
            }
        }

        int ti = 0;
        for (int z = 0; z < subdivisions; z++)
        {
            for (int x = 0; x < subdivisions; x++)
            {
                int i = z * grid + x;
                tris[ti++] = i;
                tris[ti++] = i + grid;
                tris[ti++] = i + 1;
                tris[ti++] = i + 1;
                tris[ti++] = i + grid;
                tris[ti++] = i + grid + 1;
            }
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;

        if (addCollider)
        {
            var col = GetComponent<MeshCollider>();
            if (col == null) col = gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        if (material != null) mr.sharedMaterial = material;
    }

    float SampleHeight(float wx, float wz, float ox, float oz)
    {
        float amp = 1f, freq = noiseScale, sum = 0f, norm = 0f;
        for (int o = 0; o < octaves; o++)
        {
            sum += Mathf.PerlinNoise(ox + wx * freq, oz + wz * freq) * amp;
            norm += amp;
            amp *= persistence;
            freq *= lacunarity;
        }
        float h = (sum / norm - 0.5f) * 2f * heightScale;

        float d = Mathf.Sqrt(wx * wx + wz * wz);
        if (d < flattenRadius + flattenFalloff)
        {
            float t = Mathf.SmoothStep(0f, 1f, (d - flattenRadius) / Mathf.Max(0.01f, flattenFalloff));
            h *= t;
        }
        return h;
    }
}
