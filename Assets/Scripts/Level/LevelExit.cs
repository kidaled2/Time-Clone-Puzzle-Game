using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles level completion when the player reaches the exit trigger tile.
/// </summary>
public class LevelExit : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string nextSceneName = string.Empty;
    [SerializeField] private int nextSceneIndex = -1;
    [SerializeField] private float completionDelay = 1.2f;

    [Header("References")]
    [SerializeField] private Light exitLight;
    [SerializeField] private GameObject completionVFX;

    private bool isActive = true;
    private bool isTriggered;

    private float baseLightIntensity = 2.5f;
    private float pulseSpeed = 2f;
    private float pulseAmount = 0.8f;

    private void Update()
    {
        if (exitLight == null || !isActive || isTriggered)
        {
            return;
        }

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        exitLight.intensity = baseLightIntensity + pulse;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || isTriggered)
        {
            return;
        }

        IActorTag actor = other.GetComponent<IActorTag>();
        if (actor == null || actor.ActorId != "Player")
        {
            return;
        }

        isTriggered = true;
        StartCoroutine(CompleteLevel());
    }

    /// <summary>
    /// Enables or disables the exit trigger during turn transitions.
    /// </summary>
    /// <param name="active">True to allow triggering the exit.</param>
    public void SetActive(bool active)
    {
        isActive = active;
        isTriggered = false;
    }

    private IEnumerator CompleteLevel()
    {
        Debug.Log("[LevelExit] Level complete!");

        if (completionVFX != null)
        {
            completionVFX.SetActive(true);
            ParticleSystem[] particleSystems = completionVFX.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Play(true);
            }
        }

        if (exitLight != null)
        {
            exitLight.intensity = 6f;
            exitLight.color = Color.white;
        }

        yield return new WaitForSeconds(completionDelay);
        LoadNextLevel();
    }

    private void LoadNextLevel()
    {
        if (nextSceneIndex >= 0 && nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
            return;
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        Debug.LogWarning("[LevelExit] No next scene assigned. Reloading current scene.");
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
