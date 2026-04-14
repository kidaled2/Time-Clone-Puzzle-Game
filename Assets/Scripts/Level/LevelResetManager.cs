using System.Collections;
using System.Collections.Generic;
using TimeClone.Player;
using TimeClone.Recording;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelResetManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("References")]
    [SerializeField] private Transform playerStartPosition;
    [SerializeField] private PlayerMovementController player;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private MovementRecorder recorder;
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private List<PressurePlate> allPlates = new List<PressurePlate>();
    [SerializeField] private List<SlidingDoor> allDoors = new List<SlidingDoor>();

    [Header("Boxes")]
    [SerializeField] private List<PushableBox> allBoxes = new List<PushableBox>();

    [Header("Clone Materials")]
    [SerializeField] private Material cloneMaterial1Body;
    [SerializeField] private Material cloneMaterial1Rim;
    [SerializeField] private Material cloneMaterial2Body;
    [SerializeField] private Material cloneMaterial2Rim;

    [Header("Exit")]
    [SerializeField] private LevelExit levelExit;

    [Header("UI")]
    [SerializeField] private GameObject endTurnButton;
    [SerializeField] private CloneCounterUI cloneCounterUI;

    [Header("Timer")]
    [SerializeField] private TurnTimer turnTimer;
    [SerializeField] private TurnTimerUI turnTimerUI;

    private int currentTurn = 1;
    private readonly List<List<MovementFrame>> allRecordings = new List<List<MovementFrame>>();
    private readonly List<CloneController> activeClones = new List<CloneController>();
    private Vector3 playerStart;
    private bool isResetting;
    private bool hasWarnedMissingCloneCounterUI;

    private void Awake()
    {
        playerStart = playerStartPosition != null && playerStartPosition.gameObject != null
            ? playerStartPosition.position
            : (player != null ? player.transform.position : Vector3.zero);

        SyncBoxReferences();
        TryAutoBindEndTurnButton();

        if (turnTimer != null)
        {
            turnTimer.OnTimerExpired += OnTurnTimerExpired;
        }
    }

    private void OnDestroy()
    {
        if (turnTimer != null)
        {
            turnTimer.OnTimerExpired -= OnTurnTimerExpired;
        }
    }

    private void Start()
    {
        StartTurn();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            EndTurn();
        }

        if (turnTimer != null && turnTimer.IsRunning && turnTimerUI != null)
        {
            turnTimerUI.UpdateDisplay(turnTimer.TimeRemaining);
        }
    }

    /// <summary>
    /// Ends the current turn, stores the recording, and advances/reset flow when allowed.
    /// </summary>
    public void EndTurn()
    {
        if (isResetting)
        {
            return;
        }

        if (levelConfig == null || recorder == null)
        {
            Debug.LogWarning("[LevelResetManager] Missing levelConfig or recorder reference.");
            return;
        }

        recorder.StopRecording();
        List<MovementFrame> frames = new List<MovementFrame>(recorder.GetRecordedFrames());
        allRecordings.Add(frames);

        if (currentTurn >= levelConfig.maxTurns)
        {
            Debug.Log("[LevelResetManager] Max turns reached.");
            return;
        }

        if (turnTimer != null)
        {
            turnTimer.StopTimer();
        }

        StartCoroutine(ResetAndStartNextTurn());
    }

    private IEnumerator ResetAndStartNextTurn()
    {
        isResetting = true;
        SyncBoxReferences();

        if (turnTimer != null)
        {
            turnTimer.StopTimer();
        }

        if (turnTimerUI != null)
        {
            turnTimerUI.Hide();
        }

        if (levelExit != null)
        {
            levelExit.SetActive(false);
        }

        if (inputHandler != null)
        {
            inputHandler.SetInputEnabled(false);
        }

        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < allPlates.Count; i++)
        {
            if (allPlates[i] != null)
            {
                allPlates[i].ResetPlate();
            }
        }

        for (int i = 0; i < allDoors.Count; i++)
        {
            if (allDoors[i] != null)
            {
                allDoors[i].ResetDoor();
            }
        }

        for (int i = 0; i < allBoxes.Count; i++)
        {
            if (allBoxes[i] != null)
            {
                allBoxes[i].ResetBox();
            }
        }

        for (int i = 0; i < activeClones.Count; i++)
        {
            if (activeClones[i] != null)
            {
                Destroy(activeClones[i].gameObject);
            }
        }

        activeClones.Clear();

        if (player != null)
        {
            player.TeleportTo(playerStart);
            Physics.SyncTransforms();
        }

        yield return null;

        currentTurn++;

        for (int i = 0; i < allRecordings.Count; i++)
        {
            SpawnClone(i, allRecordings[i]);
        }

        yield return new WaitForSeconds(0.1f);

        if (levelExit != null)
        {
            levelExit.SetActive(true);
        }

        StartTurn();
        isResetting = false;
    }

    private void OnTurnTimerExpired()
    {
        if (isResetting)
        {
            return;
        }

        isResetting = true;
        Debug.Log("[LevelResetManager] Turn timer expired - reloading level.");

        if (turnTimer != null)
        {
            turnTimer.StopTimer();
        }

        if (turnTimerUI != null)
        {
            turnTimerUI.Hide();
        }

        if (inputHandler != null)
        {
            inputHandler.SetInputEnabled(false);
        }

        if (player != null)
        {
            player.StopAllPlayerCoroutines();
        }

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

    private void StartTurn()
    {
        if (recorder != null)
        {
            recorder.ClearRecording();
            recorder.StartRecording();
        }

        if (inputHandler != null)
        {
            inputHandler.SetInputEnabled(true);
        }

        for (int i = 0; i < activeClones.Count; i++)
        {
            if (activeClones[i] != null)
            {
                activeClones[i].StartReplay();
            }
        }

        if (turnTimer != null)
        {
            turnTimer.StartTimer();
        }

        if (turnTimerUI != null)
        {
            turnTimerUI.Show();
            if (turnTimer != null)
            {
                turnTimerUI.UpdateDisplay(turnTimer.TimeRemaining);
            }
        }

        UpdateCloneUI();
        Debug.Log($"[LevelResetManager] Turn {currentTurn} started.");
    }

    private void SpawnClone(int index, List<MovementFrame> frames)
    {
        if (clonePrefab == null)
        {
            Debug.LogWarning("[LevelResetManager] Clone prefab is not assigned.");
            return;
        }

        GameObject cloneGameObject = Instantiate(clonePrefab, playerStart, Quaternion.identity);
        cloneGameObject.name = $"Clone_{index + 1}";

        CloneController controller = cloneGameObject.GetComponent<CloneController>();
        if (controller == null)
        {
            Debug.LogWarning("[LevelResetManager] Clone prefab is missing CloneController.");
            return;
        }

        Material bodyMaterial = index == 0 ? cloneMaterial1Body : cloneMaterial2Body;
        Material rimMaterial = index == 0 ? cloneMaterial1Rim : cloneMaterial2Rim;

        if (bodyMaterial == null)
        {
            bodyMaterial = cloneMaterial1Body;
        }

        if (rimMaterial == null)
        {
            rimMaterial = cloneMaterial1Rim;
        }

        controller.Initialize(
            actorId: $"Clone_{index + 1}",
            frames: frames,
            bodyMat: bodyMaterial,
            rimMat: rimMaterial,
            moveSpeed: 8f);

        activeClones.Add(controller);
    }

    private void TryAutoBindEndTurnButton()
    {
        if (endTurnButton == null)
        {
            return;
        }

        Button button = endTurnButton.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(EndTurn);
        button.onClick.AddListener(EndTurn);
    }

    private void UpdateCloneUI()
    {
        if (cloneCounterUI == null)
        {
            if (!hasWarnedMissingCloneCounterUI)
            {
                Debug.LogWarning("[LevelResetManager] cloneCounterUI is not assigned.");
                hasWarnedMissingCloneCounterUI = true;
            }

            return;
        }

        int maxTurnsForUi = levelConfig != null ? levelConfig.maxTurns : 0;
        cloneCounterUI.UpdateCloneDisplay(currentTurn, maxTurnsForUi);
    }

    private void SyncBoxReferences()
    {
        allBoxes.RemoveAll(box => box == null);

        PushableBox[] sceneBoxes = FindObjectsByType<PushableBox>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneBoxes.Length; i++)
        {
            if (!allBoxes.Contains(sceneBoxes[i]))
            {
                allBoxes.Add(sceneBoxes[i]);
            }
        }
    }
}

