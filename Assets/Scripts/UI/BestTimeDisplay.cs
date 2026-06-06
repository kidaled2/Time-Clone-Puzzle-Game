using TMPro;
using TimeClone.Level;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeClone.UI
{
    public sealed class BestTimeDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI bestTimeLabel;
        [SerializeField] private int levelSceneIndex = -1;

        private void Awake()
        {
            if (bestTimeLabel == null)
            {
                bestTimeLabel = GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (bestTimeLabel == null)
            {
                return;
            }

            int targetSceneIndex = levelSceneIndex >= 0
                ? levelSceneIndex
                : SceneManager.GetActiveScene().buildIndex;

            if (LevelBestTimeSession.TryGetBestTime(targetSceneIndex, out float bestSeconds))
            {
                bestTimeLabel.gameObject.SetActive(true);
                bestTimeLabel.text = $"Best time: {FormatSeconds(bestSeconds)} seconds";
            }
            else
            {
                bestTimeLabel.text = string.Empty;
                bestTimeLabel.gameObject.SetActive(false);
            }
        }

        private static string FormatSeconds(float seconds)
        {
            return seconds < 10f
                ? seconds.ToString("0.0")
                : Mathf.CeilToInt(seconds).ToString();
        }
    }
}
