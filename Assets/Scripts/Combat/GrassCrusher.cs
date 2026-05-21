using System.Collections.Generic;
using UnityEngine;

public class GrassCrusher : MonoBehaviour
{
    [SerializeField] private float crushRadius = 3f;
    [SerializeField] private float updateInterval = 0.4f;

    private float nextUpdate;
    private readonly Dictionary<TerrainData, List<int[,]>> originals = new Dictionary<TerrainData, List<int[,]>>();

    void Update()
    {
        if (Time.time < nextUpdate) return;
        nextUpdate = Time.time + updateInterval;
        foreach (var t in Terrain.activeTerrains) Crush(t);
    }

    void OnDisable()
    {
        foreach (var pair in originals)
        {
            var data = pair.Key;
            var snaps = pair.Value;
            for (int p = 0; p < snaps.Count && p < data.detailPrototypes.Length; p++)
                data.SetDetailLayer(0, 0, p, snaps[p]);
        }
        originals.Clear();
    }

    void Crush(Terrain t)
    {
        var data = t.terrainData;
        var size = data.size;
        var basePos = t.transform.position;

        float u = (transform.position.x - basePos.x) / size.x;
        float v = (transform.position.z - basePos.z) / size.z;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return;

        int detailRes = data.detailResolution;
        int cx = Mathf.RoundToInt(u * detailRes);
        int cy = Mathf.RoundToInt(v * detailRes);
        int rad = Mathf.CeilToInt(crushRadius / size.x * detailRes);

        int xMin = Mathf.Clamp(cx - rad, 0, detailRes - 1);
        int xMax = Mathf.Clamp(cx + rad, 0, detailRes - 1);
        int yMin = Mathf.Clamp(cy - rad, 0, detailRes - 1);
        int yMax = Mathf.Clamp(cy + rad, 0, detailRes - 1);
        int w = xMax - xMin + 1;
        int h = yMax - yMin + 1;
        if (w <= 0 || h <= 0) return;

        int protos = data.detailPrototypes.Length;
        if (!originals.ContainsKey(data))
        {
            var snap = new List<int[,]>();
            for (int p = 0; p < protos; p++)
                snap.Add(data.GetDetailLayer(0, 0, detailRes, detailRes, p));
            originals[data] = snap;
        }

        for (int p = 0; p < protos; p++)
        {
            var layer = data.GetDetailLayer(xMin, yMin, w, h, p);
            bool changed = false;
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    if (layer[dy, dx] > 0) { layer[dy, dx] = 0; changed = true; }
                }
            }
            if (changed) data.SetDetailLayer(xMin, yMin, p, layer);
        }
    }
}
