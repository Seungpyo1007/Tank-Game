using UnityEngine;

public class GameStats : MonoBehaviour
{
    public static GameStats Instance { get; private set; }

    public int kills;
    public int shotsFired;
    public int shotsHit;
    public int penetrations;
    public float damageDealt;
    public float damageTaken;
    public float startTime;

    public float ElapsedTime => Time.unscaledTime - startTime;
    public float Accuracy => shotsFired > 0 ? (float)shotsHit / shotsFired : 0f;

    void Awake()
    {
        Instance = this;
        startTime = Time.unscaledTime;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
