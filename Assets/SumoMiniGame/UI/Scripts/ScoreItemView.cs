using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreItemView : MonoBehaviour
{
    public Image colorDot;
    public TextMeshProUGUI playerLabel;
    public TextMeshProUGUI scoreLabel;

    public void SetName(string n)  { if (playerLabel) playerLabel.text = n; }
    public void SetScore(int s)    { if (scoreLabel) scoreLabel.text = s.ToString(); }
    public void SetColor(Color c)  { if (colorDot) colorDot.color = c; }
}
