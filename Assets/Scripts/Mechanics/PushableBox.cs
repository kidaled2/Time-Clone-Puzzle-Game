using System.Collections;
using UnityEngine;

public class PushableBox : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private Vector3 obstacleCheckHalfExtents = new Vector3(0.35f, 0.5f, 0.35f);
    [SerializeField] private float obstacleCheckVerticalOffset = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] pushClips;
    [SerializeField, Min(0f)] private float pushVolume = 1f;

    private Vector3 originPosition;
    private bool isMoving;
    private Coroutine moveRoutine;
    private int lastPushClipIndex = -1;

    public float TopSurfaceY => transform.position.y + 0.5f;
    public float StandingY => transform.position.y + 1.0f;

    private void Awake()
    {
        Vector3 position = transform.position;
        transform.position = new Vector3(
            Mathf.Round(position.x),
            position.y,
            Mathf.Round(position.z));

        originPosition = transform.position;
        TryAssignWallLayerIfUnset();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Attempts to push the box one grid unit in the given world direction.
    /// Returns true if the push succeeded, false if blocked.
    /// </summary>
    public bool TryPush(Vector3 worldDirection)
    {
        if (isMoving)
        {
            return false;
        }

        Vector3 targetPosition = new Vector3(
            Mathf.Round(transform.position.x + worldDirection.x),
            transform.position.y,
            Mathf.Round(transform.position.z + worldDirection.z));

        if (IsBlocked(targetPosition))
        {
            return false;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine(targetPosition));
        PlayPushSound();
        return true;
    }

    /// <summary>
    /// Resets the box to its original spawn position. Called on turn/level reset.
    /// </summary>
    public void ResetBox()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isMoving = false;
        StopPushAudio();
        transform.position = originPosition;
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        isMoving = true;

        float elapsed = 0f;
        float duration = 1f / moveSpeed;
        Vector3 start = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(start, targetPosition, smoothT);
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
        moveRoutine = null;
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

    private void PlayPushSound()
    {
        AudioClip clip = GetNextPushClip();
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, pushVolume);
    }

    private AudioClip GetNextPushClip()
    {
        if (pushClips == null || pushClips.Length == 0)
        {
            return null;
        }

        int clipIndex = 0;
        if (pushClips.Length > 1)
        {
            clipIndex = Random.Range(0, pushClips.Length);
            if (clipIndex == lastPushClipIndex)
            {
                clipIndex = (clipIndex + 1 + Random.Range(0, pushClips.Length - 1)) % pushClips.Length;
            }
        }

        lastPushClipIndex = clipIndex;
        return pushClips[clipIndex];
    }

    private void StopPushAudio()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.8f, 0.5f, 0.2f, 0.5f);
        Gizmos.DrawCube(transform.position + (Vector3.up * 0.5f), Vector3.one * 0.9f);
        Gizmos.color = new Color(0.8f, 0.5f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position + (Vector3.up * 0.5f), Vector3.one * 0.9f);
    }
#endif
}
