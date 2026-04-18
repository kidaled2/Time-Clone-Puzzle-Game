using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if DOTWEEN
using DG.Tweening;
#endif

namespace TimeClone.UI
{
    [RequireComponent(typeof(Image))]
    public class BackgroundScenePreview : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite[] backgroundSprites;
        [SerializeField] private Material blurMaterial;
        [SerializeField] private GameObject dimOverlay;
        [SerializeField] private float cycleInterval = 3f;
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private Color fallbackColor = new Color(0.050980393f, 0.050980393f, 0.101960786f, 1f);

        private Coroutine cycleCoroutine;
        private int currentSpriteIndex = -1;

#if DOTWEEN
        private Tween fadeTween;
#endif

        private void Awake()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            ApplyVisualMode();
        }

        private void Start()
        {
            InitializeBackground();

            if (GetValidSpriteCount() > 1)
            {
                cycleCoroutine = StartCoroutine(CycleBackgrounds());
            }
        }

        private void OnDisable()
        {
            if (cycleCoroutine != null)
            {
                StopCoroutine(cycleCoroutine);
                cycleCoroutine = null;
            }

#if DOTWEEN
            if (fadeTween != null)
            {
                fadeTween.Kill();
                fadeTween = null;
            }
#endif
        }

        private void ApplyVisualMode()
        {
            if (backgroundImage != null)
            {
                backgroundImage.material = blurMaterial;
            }

            if (dimOverlay != null)
            {
                dimOverlay.SetActive(blurMaterial == null);
            }
        }

        private void InitializeBackground()
        {
            int firstIndex = GetNextValidSpriteIndex(-1);
            if (firstIndex < 0 || backgroundImage == null)
            {
                SetFallbackState();
                return;
            }

            currentSpriteIndex = firstIndex;
            backgroundImage.sprite = backgroundSprites[currentSpriteIndex];
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;
        }

        private IEnumerator CycleBackgrounds()
        {
            while (true)
            {
                yield return new WaitForSeconds(cycleInterval);

                int nextIndex = GetNextValidSpriteIndex(currentSpriteIndex);
                if (nextIndex < 0 || nextIndex == currentSpriteIndex)
                {
                    continue;
                }

                yield return FadeTo(0f);

                currentSpriteIndex = nextIndex;
                backgroundImage.sprite = backgroundSprites[currentSpriteIndex];
                Color color = backgroundImage.color;
                color.a = 0f;
                backgroundImage.color = color;

                yield return FadeTo(1f);
            }
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (backgroundImage == null)
            {
                yield break;
            }

#if DOTWEEN
            if (fadeTween != null)
            {
                fadeTween.Kill();
            }

            fadeTween = backgroundImage.DOFade(targetAlpha, fadeDuration).SetEase(Ease.InOutSine);
            yield return fadeTween.WaitForCompletion();
            fadeTween = null;
#else
            Color color = backgroundImage.color;
            float startAlpha = color.a;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                backgroundImage.color = color;
                yield return null;
            }

            color.a = targetAlpha;
            backgroundImage.color = color;
#endif
        }

        private void SetFallbackState()
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.sprite = null;
            backgroundImage.color = fallbackColor;
        }

        private int GetValidSpriteCount()
        {
            int count = 0;
            if (backgroundSprites == null)
            {
                return count;
            }

            for (int i = 0; i < backgroundSprites.Length; i++)
            {
                if (backgroundSprites[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private int GetNextValidSpriteIndex(int startIndex)
        {
            if (backgroundSprites == null || backgroundSprites.Length == 0)
            {
                return -1;
            }

            for (int offset = 1; offset <= backgroundSprites.Length; offset++)
            {
                int candidate = (startIndex + offset + backgroundSprites.Length) % backgroundSprites.Length;
                if (backgroundSprites[candidate] != null)
                {
                    return candidate;
                }
            }

            return -1;
        }
    }
}
