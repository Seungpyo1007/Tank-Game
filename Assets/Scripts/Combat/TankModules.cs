using UnityEngine;

public class TankModules : MonoBehaviour
{
    [Header("Engine")]
    [SerializeField] private float engineMaxHP = 100f;
    [SerializeField] private float engineHP;
    [Header("Gun (breech)")]
    [SerializeField] private float gunMaxHP = 80f;
    [SerializeField] private float gunHP;
    [Header("Left Track")]
    [SerializeField] private float leftTrackMaxHP = 100f;
    [SerializeField] private float leftTrackHP;
    [Header("Right Track")]
    [SerializeField] private float rightTrackMaxHP = 100f;
    [SerializeField] private float rightTrackHP;

    public bool EngineWorks => engineHP > 0f;
    public bool GunWorks => gunHP > 0f;
    public bool LeftTrackWorks => leftTrackHP > 0f;
    public bool RightTrackWorks => rightTrackHP > 0f;

    public float EnginePerformance => Mathf.Clamp01(engineHP / Mathf.Max(0.01f, engineMaxHP));
    public float LeftTrackPerformance => Mathf.Clamp01(leftTrackHP / Mathf.Max(0.01f, leftTrackMaxHP));
    public float RightTrackPerformance => Mathf.Clamp01(rightTrackHP / Mathf.Max(0.01f, rightTrackMaxHP));

    public float EngineMaxHP => engineMaxHP;
    public float GunMaxHP => gunMaxHP;
    public float LeftTrackMaxHP => leftTrackMaxHP;
    public float RightTrackMaxHP => rightTrackMaxHP;
    public float EngineHP => engineHP;
    public float GunHP => gunHP;
    public float LeftTrackHP => leftTrackHP;
    public float RightTrackHP => rightTrackHP;

    public System.Action<string> OnModuleDestroyed;

    void Awake()
    {
        engineHP = engineMaxHP;
        gunHP = gunMaxHP;
        leftTrackHP = leftTrackMaxHP;
        rightTrackHP = rightTrackMaxHP;
    }

    public void DamageEngine(float amount) { engineHP = Damage(engineHP, amount, "ENGINE"); }
    public void DamageGun(float amount) { gunHP = Damage(gunHP, amount, "GUN"); }
    public void DamageLeftTrack(float amount) { leftTrackHP = Damage(leftTrackHP, amount, "LEFT TRACK"); }
    public void DamageRightTrack(float amount) { rightTrackHP = Damage(rightTrackHP, amount, "RIGHT TRACK"); }

    float Damage(float current, float amount, string label)
    {
        bool wasAlive = current > 0f;
        current = Mathf.Max(0f, current - amount);
        if (wasAlive && current <= 0f) OnModuleDestroyed?.Invoke(label);
        return current;
    }
}
