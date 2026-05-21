using UnityEngine;

[DefaultExecutionOrder(-100)]
public class TankRigSetup : MonoBehaviour
{
    [SerializeField] private Transform turretPivot;
    [SerializeField] private Transform barrelPivot;
    [SerializeField] private Transform turretMesh;
    [SerializeField] private Transform barrelMesh;

    void Awake()
    {
        Reparent();
        enabled = false;
    }

    [ContextMenu("Reparent Now")]
    public void Reparent()
    {
        if (turretMesh != null && turretPivot != null && turretMesh.parent != turretPivot)
            turretMesh.SetParent(turretPivot, true);
        if (barrelMesh != null && barrelPivot != null && barrelMesh.parent != barrelPivot)
            barrelMesh.SetParent(barrelPivot, true);
    }
}
