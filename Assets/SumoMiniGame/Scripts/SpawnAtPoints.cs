using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnAtPoints : MonoBehaviour
{
    [Header("Spawn Points (sırayla)")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Yerleşim")]
    [Tooltip("Spawn yüksekliği (Y). Spawn noktalarının Y'si bununla değiştirilecek.")]
    public float spawnHeightY = 3f;

    [Tooltip("Yeni oyuncuyu bu mesafede başka oyuncunun üstüne koyma.")]
    public float separationRadius = 1.2f;

    [Tooltip("Yer doluysa aramada kaç derecelik adımlarla dönelim?")]
    public float searchStepDeg = 30f;

    [Tooltip("Yer doluysa ilk arama halkasının yarıçapı.")]
    public float searchBaseDist = 1.5f;

    [Tooltip("Kaç halka denensin?")]
    public int searchRings = 3;

    [Header("Otomatik yaratma (PlayerInputManager yoksa)")]
    public bool autoSpawnOnStart = false;
    public GameObject playerPrefab;
    public int initialPlayers = 2;

    PlayerInputManager pim;
    int nextIndex = 0;

    void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        pim = FindFirstObjectByType<PlayerInputManager>();
#else
        pim = FindObjectOfType<PlayerInputManager>();
#endif
    }

    void OnEnable()
    {
        if (pim != null)
            pim.playerJoinedEvent.AddListener(OnPlayerJoined);
    }

    void OnDisable()
    {
        if (pim != null)
            pim.playerJoinedEvent.RemoveListener(OnPlayerJoined);
    }

    void Start()
    {
        // PlayerInputManager yoksa ve otomatik istiyorsan sahne başında üret
        if (pim == null && autoSpawnOnStart && playerPrefab != null)
        {
            for (int i = 0; i < Mathf.Max(1, initialPlayers); i++)
            {
                var go = Instantiate(playerPrefab);
                PlaceAt(go, i);
            }
        }
    }

    // PlayerInputManager ile tuşa basınca gelen oyuncular burada konumlanır
    void OnPlayerJoined(PlayerInput input)
    {
        var go = input.gameObject;

        int idx = nextIndex % Mathf.Max(1, spawnPoints.Count);
        nextIndex++;

        PlaceAt(go, idx);
    }

    // Dışarıdan da çağırabil (örn. round restart’ta)
    public void PlaceAt(GameObject go, int idx)
    {
        if (spawnPoints.Count == 0)
        {
            go.transform.position = new Vector3(0, spawnHeightY, 0);
            return;
        }

        Transform t = spawnPoints[idx % spawnPoints.Count];
        Vector3 basePos = t.position; basePos.y = spawnHeightY;

        Vector3 finalPos = FindFreeSpotNear(basePos, separationRadius, searchStepDeg, searchBaseDist, searchRings);
        Quaternion rot = t.rotation;

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = finalPos;
            rb.rotation = rot;
            Physics.SyncTransforms();
            rb.WakeUp();
        }
        else
        {
            go.transform.SetPositionAndRotation(finalPos, rot);
            Physics.SyncTransforms();
        }
    }

    Vector3 FindFreeSpotNear(Vector3 basePos, float radius, float stepDeg, float baseDist, int rings)
    {
        if (!Occupied(basePos, radius)) return basePos;

        for (int r = 1; r <= rings; r++)
        {
            float dist = baseDist * r;
            for (float ang = 0; ang < 360f; ang += stepDeg)
            {
                float rad = ang * Mathf.Deg2Rad;
                Vector3 p = basePos + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * dist;
                p.y = basePos.y;
                if (!Occupied(p, radius)) return p;
            }
        }
        return basePos; // yer bulamadıysa en azından base'e koy
    }

    bool Occupied(Vector3 pos, float radius)
    {
        // Bu yarıçapta başka PlayerController var mı?
        var hits = Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            var rb = h.attachedRigidbody;
            if (rb != null && rb.GetComponent<PlayerController>() != null)
                return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (var t in spawnPoints)
        {
            if (t == null) continue;
            var p = t.position; p.y = spawnHeightY;
            Gizmos.DrawWireSphere(p, separationRadius);
            Gizmos.DrawLine(t.position + Vector3.up * 0.1f, p);
        }
    }
}
