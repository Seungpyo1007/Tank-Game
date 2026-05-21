using UnityEngine;
using UnityEngine.InputSystem;

public class Gunner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Rigidbody hullRb;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private ShellData[] shells;

    [Header("Firing")]
    [SerializeField] private float reloadTime = 8f;
    [SerializeField] private float recoilImpulse = 2400f;
    [SerializeField] private bool playerControlled = true;

    [Header("Modules")]
    [SerializeField] private TankModules modules;

    [Header("Muzzle Flash")]
    [SerializeField] private Material muzzleFlashMaterial;
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Material smokeMaterial;
    [SerializeField] private float muzzleFlashDuration = 0.18f;
    [SerializeField] private float muzzleFlashIntensity = 35f;

    private int currentShellIndex;
    private float lastFireTime = -999f;

    public float ReloadProgress01 => Mathf.Clamp01((Time.time - lastFireTime) / Mathf.Max(0.01f, reloadTime));
    public bool IsReloading => Time.time - lastFireTime < reloadTime;
    public float ReloadTime => reloadTime;
    public float ReloadRemaining => Mathf.Max(0f, reloadTime - (Time.time - lastFireTime));
    public bool PlayerControlled { get => playerControlled; set => playerControlled = value; }
    public ShellData CurrentShell => (shells != null && shells.Length > 0) ? shells[currentShellIndex] : null;
    public int CurrentShellIndex => currentShellIndex;
    public int ShellCount => shells != null ? shells.Length : 0;

    void Update()
    {
        if (!playerControlled) return;

        var kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame) CycleShell();

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) TryFire();
    }

    public void CycleShell()
    {
        if (shells == null || shells.Length == 0) return;
        currentShellIndex = (currentShellIndex + 1) % shells.Length;
    }

    public void SelectShell(int index)
    {
        if (shells == null || shells.Length == 0) return;
        currentShellIndex = Mathf.Clamp(index, 0, shells.Length - 1);
    }

    public bool TryFire()
    {
        if (IsReloading) return false;
        if (modules != null && !modules.GunWorks) return false;
        if (muzzle == null || shellPrefab == null || CurrentShell == null) return false;

        GameObject go = Instantiate(shellPrefab, muzzle.position, Quaternion.LookRotation(muzzle.forward));
        Vector3 carrierVel = hullRb != null ? hullRb.linearVelocity : Vector3.zero;

        var shell = go.GetComponent<Shell>();
        if (shell != null)
        {
            var ignore = GetComponentsInChildren<Collider>(true);
            shell.IsPlayerShell = playerControlled;
            shell.Initialize(CurrentShell, muzzle.position, muzzle.forward, carrierVel, ignore);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = carrierVel + muzzle.forward * CurrentShell.muzzleVelocity;
        }

        if (hullRb != null)
            hullRb.AddForceAtPosition(-muzzle.forward * recoilImpulse, muzzle.position, ForceMode.Impulse);

        SpawnMuzzleFlash();

        lastFireTime = Time.time;
        if (playerControlled && GameStats.Instance != null) GameStats.Instance.shotsFired++;
        return true;
    }

    void SpawnMuzzleFlash()
    {
        if (muzzle == null) return;
        var go = new GameObject("MuzzleFlashFX");
        go.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
        go.transform.SetParent(muzzle, true);
        var mf = go.AddComponent<MuzzleFlashFX>();
        mf.flashCoreMaterial = muzzleFlashMaterial;
        mf.sparkMaterial = sparkMaterial;
        mf.smokeMaterial = smokeMaterial;
        mf.lightDuration = muzzleFlashDuration;
        mf.lightIntensity = muzzleFlashIntensity;
    }
}
