using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeClone.UI
{
    public class ResetLevelButton : MonoBehaviour
    {
        public void OnResetPressed()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(activeScene.path);
            }
        }
    }
}
