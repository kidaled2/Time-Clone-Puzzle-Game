using TMPro;
using UnityEngine;

public class TurnTimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorWarning = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color colorCritical = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float warningThreshold = 20f;
    [SerializeField] private float criticalThreshold = 10f;

    public void UpdateDisplay(float secondsRemaining)
    {
        if (timerText == null)
        {
            return;
        }

        int seconds = Mathf.CeilToInt(secondsRemaining);
        timerText.text = $"Time Left: {seconds} seconds";

        if (secondsRemaining <= criticalThreshold)
        {
            timerText.color = colorCritical;
        }
        else if (secondsRemaining <= warningThreshold)
        {
            timerText.color = colorWarning;
        }
        else
        {
            timerText.color = colorNormal;
        }
    }

    public void Hide() => gameObject.SetActive(false);

    public void Show() => gameObject.SetActive(true);
}
