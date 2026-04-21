using TimeClone.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeClone.UI
{
    public sealed class PauseMenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private PlayerInputHandler inputHandler;

        public void OnPausePressed()
        {
            Time.timeScale = 0f;

            if (inputHandler != null)
            {
                inputHandler.SetInputEnabled(false);
            }

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }
        }

        public void OnResumePressed()
        {
            Time.timeScale = 1f;

            if (inputHandler != null)
            {
                inputHandler.SetInputEnabled(true);
            }

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
        }

        public void OnQuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
