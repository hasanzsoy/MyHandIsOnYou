using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Refs")]
    public SumoGameManager game;        // Inspector’dan atayacaksın
    public RingRules ringRules;         // Sadece toast için opsiyonel
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public GameObject roundEndPanel;
    public TextMeshProUGUI roundEndLabel;
    public TextMeshProUGUI toastText;

    [Header("Score UI")]
    public List<ScoreItemView> scoreItems = new List<ScoreItemView>();
    public Color p1Color = new Color(0.20f, 0.75f, 1f); // fallback mavi
    public Color p2Color = new Color(1f, 0.35f, 0.35f); // fallback kırmızı

    [Header("Options")]
    [Tooltip("Sadece bilgilendirme: oyuncu elenince küçük toast göster.")]
    public bool listenRingElimsForToast = true;

    float roundTimer;
    bool timerRunning;

    void Awake()
    {
        if (countdownText) countdownText.text = "";
        if (timerText) timerText.text = "";
        if (roundEndPanel) roundEndPanel.SetActive(false);
        if (toastText)
        {
            var c = toastText.color; c.a = 0f; toastText.color = c;
        }
    }

    void OnEnable()
    {
        // Round akışını SADECE GameManager'dan dinle
        if (game != null)
        {
            game.OnRoundBegin += OnRoundBegin;
            game.OnScoreChanged += OnScoreChanged;
        }

        // RingRules: Sadece ELENME için toast (round end dinlenmez!)
        if (ringRules != null && listenRingElimsForToast)
            ringRules.OnPlayerEliminated += HandlePlayerEliminatedToast;
    }

    void OnDisable()
    {
        if (game != null)
        {
            game.OnRoundBegin -= OnRoundBegin;
            game.OnScoreChanged -= OnScoreChanged;
        }
        if (ringRules != null && listenRingElimsForToast)
            ringRules.OnPlayerEliminated -= HandlePlayerEliminatedToast;
    }

    void Update()
    {
        if (timerRunning)
        {
            roundTimer += Time.deltaTime;
            if (timerText) timerText.text = $"{(int)(roundTimer/60):00}:{(int)(roundTimer%60):00}";
        }
    }

    // -------- Public API (GameManager çağırır) --------
    public void InitScores(IReadOnlyList<GameObject> players)
    {
        for (int i = 0; i < scoreItems.Count; i++)
        {
            bool has = players != null && i < players.Count && players[i] != null;
            scoreItems[i].gameObject.SetActive(has);
            if (has)
            {
                scoreItems[i].SetName($"P{i + 1}");

                // Renk: önce GameManager.playerColors, yoksa p1/p2 fallback
                Color col = (game != null && game.playerColors != null && game.playerColors.Length > 0)
                    ? game.playerColors[i % game.playerColors.Length]
                    : (i == 0 ? p1Color : p2Color);
                scoreItems[i].SetColor(col);

                scoreItems[i].SetScore(game != null ? game.GetScore(players[i]) : 0);
            }
        }
    }

    public void UpdateScore(GameObject player, int newScore)
    {
        if (game == null || player == null) return;
        int idx = game.IndexOfPlayer(player);
        if (0 <= idx && idx < scoreItems.Count) scoreItems[idx].SetScore(newScore);
    }

    public void UpdateScore(int playerIndex, int newScore)
    {
        if (0 <= playerIndex && playerIndex < scoreItems.Count)
            scoreItems[playerIndex].SetScore(newScore);
    }

    public void ShowCountdown(float prep = 0.8f, int count = 3)
    {
        StopAllCoroutines();
        StartCoroutine(CoCountdownRealtime(prep, count));
    }

    IEnumerator CoCountdownRealtime(float prep, int count)
    {
        timerRunning = false;
        roundTimer = 0f;

        if (countdownText) countdownText.text = "";
        yield return new WaitForSecondsRealtime(prep);

        for (int i = count; i >= 1; i--)
        {
            if (countdownText) countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        if (countdownText) countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.6f);
        if (countdownText) countdownText.text = "";

        timerRunning = true;
    }

    public void ShowRoundEnd(string label = "ROUND WON!")
    {
        timerRunning = false;
        if (roundEndPanel) roundEndPanel.SetActive(true);
        if (roundEndLabel) roundEndLabel.text = label;
        StartCoroutine(CoHideRoundEnd(1.5f));
    }

    IEnumerator CoHideRoundEnd(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        if (roundEndPanel) roundEndPanel.SetActive(false);
    }

    public void ShowToast(string msg, float showTime = 1.2f)
    {
        if (toastText == null) return;
        StopCoroutine(nameof(CoToast));
        StartCoroutine(CoToast(msg, showTime));
    }

    IEnumerator CoToast(string msg, float showTime)
    {
        toastText.text = msg;

        // fade in
        for (float a = 0; a < 1f; a += Time.unscaledDeltaTime * 6f)
        {
            var c = toastText.color; c.a = Mathf.Clamp01(a); toastText.color = c;
            yield return null;
        }

        // hold
        yield return new WaitForSecondsRealtime(showTime);

        // fade out
        for (float a = 1; a > 0f; a -= Time.unscaledDeltaTime * 3f)
        {
            var c = toastText.color; c.a = Mathf.Clamp01(a); toastText.color = c;
            yield return null;
        }

        var c2 = toastText.color; c2.a = 0f; toastText.color = c2;
    }

    // -------- Event handlers --------
    void OnRoundBegin()
    {
        if (roundEndPanel) roundEndPanel.SetActive(false);
        ShowCountdown(0.8f, 3);
    }

    void OnScoreChanged(GameObject p, int newScore) => UpdateScore(p, newScore);

    // Sadece toast için (round end UI’sini GameManager açacak!)
    void HandlePlayerEliminatedToast(GameObject p)
    {
        if (game == null || p == null) return;
        int idx = game.IndexOfPlayer(p);
        if (idx >= 0) ShowToast($"P{idx + 1} suya düştü!");
    }
}
