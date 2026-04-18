using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if DOTWEEN
using DG.Tweening;
#endif

namespace TimeClone.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private float creditsDuration = 3f;

        public static bool enteredViaLevelSelect = false;

        private CanvasGroup creditsCanvasGroup;
        private Coroutine creditsCoroutine;

#if DOTWEEN
        private Tween creditsFadeTween;
#endif

        private void Awake()
        {
            if (creditsPanel != null)
            {
                creditsCanvasGroup = creditsPanel.GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            OnBackToMainMenu();
        }

        private void OnDisable()
        {
            StopCreditsFlow();
        }

        public void OnStartPressed()
        {
            enteredViaLevelSelect = false;
            SceneManager.LoadScene(1);
        }

        public void OnLevelSelectPressed()
        {
            StopCreditsFlow();
            ShowPanel(mainMenuPanel, false);
            ShowPanel(levelSelectPanel, true);
            ShowPanel(creditsPanel, false);
            ResetCreditsVisuals();
        }

        public void OnLevelButtonPressed(int levelIndex)
        {
            enteredViaLevelSelect = true;
            SceneManager.LoadScene(levelIndex);
        }

        public void OnCreditsPressed()
        {
            StopCreditsFlow();
            ShowPanel(mainMenuPanel, false);
            ShowPanel(levelSelectPanel, false);
            ShowPanel(creditsPanel, true);
            ResetCreditsVisuals();

            creditsCoroutine = StartCoroutine(CreditsRoutine());
        }

        public void OnBackToMainMenu()
        {
            StopCreditsFlow();
            ShowPanel(levelSelectPanel, false);
            ShowPanel(creditsPanel, false);
            ShowPanel(mainMenuPanel, true);
            ResetCreditsVisuals();
        }

        private IEnumerator CreditsRoutine()
        {
            yield return FadeCreditsIn();
            yield return new WaitForSeconds(creditsDuration);
            creditsCoroutine = null;
            OnBackToMainMenu();
        }

        private IEnumerator FadeCreditsIn()
        {
            if (creditsCanvasGroup == null)
            {
                yield break;
            }

#if DOTWEEN
            if (creditsFadeTween != null)
            {
                creditsFadeTween.Kill();
            }

            creditsCanvasGroup.alpha = 0f;
            creditsFadeTween = creditsCanvasGroup.DOFade(1f, 0.35f).SetEase(Ease.OutSine);
            yield return creditsFadeTween.WaitForCompletion();
            creditsFadeTween = null;
#else
            float duration = 0.35f;
            float elapsed = 0f;
            creditsCanvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                creditsCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            creditsCanvasGroup.alpha = 1f;
#endif
        }

        private void StopCreditsFlow()
        {
            if (creditsCoroutine != null)
            {
                StopCoroutine(creditsCoroutine);
                creditsCoroutine = null;
            }

#if DOTWEEN
            if (creditsFadeTween != null)
            {
                creditsFadeTween.Kill();
                creditsFadeTween = null;
            }
#endif
        }

        private void ResetCreditsVisuals()
        {
            if (creditsCanvasGroup != null)
            {
                creditsCanvasGroup.alpha = 0f;
            }
        }

        private static void ShowPanel(GameObject panel, bool shouldShow)
        {
            if (panel != null)
            {
                panel.SetActive(shouldShow);
            }
        }
    }
}
