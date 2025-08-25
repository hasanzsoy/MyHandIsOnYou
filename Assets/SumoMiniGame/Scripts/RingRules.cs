using System;
using System.Collections.Generic;
using UnityEngine;

public class RingRules : MonoBehaviour
{
    public Transform center;
    public float ringRadius = 6f;
    public bool shrinkOverTime = true;
    public float shrinkPerSecond = 0.2f; // saniyede 0.2 m küçül

    public event Action<GameObject> OnPlayerEliminated;
    public event Action OnLastPlayerStanding; // round biter

    readonly List<GameObject> alive = new();
    bool roundRunning;


    [Header("Auto")]
    public Transform ringTransform;
    public bool autoRadiusFromScale = true;

    void Start()
    {
        if (center == null)
        {
            var c = new GameObject("RingCenter");
            c.transform.position = Vector3.zero;
            center = c.transform;
        }
    }

    public void RegisterPlayer(GameObject go)
    {
        if (!alive.Contains(go))
            alive.Add(go);
    }

    public void DeregisterPlayer(GameObject go)
    {
        alive.Remove(go);
    }

    public void StartRound()
    {
        roundRunning = true;
    }

    public void StopRound()
    {
        roundRunning = false;
    }

    void Update()
    {
        if (!roundRunning) return;

        if (shrinkOverTime && ringRadius > 2.5f)
            ringRadius -= shrinkPerSecond * Time.deltaTime;

        // Dışarı çıkanları bul
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            var p = alive[i];
            if (p == null) { alive.RemoveAt(i); continue; }

            float dist = Vector3.Distance(new Vector3(p.transform.position.x, 0, p.transform.position.z),
                                          new Vector3(center.position.x, 0, center.position.z));
            if (dist > ringRadius)
            {
                // elendi
                OnPlayerEliminated?.Invoke(p);
                alive.RemoveAt(i);
                p.SetActive(false);
            }
        }

        if (alive.Count <= 1)
        {
            roundRunning = false;
            OnLastPlayerStanding?.Invoke();
        }
    }

    void LateUpdate()
    {
        if (autoRadiusFromScale && ringTransform != null)
        {
            // Daire benzeri ring için: yarıçap ≈ scale.x * 0.5
            ringRadius = ringTransform.lossyScale.x * 0.5f;
        }
    }

    // Debug için sahnede ringi çiz
    void OnDrawGizmos()
    {
        if (center == null) return;
        Gizmos.color = Color.yellow;
        DrawWireCircle(center.position, Vector3.up, ringRadius, 64);
    }

    static void DrawWireCircle(Vector3 center, Vector3 normal, float radius, int segments)
    {
        Vector3 prev = center + Quaternion.AngleAxis(0, normal) * (Vector3.forward * radius);
        for (int i = 1; i <= segments; i++)
        {
            float angle = 360f * i / segments;
            Vector3 next = center + Quaternion.AngleAxis(angle, normal) * (Vector3.forward * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
    
    public void ForceEliminate(GameObject p)
{
    OnPlayerEliminated?.Invoke(p);

    // alive listesinden çıkar
    for (int i = alive.Count - 1; i >= 0; i--)
        if (alive[i] == p) alive.RemoveAt(i);

    // 1 veya 0 kaldıysa round'u bitir
    if (alive.Count <= 1)
    {
        StopRound();
        OnLastPlayerStanding?.Invoke();
    }
}

}
