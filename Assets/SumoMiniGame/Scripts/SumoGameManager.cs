using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SumoGameManager : MonoBehaviour
{
    [Header("References")]
    public RingRules ringRules;
    public List<Transform> spawnPoints = new List<Transform>();

    private PlayerInputManager pim;

    // Oyuncular & skorlar
    private readonly List<GameObject> players = new List<GameObject>();
    private readonly Dictionary<GameObject, int> scores = new Dictionary<GameObject, int>();

    private int nextSpawnSlot = 0; // her yeni oyuncu farklı spawn'a gitsin

    [Header("Player Colors (slot sırasına göre)")]
    public Color[] playerColors = new Color[]
    {
        new Color(0.20f, 0.75f, 1.00f), // P1: mavi-cyan
        new Color(1.00f, 0.35f, 0.35f), // P2: kırmızı
        new Color(0.30f, 1.00f, 0.50f), // P3: yeşil
        new Color(1.00f, 0.85f, 0.30f), // P4: sarı
    };

    [Header("Round Flow")]
    [Tooltip("Geri sayım toplam bekleme (UI 0.8 prep + 3 sn geri sayım varsayıyoruz).")]
    public float countdownTotalSeconds = 3.6f;
    public float spawnProtectionSeconds = 0.75f;

    private bool roundActive = false;

    // --- UI event'leri (GameUI kullanıyorsa dinler) ---
    public event Action OnRoundBegin;                         // Round başlarken UI countdown
    public event Action<GameObject, int> OnScoreChanged;      // Skor değişince UI update

    // --- UI'nin okuyacağı yardımcılar ---
    public IReadOnlyList<GameObject> Players => players;
    public int IndexOfPlayer(GameObject go) => players.IndexOf(go);
    public int GetScore(GameObject go) => scores.TryGetValue(go, out var s) ? s : 0;

    void Awake()
    {
        #if UNITY_2023_1_OR_NEWER
        pim = FindFirstObjectByType<PlayerInputManager>();
        #else
        pim = FindObjectOfType<PlayerInputManager>();
        #endif

        if (pim == null)
            Debug.LogWarning("PlayerInputManager bulunamadı. 'Systems' objesine PlayerInputManager ekleyin ve Player Prefab'ı PF_Player yapın.");

        if (ringRules == null)
        {
            #if UNITY_2023_1_OR_NEWER
            ringRules = FindFirstObjectByType<RingRules>();
            #else
            ringRules = FindObjectOfType<RingRules>();
            #endif
        }
    }

    void OnEnable()
    {
        if (pim != null)
            pim.playerJoinedEvent.AddListener(OnPlayerJoined);

        if (ringRules != null)
        {
            ringRules.OnPlayerEliminated += OnPlayerEliminated;
            ringRules.OnLastPlayerStanding += OnLastPlayerStanding;
        }
    }

    void OnDisable()
    {
        if (pim != null)
            pim.playerJoinedEvent.RemoveListener(OnPlayerJoined);

        if (ringRules != null)
        {
            ringRules.OnPlayerEliminated -= OnPlayerEliminated;
            ringRules.OnLastPlayerStanding -= OnLastPlayerStanding;
        }
    }

    void Start()
    {
        StartRound();
    }

    // ----------------------- ROUND AKIŞI -----------------------
    public void StartRound()
    {
        StopAllCoroutines();
        StartCoroutine(CoStartRound());
    }

    IEnumerator CoStartRound()
    {
        roundActive = false;

        // Elenmiş olanları geri aç + freeze
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p == null) continue;
            if (!p.activeSelf) p.SetActive(true);

            var pc = p.GetComponent<PlayerController>();
            if (pc) pc.SetControlEnabled(false);

            var rb = p.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // UI'ya haber ver: geri sayım başlasın
        OnRoundBegin?.Invoke();

        // Countdown tamamlanana kadar bekle (unscaled: pause etkilenmez)
        float t = countdownTotalSeconds;
        while (t > 0f)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            t -= 0.1f;
        }

        // Kontrolleri aç, round'u başlat
        foreach (var go in players)
            go?.GetComponent<PlayerController>()?.SetControlEnabled(true);

        ringRules?.StartRound();
        roundActive = true;
    }

    /// <summary>
    /// Round bittiğinde skor yazar, UI'yı açar, kontrolleri kapatır. TEK MERKEZ.
    /// </summary>
    void EndRound(GameObject winner)
    {
        roundActive = false;

        // Kontrolleri kapat
        foreach (var go in players)
        {
            var pc = go ? go.GetComponent<PlayerController>() : null;
            if (pc) pc.SetControlEnabled(false);
        }

        // --- SKOR & UI ---
        if (winner != null)
        {
            if (!scores.ContainsKey(winner)) scores[winner] = 0;
            scores[winner] += 1;

            // Event ile UI'ya haber ver (bağlıysa)
            OnScoreChanged?.Invoke(winner, scores[winner]);
            Debug.Log($"Round Winner: {winner.name} | Score: {scores[winner]}");

            // Fallback: Event bağlanmadıysa UI'yı doğrudan güncelle
            var ui = SafeFindUI();
            if (ui != null)
            {
                // Kazananın skorunu anında UI'a bas
                ui.UpdateScore(winner, scores[winner]);
                ui.ShowRoundEnd("ROUND WON!");
            }
        }
        else
        {
            Debug.Log("No winner");
            // Yine de UI'ya round end göster
            var ui = SafeFindUI();
            if (ui != null) ui.ShowRoundEnd("ROUND OVER");
        }
    }

    public void RestartRound()
    {
        // Tüm oyuncuları respawn + freeze
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p == null) continue;

            if (!p.activeSelf) p.SetActive(true);

            p.GetComponent<PlayerController>()?.SetControlEnabled(false);
            PositionAtSpawn(p, i);
            ApplySpawnProtection(p);

            var rb = p.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        StopAllCoroutines();
        StartCoroutine(CoRestartDelay());
    }

    IEnumerator CoRestartDelay()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        StartRound();
    }

    // ------------------- JOIN & SPAWN -------------------
    void OnPlayerJoined(PlayerInput input)
    {
        var go = input.gameObject;

        players.Add(go);
        if (!scores.ContainsKey(go)) scores[go] = 0; // <<< skoru 0'dan başlat

        int idx = nextSpawnSlot % Mathf.Max(1, spawnPoints.Count);
        nextSpawnSlot++;

        PositionAtSpawn(go, idx);
        ApplyColorBySlot(go, idx);
        ApplySpawnProtection(go);

        if (ringRules != null) ringRules.RegisterPlayer(go);

        var pc = go.GetComponent<PlayerController>();
        if (pc != null) pc.SetControlEnabled(true);

        go.name = $"Player_{idx + 1}";

        // UI ilk skor/renk senkronu
        var ui = SafeFindUI();
        if (ui != null) ui.InitScores(Players);
    }

    void PositionAtSpawn(GameObject go, int idx)
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            go.transform.position = Vector3.up * 3f;
            return;
        }

        const float spawnHeightY = 3.0f;
        const float sepRadius    = 1.4f;
        const float stepDeg      = 30f;
        const float baseDist     = 1.6f;
        const int   ringCount    = 3;

        Transform t = spawnPoints[idx % spawnPoints.Count];
        Vector3 basePos = t.position; basePos.y = spawnHeightY;

        Vector3 finalPos = FindFreeSpotNear(basePos, sepRadius, stepDeg, baseDist, ringCount);
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

    Vector3 FindFreeSpotNear(Vector3 basePos, float radius, float stepDeg, float baseDist, int ringCount)
    {
        if (!IsOccupiedByPlayer(basePos, radius)) return basePos;

        for (int ring = 1; ring <= ringCount; ring++)
        {
            float r = baseDist * ring;
            for (float ang = 0f; ang < 360f; ang += stepDeg)
            {
                float rad = ang * Mathf.Deg2Rad;
                Vector3 p = basePos + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * r;
                p.y = basePos.y;
                if (!IsOccupiedByPlayer(p, radius)) return p;
            }
        }
        return basePos;
    }

    bool IsOccupiedByPlayer(Vector3 pos, float radius)
    {
        var hits = Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            var rb = h.attachedRigidbody;
            if (rb != null && rb.GetComponent<PlayerController>() != null)
                return true;
        }
        return false;
    }

    void ApplyColorBySlot(GameObject go, int slot)
    {
        var appear = go.GetComponentInChildren<PlayerAppearance>();
        if (appear == null || playerColors == null || playerColors.Length == 0) return;

        var col = playerColors[slot % playerColors.Length];
        appear.ApplyColor(col);
    }

    void ApplySpawnProtection(GameObject go)
    {
        var st = go.GetComponent<PlayerState>();
        if (st != null)
            st.SetSpawnProtection(spawnProtectionSeconds);
    }

    GameUI SafeFindUI()
    {
        #if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<GameUI>();
        #else
        return FindObjectOfType<GameUI>();
        #endif
    }

    // ------------------- RING RULES CALLBACKS -------------------
    void OnPlayerEliminated(GameObject go)
    {
        Debug.Log(go.name + " eliminated");
        // İstersen burada küçük bir toast göstermek için:
        // SafeFindUI()?.ShowToast($"{go.name} suya düştü!");
    }

    void OnLastPlayerStanding()
    {
        if (!roundActive) return; // yanlış/erken tetiklenmeyi yoksay

        // Sahnedeki aktif oyunculardan kazananı bul
        GameObject winner = null;
        foreach (var go in players)
            if (go && go.activeSelf) { winner = go; break; }

        EndRound(winner);
    }
}
