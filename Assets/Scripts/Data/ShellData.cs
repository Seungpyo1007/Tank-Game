using UnityEngine;

public enum ShellType { AP, HE }

[CreateAssetMenu(fileName = "ShellData", menuName = "TankGame/Shell Data")]
public class ShellData : ScriptableObject
{
    public ShellType type = ShellType.AP;
    public string displayName = "AP";
    public float muzzleVelocity = 900f;
    public float mass = 15f;
    public float damage = 600f;
    public float penetrationMm = 200f;
    public float explosionRadius = 0f;
    public float explosionForce = 0f;
    public Color tracerColor = Color.yellow;
}
