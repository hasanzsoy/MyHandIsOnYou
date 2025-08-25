using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SumoGameManager : MonoBehaviour
{
    public RingRules ringRules;
    public List<Transform> spawnPoints = new();

    // basit skor: hayatta kalana +1
    Dictionary<GameObject, int> scores = new();

    PlayerInputManager pim;
    readonly List<GameObject> players = new();

    void Awake()
    {
        pim = FindAnyObjectByType<PlayerInputManager>();
    }

    void OnEnable()
    {
        if (pim != null)
            PlayerInputManager.playerJoinedEvent += OnPlayerJoined;

        if (ringRules != null)
        {
            ringRules.OnPlayerEliminated += OnPlayerEliminated;
            ringRules.OnLastPlayerStanding += OnLastPlayerStanding;
        }
    }

    void OnDisable()
    {
        if (pim != null)
            PlayerInputManager.playerJoinedEvent -= OnPlayerJoined;

        if (ringRules != null)
        {
            ringRules.OnPlayerEliminated -= OnPlayerEliminated;
            ringRules.OnLastPlayerStanding -= OnLastPlayerStanding;
        }
    }

    void Start()
    {
        // İstersen 2-3 sn join bekleyip otomatik başlat
        StartCoroutine(Co_WarmupThenStart());
    }

    IEnumerator Co_WarmupThenStart()
    {
        yield return new WaitForSeconds(2f); // oyuncular butona bassın
        yield return StartCoroutine(Co_Countdown(3));
        StartRound();
    }

    IEnumerator Co_Countdown(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            Debug.Log(i.ToString());
            yield return new WaitForSeconds(1f);
        }
        Debug.Log("GO!");
    }

    void StartRound()
    {
        // Kontrolleri aç
        foreach (var go in players)
        {
            var pc = go.GetComponent<PlayerController>();
            if (pc) pc.SetControlEnabled(true);
        }

        // Ring başlasın
        if (ringRules) ringRules.StartRound();
    }

    void EndRound(GameObject winner)
    {
        // Kontrolleri kapat
        foreach (var go in players)
        {
            var pc = go.GetComponent<PlayerController>();
            if (pc) pc.SetControlEnabled(false);
        }

        if (winner != null)
        {
            if (!scores.ContainsKey(winner)) scores[winner] = 0;
            scores[winner] += 1;
            Debug.Log($"Round Winner: {winner.name} | Score: {scores[winner]}");
        }
        else
        {
            Debug.Log("No winner (everyone fell?)");
        }

        // Burada genel oyun akışına skorları göndereceğiz (minigame framework'üne hook).
        // Şimdilik tek round. İstersen yeniden başlat:
        // StartCoroutine(Co_RestartRound());
    }

    IEnumerator Co_RestartRound()
    {
        yield return new WaitForSeconds(2f);

        // herkesi ringe geri koy + aktif et
        for (int i = 0; i < players.Count; i++)
        {
            var go = players[i];
            if (go == null) continue;
            go.SetActive(true);
            PositionAtSpawn(go, i);
            var rb = go.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity = Vector3.zero;

            var pc = go.GetComponent<PlayerController>();
            if (pc) pc.SetControlEnabled(false);

            if (ringRules) ringRules.RegisterPlayer(go);
        }

        if (ringRules) ringRules.ringRadius = 6f;

        yield return StartCoroutine(Co_Countdown(3));
        StartRound();
    }

    void OnPlayerJoined(PlayerInput input)
    {
        var go = input.gameObject;
        players.Add(go);

        int idx = players.Count - 1;
        PositionAtSpawn(go, idx);

        // oyuna kayıt
        if (ringRules) ringRules.RegisterPlayer(go);

        // round başlamadan kontroller kapalı
        var pc = go.GetComponent<PlayerController>();
        if (pc) pc.SetControlEnabled(false);

        // isim farklılaştır
        go.name = $"Player_{idx + 1}";
    }

    void PositionAtSpawn(GameObject go, int playerIndex)
    {
        if (spawnPoints.Count == 0) { go.transform.position = Vector3.up; return; }
        var t = spawnPoints[playerIndex % spawnPoints.Count];
        go.transform.SetPositionAndRotation(t.position, t.rotation);
    }

    void OnPlayerEliminated(GameObject go)
    {
        Debug.Log($"{go.name} eliminated");
    }

    void OnLastPlayerStanding()
    {
        // ringRules içindeki alive listesini kullanmadık, winner'ı biz tespit edelim
        GameObject winner = null;
        foreach (var go in players)
        {
            if (go != null && go.activeSelf)
            {
                winner = go;
                break;
            }
        }
        EndRound(winner);
    }
}
