using TimeClone.Player;
using UnityEngine;

public class PlayerIdleBobBridge : MonoBehaviour
{
    [SerializeField] private PlayerMovementController movementController;
    [SerializeField] private IdleHeadBob idleHeadBob;

    private bool wasMoving;
    private float movingTimer;

    private void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<PlayerMovementController>();
        }

        if (idleHeadBob == null)
        {
            idleHeadBob = GetComponent<IdleHeadBob>();
        }
    }

    private void OnEnable()
    {
        if (movementController != null)
        {
            movementController.OnMoveConfirmed += OnMoveConfirmed;
        }
    }

    private void OnDisable()
    {
        if (movementController != null)
        {
            movementController.OnMoveConfirmed -= OnMoveConfirmed;
        }

        wasMoving = false;
        movingTimer = 0f;
    }

    private void OnMoveConfirmed(Vector2 dir, Vector3 target)
    {
        if (idleHeadBob != null)
        {
            idleHeadBob.OnMovementStart();
        }

        wasMoving = true;
        movingTimer = 0f;
    }

    private void Update()
    {
        if (!wasMoving)
        {
            return;
        }

        movingTimer += Time.deltaTime;
        if (movingTimer >= 0.18f)
        {
            wasMoving = false;
            movingTimer = 0f;

            if (idleHeadBob != null)
            {
                idleHeadBob.OnMovementEnd();
            }
        }
    }
}
