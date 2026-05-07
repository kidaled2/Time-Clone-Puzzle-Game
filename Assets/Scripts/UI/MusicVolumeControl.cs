using TMPro;
using TimeClone.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace TimeClone.UI
{
    public sealed class MusicVolumeControl : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI valueLabel;

        private bool initialized;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (volumeSlider == null)
            {
                volumeSlider = GetComponentInChildren<Slider>(true);
            }
        }

        private void OnEnable()
        {
            InitializeSlider();
        }

        private void OnDestroy()
        {
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
            }
        }

        public void Show()
        {
            InitializeSlider();
            SetPanelVisible(true);
        }

        public void Hide()
        {
            SetPanelVisible(false);
        }

        public void Toggle()
        {
            bool shouldShow = panelRoot == null || !panelRoot.activeSelf;
            if (shouldShow)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void InitializeSlider()
        {
            if (initialized || volumeSlider == null)
            {
                return;
            }

            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.wholeNumbers = false;
            volumeSlider.SetValueWithoutNotify(GetCurrentVolume());
            volumeSlider.onValueChanged.AddListener(OnSliderChanged);
            UpdateValueLabel(volumeSlider.value);
            initialized = true;
        }

        private static float GetCurrentVolume()
        {
            return MusicManager.Instance != null
                ? MusicManager.Instance.MusicVolume
                : MusicManager.GetSavedMusicVolume();
        }

        private void OnSliderChanged(float value)
        {
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.SetMusicVolume(value);
            }
            else
            {
                PlayerPrefs.SetFloat("MusicVolume", Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }

            UpdateValueLabel(value);
        }

        private void UpdateValueLabel(float value)
        {
            if (valueLabel != null)
            {
                valueLabel.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }
    }
}
