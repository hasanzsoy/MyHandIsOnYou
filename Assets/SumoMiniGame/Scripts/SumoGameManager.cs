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
    private Dictionary<GameObject, int> scores = new Dictionary<GameObject, int>();
    private readonly List<GameObject> players = new List<GameObject>();

    void Awake()
    {
        pim = FindObjectOfType<PlayerInputManager>();
        if (pim == null)
            Debug.LogWarning("PlayerInputManager yok. 'Systems' objesine ekleyip Player Prefab'ı PF_Player yap.");
    }

    void OnEnable()
    {
        if (pim != null)
            pim.playerJoinedEvent.AddListener(OnPlayerJoined);   // ✅ UnityEvent

        if (ringRules != null)
        {
            ringRules.OnPlayerEliminated += OnPlayerEliminated;
            ringRules.OnLastPlayerStanding += OnLastPlayerStanding;
        }
    }

    void OnDisable()
    {
        if (pim != null)
            pim.playerJoinedEvent.RemoveListener(OnPlayerJoined); // ✅ UnityEvent

        if (ringRules != null)
        {
            ringRules.OnPlayerEliminated -= OnPlayerEliminated;
            ringRules.OnLastPlayerStanding -= OnLastPlayerStanding;
        }
    }

    void Start() { StartRound(); }

    void StartRound()
    {
        foreach (var go in players)
        {
            var pc = go ? go.GetComponent<PlayerController>() : null;
            if (pc) pc.SetControlEnabled(true);
        }
        if (ringRules) ringRules.StartRound();
    }

    void EndRound(GameObject winner)
    {
        foreach (var go in players)
        {
            var pc = go ? go.GetComponent<PlayerController>() : null;
            if (pc) pc.SetControlEnabled(false);
        }
        if (winner != null)
        {
            if (!scores.ContainsKey(winner)) scores[winner] = 0;
            scores[winner] += 1;
            Debug.Log("Round Winner: " + winner.name + " | Score: " + scores[winner]);
        }
        else Debug.Log("No winner");
    }

    // -- JOIN CALLBACK --
    void OnPlayerJoined(PlayerInput input)
    {
        var go = input.gameObject;
        players.Add(go);

        int idx = players.Count - 1;
        PositionAtSpawn(go, idx);

        if (ringRules) ringRules.RegisterPlayer(go);

        var pc = go.GetComponent<PlayerController>();
        if (pc) pc.SetControlEnabled(true);

        go.name = "Player_" + (idx + 1);
    }

    void PositionAtSpawn(GameObject go, int playerIndex)
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            var t = spawnPoints[playerIndex % spawnPoints.Count];
            go.transform.SetPositionAndRotation(t.position, t.rotation);
        }
        else go.transform.position = Vector3.up;

        var rb = go.GetComponent<Rigidbody>();
        if (rb) rb.velocity = Vector3.zero;
    }

    void OnPlayerEliminated(GameObject go) { Debug.Log(go.name + " eliminated"); }

    void OnLastPlayerStanding()
    {
        GameObject winner = null;
        foreach (var go in players) if (go && go.activeSelf) { winner = go; break; }
        EndRound(winner);
    }
}
