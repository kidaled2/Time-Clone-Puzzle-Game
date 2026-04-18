using UnityEngine;
using UnityEngine.UI;

namespace TimeClone.UI
{
    public class LevelSelectButton : MonoBehaviour
    {
        [SerializeField] private int levelSceneIndex;
        [SerializeField] private MainMenuManager menuManager;

        private void Start()
        {
            if (menuManager == null)
            {
                menuManager = GetComponentInParent<MainMenuManager>();
            }

            Button button = GetComponent<Button>();
            if (button != null && menuManager != null)
            {
                button.onClick.AddListener(() => menuManager.OnLevelButtonPressed(levelSceneIndex));
            }
        }
    }
}
