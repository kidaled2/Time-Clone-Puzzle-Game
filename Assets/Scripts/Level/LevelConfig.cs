using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "TimeClon/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "Level_1";
    public int levelIndex = 1;

    [Header("Clone Settings")]
    [Range(1, 3)]
    public int maxTurns = 2;

    [Header("Scene Transition")]
    public string nextSceneName = string.Empty;
    public int nextSceneIndex = -1;
}
