using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public float ignoreKillUntil; // Time.time ile karşılaştırılacak

    public void SetSpawnProtection(float seconds)
    {
        ignoreKillUntil = Time.time + Mathf.Max(0f, seconds);
    }

    public bool CanBeKilled()
    {
        return Time.time >= ignoreKillUntil;
    }
}
