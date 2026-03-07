using System.Collections;
using System.Collections.Generic;
using TimeClone.Recording;
using UnityEngine;

public class CloneController : MonoBehaviour, IActorTag
{
    [SerializeField] private MeshRenderer visualRenderer;
    [SerializeField] private MeshRenderer rimRenderer;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private Vector3 obstacleCheckHalfExtents = new Vector3(0.35f, 0.5f, 0.35f);
    [SerializeField, Min(0f)] private float obstacleCheckVerticalOffset = 0.5f;

    private string actorId = "Clone_1";
    private List<MovementFrame> frames;
    private bool isReplaying;
    private float moveSpeed = 8f;

    public string ActorId => actorId;

    private void Awake()
    {
        TryAssignWallLayerIfUnset();
    }

    /// <summary>
    /// Initializes this clone with identity, frame data, visuals, and movement speed.
    /// </summary>
    public void Initialize(string actorId, List<MovementFrame> frames, Material bodyMat, Material rimMat, float moveSpeed)
    {
        this.actorId = actorId;
        this.frames = frames;
        this.moveSpeed = Mathf.Max(0.01f, moveSpeed);

        if (visualRenderer != null && bodyMat != null)
        {
            visualRenderer.material = bodyMat;
        }

        if (rimRenderer != null && rimMat != null)
        {
            rimRenderer.material = rimMat;
        }
    }

    /// <summary>
    /// Starts replaying the recorded movement frames for this clone.
    /// </summary>
    public void StartReplay()
    {
        if (isReplaying || frames == null || frames.Count == 0)
        {
            return;
        }

        StartCoroutine(ReplayCoroutine());
    }

    /// <summary>
    /// Stops replaying movement for this clone.
    /// </summary>
    public void StopReplay()
    {
        StopAllCoroutines();
        isReplaying = false;
    }

    private IEnumerator ReplayCoroutine()
    {
        isReplaying = true;
        float replayStartTime = Time.time;

        for (int i = 0; i < frames.Count; i++)
        {
            MovementFrame frame = frames[i];

            float waitUntil = replayStartTime + frame.timestamp;
            while (Time.time < waitUntil)
            {
                yield return null;
            }

            Vector3 isoDirection = GetIsometricDirection(frame.inputDirection);
            if (isoDirection == Vector3.zero)
            {
                continue;
            }

            Vector3 target = new Vector3(
                Mathf.Round(transform.position.x + isoDirection.x),
                transform.position.y,
                Mathf.Round(transform.position.z + isoDirection.z));

            if (IsBlocked(target))
            {
                continue;
            }

            yield return StartCoroutine(MoveToTarget(target));
        }

        isReplaying = false;
    }

    private IEnumerator MoveToTarget(Vector3 target)
    {
        float elapsed = 0f;
        float duration = 1f / moveSpeed;
        Vector3 start = transform.position;

        Vector3 direction = target - start;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        transform.position = target;
    }

    private Vector3 GetIsometricDirection(Vector2 input)
    {
        if (input == Vector2.up) return new Vector3(0f, 0f, 1f);
        if (input == Vector2.down) return new Vector3(0f, 0f, -1f);
        if (input == Vector2.left) return new Vector3(-1f, 0f, 0f);
        if (input == Vector2.right) return new Vector3(1f, 0f, 0f);
        return Vector3.zero;
    }

    private bool IsBlocked(Vector3 targetPosition)
    {
        Vector3 checkCenter = targetPosition + (Vector3.up * obstacleCheckVerticalOffset);
        return Physics.CheckBox(
            checkCenter,
            obstacleCheckHalfExtents,
            Quaternion.identity,
            wallLayerMask,
            QueryTriggerInteraction.Ignore);
    }

    private void TryAssignWallLayerIfUnset()
    {
        if (wallLayerMask.value != 0)
        {
            return;
        }

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
        {
            wallLayerMask = 1 << wallLayer;
        }
    }
}
