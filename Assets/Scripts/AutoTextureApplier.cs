using UnityEngine;

[ExecuteInEditMode]
public class AutoTextureApplier : MonoBehaviour
{
    [Header("Materials to Apply")]
    public Material tankMaterial;
    public Material bulletMaterial;
    public Material mapMaterial;

    [Header("Search Keywords")]
    public string tankKeyword = "t55";
    public string bulletKeyword = "bullet";
    public string mapKeyword = "desert city";

    void Start()
    {
        ApplyToScene();
    }

    [ContextMenu("Manual Apply Materials")]
    public void ApplyToScene()
    {
        // Find all objects in the scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
        
        foreach (GameObject obj in allObjects)
        {
            string lowerName = obj.name.ToLower();
            
            // Check for Tank
            if (lowerName.Contains(tankKeyword.ToLower()) || lowerName.Contains("tank"))
            {
                ApplyMaterialToAllSlots(obj, tankMaterial);
            }
            // Check for Bullet
            else if (lowerName.Contains(bulletKeyword.ToLower()) || lowerName.Contains("223"))
            {
                ApplyMaterialToAllSlots(obj, bulletMaterial);
            }
            // Check for Map
            else if (lowerName.Contains(mapKeyword.ToLower()) || lowerName.Contains("map"))
            {
                ApplyMaterialToAllSlots(obj, mapMaterial);
            }
        }
        
        Debug.Log("AutoTextureApplier: Applied materials to scene objects.");
    }

    private void ApplyMaterialToAllSlots(GameObject obj, Material mat)
    {
        if (mat == null) return;
        
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            // Create a new array of materials to replace all slots
            Material[] newMaterials = new Material[mr.sharedMaterials.Length];
            for (int i = 0; i < newMaterials.Length; i++)
            {
                newMaterials[i] = mat;
            }
            mr.sharedMaterials = newMaterials;
        }
    }
}
