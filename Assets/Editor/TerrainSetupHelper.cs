#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class TerrainSetupHelper
{
    const string DataFolder = "Assets/TerrainDemoScene_URP/Terrain/Data";

    [MenuItem("TankGame/Load Demo Terrain")]
    public static void Load()
    {
        var existing = GameObject.Find("WorldTerrain");
        if (existing != null) Object.DestroyImmediate(existing);

        var parent = new GameObject("WorldTerrain");
        parent.transform.position = Vector3.zero;

        var guids = AssetDatabase.FindAssets("t:TerrainData", new[] { DataFolder });
        int loaded = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName.EndsWith(" 1")) continue;

            var parts = fileName.Split('_');
            if (parts.Length < 3) continue;
            if (!int.TryParse(parts[1], out int col)) continue;
            if (!int.TryParse(parts[2], out int row)) continue;

            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data == null) continue;

            float tileSize = data.size.x;
            var go = new GameObject($"Terrain_{col}_{row}", typeof(Terrain), typeof(TerrainCollider));
            go.transform.SetParent(parent.transform, false);
            float halfGrid = tileSize * 2f;
            go.transform.position = new Vector3(col * tileSize - halfGrid, 0f, row * tileSize - halfGrid);
            go.isStatic = true;

            var t = go.GetComponent<Terrain>();
            t.terrainData = data;
            t.groupingID = 0;
            t.allowAutoConnect = true;

            var tc = go.GetComponent<TerrainCollider>();
            tc.terrainData = data;

            loaded++;
        }

        if (loaded > 0)
        {
            Terrain.SetConnectivityDirty();
            Debug.Log($"[TerrainSetup] Loaded {loaded} terrain tiles into WorldTerrain.");
        }
        else
        {
            Debug.LogWarning($"[TerrainSetup] No TerrainData found in {DataFolder}");
        }
    }

    [MenuItem("TankGame/Clear Demo Terrain")]
    public static void Clear()
    {
        var existing = GameObject.Find("WorldTerrain");
        if (existing != null) Object.DestroyImmediate(existing);
    }

    [MenuItem("TankGame/Snap Tanks To Terrain")]
    public static void SnapTanks()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        int moved = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!root.name.StartsWith("t55")) continue;
            var pos = root.transform.position;
            float terrainY = SampleTerrainHeight(pos);
            root.transform.position = new Vector3(pos.x, terrainY + 2.5f, pos.z);
            moved++;
            Debug.Log($"Moved {root.name} to y={terrainY + 2.5f:F2} (terrain={terrainY:F2})");
        }
        Debug.Log($"Snapped {moved} tanks to terrain.");
    }

    static float SampleTerrainHeight(Vector3 worldPos)
    {
        float best = float.NegativeInfinity;
        foreach (var t in Terrain.activeTerrains)
        {
            var size = t.terrainData.size;
            var basePos = t.transform.position;
            if (worldPos.x < basePos.x || worldPos.x > basePos.x + size.x) continue;
            if (worldPos.z < basePos.z || worldPos.z > basePos.z + size.z) continue;
            float y = t.SampleHeight(worldPos) + basePos.y;
            if (y > best) best = y;
        }
        return best == float.NegativeInfinity ? 0f : best;
    }

    [MenuItem("TankGame/Find Pink Materials (All Project)")]
    public static void FindPinkMaterials()
    {
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        Debug.Log($"Current pipeline: {(pipeline != null ? pipeline.GetType().Name : "Built-in")}");

        int bad = 0, total = 0;
        var guids = AssetDatabase.FindAssets("t:Material");
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.StartsWith("Packages/")) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null || m.shader == null) continue;
            total++;
            var sh = m.shader.name;
            if (IsBadShader(sh))
            {
                bad++;
                Debug.Log($"  [BAD MAT] {p}   shader='{sh}'");
            }
        }
        Debug.Log($"Material check: {bad}/{total} project materials use non-URP shader.");

        int badTerrain = 0;
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            var mt = t.materialTemplate;
            if (mt == null)
            {
                badTerrain++;
                Debug.Log($"  [TERRAIN NULL MAT] {t.gameObject.name}");
            }
            else if (IsBadShader(mt.shader.name))
            {
                badTerrain++;
                Debug.Log($"  [TERRAIN BAD MAT] {t.gameObject.name}  shader='{mt.shader.name}'");
            }
        }
        Debug.Log($"Terrain check: {badTerrain} terrains have non-URP material.");
    }

    static bool IsBadShader(string name)
    {
        return name.Contains("Standard")
            || name.StartsWith("HDRP")
            || name.StartsWith("Hidden/InternalErrorShader")
            || name.Contains("Nature/SpeedTree");
    }

    [MenuItem("TankGame/Fix Terrain Materials")]
    public static void FixTerrainMaterials()
    {
        Material demoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TerrainDemoScene_URP/Terrain/Materials/TerrainLit.mat");
        Material mat = demoMat;

        if (mat == null)
        {
            var urpTerrain = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (urpTerrain == null)
            {
                Debug.LogError("URP Terrain shader not found and demo TerrainLit.mat missing.");
                return;
            }
            mat = new Material(urpTerrain) { name = "URP_Terrain_Default" };
            string path = "Assets/Materials/URP_Terrain_Default.mat";
            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"Created fallback terrain material at {path}");
        }

        int n = 0;
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            t.materialTemplate = mat;
            EditorUtility.SetDirty(t);
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Applied {mat.name} (shader: {mat.shader.name}) to {n} terrains.");
    }

    [MenuItem("TankGame/Restore Demo Tree Textures")]
    public static void RestoreDemoTreeTextures()
    {
        int matsFixed = 0, basemapsSet = 0, normalsSet = 0;
        var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/TerrainDemoScene_URP" });
        foreach (var g in guids)
        {
            var matPath = AssetDatabase.GUIDToAssetPath(g);
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m == null) continue;

            string folder = System.IO.Path.GetDirectoryName(matPath);
            string texFolder = folder.EndsWith("Materials") ? folder.Replace("Materials", "Textures") : folder + "/Textures";
            if (!System.IO.Directory.Exists(texFolder)) texFolder = folder;

            string matName = System.IO.Path.GetFileNameWithoutExtension(matPath);
            string stem = matName.Replace("_Material", "");

            Texture2D baseTex = null, normalTex = null;
            var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { texFolder });
            foreach (var tg in texGuids)
            {
                var tp = AssetDatabase.GUIDToAssetPath(tg);
                var tn = System.IO.Path.GetFileNameWithoutExtension(tp);
                if (!tn.ToLower().Contains(stem.ToLower().Replace("conifer", "confier").Substring(0, System.Math.Min(stem.Length, 6)))) {
                    // Try other stem variations
                    if (!tn.ToLower().StartsWith(stem.ToLower().Substring(0, System.Math.Min(stem.Length, 4)))) continue;
                }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
                if (tex == null) continue;
                var tnl = tn.ToLower();
                if (tnl.Contains("color") || tnl.Contains("diff") || tnl.Contains("albedo") || tnl.Contains("basecolor"))
                {
                    if (baseTex == null) baseTex = tex;
                }
                else if (tnl.Contains("normal") || tnl.Contains("_nor"))
                {
                    if (normalTex == null) normalTex = tex;
                }
            }

            bool changed = false;
            if (baseTex != null && m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", baseTex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", baseTex);
                basemapsSet++;
                changed = true;
            }
            if (normalTex != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normalTex);
                m.EnableKeyword("_NORMALMAP");
                normalsSet++;
                changed = true;
            }
            if (changed)
            {
                EditorUtility.SetDirty(m);
                matsFixed++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Tree texture restore: {matsFixed} materials updated. {basemapsSet} basemaps, {normalsSet} normals.");
    }

    [MenuItem("TankGame/Convert Standard Mats To URP")]
    public static void ConvertStandardMats()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        var urpParticles = Shader.Find("Universal Render Pipeline/Particles/Lit");
        var urpTerrain = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (urpLit == null) { Debug.LogError("URP/Lit shader not found"); return; }

        int n = 0, errored = 0;
        var guids = AssetDatabase.FindAssets("t:Material");
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.StartsWith("Packages/")) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null || m.shader == null) continue;
            var sh = m.shader.name;

            Shader replace = null;
            if (sh.StartsWith("Hidden/InternalErrorShader")) replace = urpLit;
            else if (sh.Contains("Standard")) replace = urpLit;
            else if (sh.StartsWith("HDRP/Lit")) replace = urpLit;
            else if (sh.StartsWith("HDRP/TerrainLit")) replace = urpTerrain;
            else if (sh.StartsWith("HDRP/Unlit")) replace = urpUnlit;
            else if (sh.StartsWith("HDRP/")) replace = urpLit;
            else if (sh.Contains("Nature/SpeedTree")) replace = urpLit;
            if (replace == null) continue;

            try
            {
                m.shader = replace;
                EditorUtility.SetDirty(m);
                n++;
            }
            catch (System.Exception e)
            {
                errored++;
                Debug.LogWarning($"Failed to convert {p}: {e.Message}");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Converted {n} materials. ({errored} errors)");
    }
}
#endif
