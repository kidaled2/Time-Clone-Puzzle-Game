using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if DOTWEEN
using DG.Tweening;
#endif

namespace TimeClone.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
    {
        [Header("Scale")]
        [SerializeField] private Transform targetTransform;
        [SerializeField, Min(0.01f)] private float pressedScale = 0.94f;
        [SerializeField, Min(0.01f)] private float overshootScale = 1.03f;
        [SerializeField, Min(0.01f)] private float pressDuration = 0.06f;
        [SerializeField, Min(0.01f)] private float releaseDuration = 0.12f;

        [Header("Sound")]
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float clickVolume = 0.65f;

        [Header("Idle Pulse")]
        [SerializeField] private bool idlePulse = false;
        [SerializeField, Range(0f, 1f)] private float idlePulseMinAlpha = 0.75f;
        [SerializeField, Range(0f, 1f)] private float idlePulseMaxAlpha = 0.95f;
        [SerializeField, Min(0.1f)] private float idlePulseDuration = 1.8f;

        private static AudioSource sharedAudioSource;

        private Button button;
        private Image buttonImage;
        private Color baseImageColor = Color.white;
        private Vector3 restScale = Vector3.one;
        private Coroutine scaleRoutine;
        private Coroutine pulseRoutine;
        private bool isPointerDown;

#if DOTWEEN
        private Tween pulseTween;
#endif

        private void Awake()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            if (targetTransform == null)
            {
                targetTransform = transform;
            }

            restScale = targetTransform.localScale;
            if (buttonImage != null)
            {
                baseImageColor = buttonImage.color;
            }
        }

        private void OnEnable()
        {
            if (targetTransform != null)
            {
                restScale = targetTransform.localScale;
            }

            StartIdlePulse();
        }

        private void OnDisable()
        {
            isPointerDown = false;
            StopScaleRoutine();
            StopIdlePulse();

            if (targetTransform != null)
            {
                targetTransform.localScale = restScale;
            }

            if (buttonImage != null)
            {
                buttonImage.color = baseImageColor;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanPlayFeedback())
            {
                return;
            }

            isPointerDown = true;
            PlayClick();
            AnimateToScale(pressedScale, pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPointerDown)
            {
                return;
            }

            isPointerDown = false;
            PlayReleaseAnimation();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isPointerDown)
            {
                return;
            }

            isPointerDown = false;
            AnimateToScale(1f, releaseDuration * 0.5f);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!CanPlayFeedback())
            {
                return;
            }

            PlayClick();
            StopScaleRoutine();
            scaleRoutine = StartCoroutine(SubmitRoutine());
        }

        private bool CanPlayFeedback()
        {
            return isActiveAndEnabled
                && button != null
                && button.IsInteractable()
                && targetTransform != null;
        }

        private void PlayReleaseAnimation()
        {
            StopScaleRoutine();
            scaleRoutine = StartCoroutine(ReleaseRoutine());
        }

        private IEnumerator ReleaseRoutine()
        {
            yield return ScaleTo(restScale * overshootScale, releaseDuration * 0.45f);
            yield return ScaleTo(restScale, releaseDuration * 0.55f);
            scaleRoutine = null;
        }

        private IEnumerator SubmitRoutine()
        {
            yield return ScaleTo(restScale * pressedScale, pressDuration);
            yield return ScaleTo(restScale * overshootScale, releaseDuration * 0.45f);
            yield return ScaleTo(restScale, releaseDuration * 0.55f);
            scaleRoutine = null;
        }

        private void AnimateToScale(float scale, float duration)
        {
            StopScaleRoutine();
            scaleRoutine = StartCoroutine(ScaleTo(restScale * scale, duration));
        }

        private IEnumerator ScaleTo(Vector3 targetScale, float duration)
        {
            Vector3 startScale = targetTransform.localScale;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.001f, duration);

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                targetTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                yield return null;
            }

            targetTransform.localScale = targetScale;
        }

        private void StopScaleRoutine()
        {
            if (scaleRoutine == null)
            {
                return;
            }

            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        private void PlayClick()
        {
            if (clickClip == null || clickVolume <= 0f)
            {
                return;
            }

            AudioSource source = GetSharedAudioSource();
            source.PlayOneShot(clickClip, clickVolume);
        }

        private static AudioSource GetSharedAudioSource()
        {
            if (sharedAudioSource != null)
            {
                return sharedAudioSource;
            }

            GameObject audioObject = new GameObject("UI_ButtonFeedbackAudio");
            DontDestroyOnLoad(audioObject);

            sharedAudioSource = audioObject.AddComponent<AudioSource>();
            sharedAudioSource.playOnAwake = false;
            sharedAudioSource.loop = false;
            sharedAudioSource.spatialBlend = 0f;
            return sharedAudioSource;
        }

        private void StartIdlePulse()
        {
            if (!idlePulse || buttonImage == null)
            {
                return;
            }

            StopIdlePulse();
            baseImageColor = buttonImage.color;

#if DOTWEEN
            Color pulseTarget = baseImageColor;
            pulseTarget.a = idlePulseMaxAlpha;
            Color pulseStart = baseImageColor;
            pulseStart.a = idlePulseMinAlpha;
            buttonImage.color = pulseStart;
            pulseTween = buttonImage
                .DOColor(pulseTarget, idlePulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
#else
            pulseRoutine = StartCoroutine(IdlePulseRoutine());
#endif
        }

        private void StopIdlePulse()
        {
#if DOTWEEN
            if (pulseTween != null && pulseTween.IsActive())
            {
                pulseTween.Kill();
                pulseTween = null;
            }
#else
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }
#endif
        }

#if !DOTWEEN
        private IEnumerator IdlePulseRoutine()
        {
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = (Mathf.Sin((elapsed / Mathf.Max(0.001f, idlePulseDuration)) * Mathf.PI * 2f) + 1f) * 0.5f;
                Color color = baseImageColor;
                color.a = Mathf.Lerp(idlePulseMinAlpha, idlePulseMaxAlpha, wave);
                buttonImage.color = color;
                yield return null;
            }
        }
#endif
    }
}
