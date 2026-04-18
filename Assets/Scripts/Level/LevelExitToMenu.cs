using UnityEngine.SceneManagement;

namespace TimeClone.Level
{
    public static class LevelExitToMenu
    {
        public static bool TryHandleLevelSelectReturn()
        {
            if (!TimeClone.UI.MainMenuManager.enteredViaLevelSelect)
            {
                return false;
            }

            TimeClone.UI.MainMenuManager.enteredViaLevelSelect = false;
            SceneManager.LoadScene(0);
            return true;
        }
    }
}
