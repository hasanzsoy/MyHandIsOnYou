using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Refs")]
    public SumoGameManager game;        // Inspector’dan atayacaksın
    public RingRules ringRules;         // Inspector’dan atayacaksın
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public GameObject roundEndPanel;
    public TextMeshProUGUI roundEndLabel;
    public TextMeshProUGUI toastText;

    [Header("Score UI")]
    public List<ScoreItemView> scoreItems = new List<ScoreItemView>();
    public Color p1Color = new Color(0.20f,0.75f,1f); // mavi
    public Color p2Color = new Color(1f,0.35f,0.35f); // kırmızı

    float roundTimer;
    bool timerRunning;

    void Awake()
    {
        if (countdownText) countdownText.text = "";
        if (timerText) timerText.text = "";
        if (roundEndPanel) roundEndPanel.SetActive(false);
        if (toastText) { var c = toastText.color; c.a = 0f; toastText.color = c; }
    }

    void OnEnable()
    {
        if (ringRules != null)
        {
            ringRules.OnPlayerEliminated += OnPlayerEliminated;
            ringRules.OnLastPlayerStanding += OnLastPlayerStanding;
        }
        if (game != null)
        {
            game.OnRoundBegin += OnRoundBegin;
            game.OnScoreChanged += OnScoreChanged;
        }
    }

    void OnDisable()
    {
        if (ringRules != null)
        {
            ringRules.OnPlayerEliminated -= OnPlayerEliminated;
            ringRules.OnLastPlayerStanding -= OnLastPlayerStanding;
        }
        if (game != null)
        {
            game.OnRoundBegin -= OnRoundBegin;
            game.OnScoreChanged -= OnScoreChanged;
        }
    }

    void Update()
    {
        if (timerRunning)
        {
            roundTimer += Time.deltaTime;
            if (timerText) timerText.text = FormatTime(roundTimer);
        }
    }

    string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        return $"{m:00}:{s:00}";
    }

    // --- Public API: GameManager çağıracak ---
    public void InitScores(IReadOnlyList<GameObject> players)
    {
        for (int i = 0; i < scoreItems.Count; i++)
        {
            bool has = players != null && i < players.Count && players[i] != null;
            scoreItems[i].gameObject.SetActive(has);
            if (has)
            {
                scoreItems[i].SetName($"P{i+1}");
                scoreItems[i].SetColor(i==0 ? p1Color : p2Color);
                scoreItems[i].SetScore(game != null ? game.GetScore(players[i]) : 0);
            }
        }
    }

    public void ShowCountdown(float prep = 0.8f, int count = 3)
    {
        StopAllCoroutines();
        StartCoroutine(CoCountdown(prep, count));
    }

    IEnumerator CoCountdown(float prep, int count)
    {
        timerRunning = false;
        if (countdownText) countdownText.text = "";
        yield return new WaitForSeconds(prep);

        for (int i = count; i >= 1; i--)
        {
            if (countdownText) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        if (countdownText) countdownText.text = "GO!";
        yield return new WaitForSeconds(0.6f);
        if (countdownText) countdownText.text = "";

        roundTimer = 0f;
        timerRunning = true;
    }

    // Kazananın skorunu doğrudan güncellemek için (SumoGameManager fallback bunu çağırıyor)
public void UpdateScore(GameObject player, int newScore)
{
    if (game == null || player == null || scoreItems == null) return;
    int idx = game.IndexOfPlayer(player);
    if (idx >= 0 && idx < scoreItems.Count)
    {
        scoreItems[idx].SetScore(newScore);
    }
}

// İstersen index ile de çağırabilmek için küçük yardımcı
public void UpdateScore(int playerIndex, int newScore)
{
    if (scoreItems == null) return;
    if (playerIndex >= 0 && playerIndex < scoreItems.Count)
    {
        scoreItems[playerIndex].SetScore(newScore);
    }
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
        yield return new WaitForSeconds(t);
        if (roundEndPanel) roundEndPanel.SetActive(false);
    }

    public void ShowToast(string msg, float showTime = 1.2f)
    {
        if (toastText == null) return;
        StopCoroutine("CoToast");
        StartCoroutine(CoToast(msg, showTime));
    }

    IEnumerator CoToast(string msg, float showTime)
    {
        toastText.text = msg;
        // fade in
        for (float a = 0; a < 1f; a += Time.deltaTime * 6f)
        {
            var c = toastText.color; c.a = Mathf.Clamp01(a); toastText.color = c;
            yield return null;
        }
        // hold
        yield return new WaitForSeconds(showTime);
        // fade out
        for (float a = 1f; a > 0f; a -= Time.deltaTime * 3f)
        {
            var c = toastText.color; c.a = Mathf.Clamp01(a); toastText.color = c;
            yield return null;
        }
        var c2 = toastText.color; c2.a = 0f; toastText.color = c2;
    }

    // ---- Event handlers ----
    void OnRoundBegin()
    {
        ShowCountdown(0.8f, 3);
        if (roundEndPanel) roundEndPanel.SetActive(false);
    }

    void OnPlayerEliminated(GameObject p)
    {
        // Hangi oyuncu düştü?
        if (game == null) return;
        int idx = game.IndexOfPlayer(p);
        if (idx >= 0) ShowToast($"P{idx+1} suya düştü!");
    }

    void OnLastPlayerStanding()
    {
        ShowRoundEnd("ROUND WON!");
    }

    void OnScoreChanged(GameObject p, int newScore)
    {
        if (game == null) return;
        int idx = game.IndexOfPlayer(p);
        if (idx >= 0 && idx < scoreItems.Count)
            scoreItems[idx].SetScore(newScore);
    }
}
