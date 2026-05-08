using TMPro;
using System.Collections;
using UnityEngine;

#if DOTWEEN
using DG.Tweening;
#endif

public class TurnTimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorWarning = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color colorCritical = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float warningThreshold = 20f;
    [SerializeField] private float criticalThreshold = 10f;
    [SerializeField] private float criticalColorLerpDuration = 1f;
    [SerializeField] private float criticalPunchScale = 0.15f;

    private RectTransform timerRectTransform;
    private float criticalBlend;
    private int lastCriticalPulseSecond = int.MinValue;
    private Coroutine punchRoutine;

#if DOTWEEN
    private Tween punchTween;
#endif

    private void Awake()
    {
        if (timerText != null)
        {
            timerRectTransform = timerText.rectTransform;
        }
    }

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
            criticalBlend = Mathf.MoveTowards(
                criticalBlend,
                1f,
                Time.deltaTime / Mathf.Max(0.001f, criticalColorLerpDuration));
            timerText.color = Color.Lerp(colorNormal, colorCritical, criticalBlend);
            PulseCriticalSecond(seconds);
        }
        else if (secondsRemaining <= warningThreshold)
        {
            criticalBlend = 0f;
            lastCriticalPulseSecond = int.MinValue;
            timerText.color = colorWarning;
        }
        else
        {
            criticalBlend = 0f;
            lastCriticalPulseSecond = int.MinValue;
            timerText.color = colorNormal;
        }
    }

    public void Hide() => gameObject.SetActive(false);

    public void Show() => gameObject.SetActive(true);

    private void PulseCriticalSecond(int seconds)
    {
        if (timerRectTransform == null || seconds == lastCriticalPulseSecond)
        {
            return;
        }

        lastCriticalPulseSecond = seconds;

#if DOTWEEN
        if (punchTween != null && punchTween.IsActive())
        {
            punchTween.Kill();
        }

        timerRectTransform.localScale = Vector3.one;
        punchTween = timerRectTransform
            .DOPunchScale(Vector3.one * criticalPunchScale, 0.35f, 8, 0.75f)
            .SetUpdate(false)
            .OnComplete(() => punchTween = null);
#else
        if (punchRoutine != null)
        {
            StopCoroutine(punchRoutine);
        }

        punchRoutine = StartCoroutine(PunchRoutine());
#endif
    }

#if !DOTWEEN
    private IEnumerator PunchRoutine()
    {
        float elapsed = 0f;
        const float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI);
            timerRectTransform.localScale = Vector3.one * (1f + criticalPunchScale * wave);
            yield return null;
        }

        timerRectTransform.localScale = Vector3.one;
        punchRoutine = null;
    }
#endif
}
