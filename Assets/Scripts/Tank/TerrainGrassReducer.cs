using UnityEngine;

[ExecuteAlways]
public class TerrainGrassReducer : MonoBehaviour
{
    [Range(0f, 1f)] [SerializeField] private float detailDensity = 0.25f;
    [SerializeField] private float detailVisibleDistance = 60f;
    [SerializeField] private bool applyOnAwake = true;

    void Awake() { if (applyOnAwake) Apply(); }

    [ContextMenu("Apply")]
    public void Apply()
    {
        foreach (var t in Terrain.activeTerrains)
        {
            t.detailObjectDensity = detailDensity;
            t.detailObjectDistance = detailVisibleDistance;
            t.treeBillboardDistance = 80f;
            t.treeCrossFadeLength = 5f;
            t.treeMaximumFullLODCount = 20;
        }
    }
}
