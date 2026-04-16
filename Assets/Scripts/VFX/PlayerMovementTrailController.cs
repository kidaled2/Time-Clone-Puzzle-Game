using System.Collections;
using TimeClone.Player;
using UnityEngine;

#if DOTWEEN
using DG.Tweening;
#endif

[DisallowMultipleComponent]
public class PlayerMovementTrailController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovementController movementController;
    [SerializeField] private TrailRenderer trailRenderer;

    [Header("Trail Timing")]
    [SerializeField] private float activeTrailTime = 0.18f;
    [SerializeField] private float movementWindow = 0.16f;
    [SerializeField] private float retractDuration = 0.1f;
    [SerializeField] private float activeWidthMultiplier = 0.32f;
    [SerializeField] private float retractWidthMultiplier = 0f;
    [SerializeField] private bool clearOnDisable = true;
    [SerializeField] private float teleportClearDistance = 1.25f;

    private float movementTimer;
    private Vector3 lastPosition;
    private bool isRetracting;

#if DOTWEEN
    private Tween retractTween;
#else
    private Coroutine retractCoroutine;
#endif

    private void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<PlayerMovementController>();
        }

        if (trailRenderer == null)
        {
            Transform anchor = transform.Find("Character_Root/MovementTrailAnchor");
            if (anchor != null)
            {
                trailRenderer = anchor.GetComponent<TrailRenderer>();
            }
        }

        if (trailRenderer != null)
        {
            RestoreActiveTrailSettings();
            trailRenderer.emitting = false;
            trailRenderer.Clear();
        }

        lastPosition = transform.position;
    }

    private void OnEnable()
    {
        if (movementController != null)
        {
            movementController.OnMoveConfirmed += OnMoveConfirmed;
        }

        StopTrail(clearTrail: false);
        lastPosition = transform.position;
    }

    private void OnDisable()
    {
        if (movementController != null)
        {
            movementController.OnMoveConfirmed -= OnMoveConfirmed;
        }

        StopTrail(clearOnDisable);
    }

    private void Update()
    {
        if (trailRenderer == null)
        {
            return;
        }

        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        if (movedDistance > teleportClearDistance)
        {
            StopTrail(clearTrail: true);
        }

        if (movementTimer > 0f)
        {
            movementTimer -= Time.deltaTime;
            if (movementTimer <= 0f)
            {
                movementTimer = 0f;
                StartRetract();
            }
        }

        lastPosition = transform.position;
    }

    private void OnMoveConfirmed(Vector2 inputDirection, Vector3 targetPosition)
    {
        if (trailRenderer == null)
        {
            return;
        }

        CancelRetract();
        RestoreActiveTrailSettings();
        trailRenderer.emitting = true;
        movementTimer = movementWindow;
    }

    private void StartRetract()
    {
        if (trailRenderer == null || isRetracting)
        {
            return;
        }

        trailRenderer.emitting = false;
        isRetracting = true;

#if DOTWEEN
        retractTween = DOTween.Sequence()
            .Append(DOTween.To(
                () => trailRenderer.time,
                value => trailRenderer.time = value,
                0f,
                retractDuration))
            .Join(DOTween.To(
                () => trailRenderer.widthMultiplier,
                value => trailRenderer.widthMultiplier = value,
                retractWidthMultiplier,
                retractDuration))
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                retractTween = null;
                FinishRetract();
            });
#else
        retractCoroutine = StartCoroutine(RetractRoutine());
#endif
    }

    private void FinishRetract()
    {
        isRetracting = false;

        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.emitting = false;
        trailRenderer.Clear();
        RestoreActiveTrailSettings();
    }

    private void StopTrail(bool clearTrail)
    {
        movementTimer = 0f;
        CancelRetract();

        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.emitting = false;

        if (clearTrail)
        {
            trailRenderer.Clear();
        }

        RestoreActiveTrailSettings();
    }

    private void RestoreActiveTrailSettings()
    {
        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.time = activeTrailTime;
        trailRenderer.widthMultiplier = activeWidthMultiplier;
    }

    private void CancelRetract()
    {
        isRetracting = false;

#if DOTWEEN
        if (retractTween != null && retractTween.IsActive())
        {
            retractTween.Kill();
        }

        retractTween = null;
#else
        if (retractCoroutine != null)
        {
            StopCoroutine(retractCoroutine);
            retractCoroutine = null;
        }
#endif
    }

#if !DOTWEEN
    private IEnumerator RetractRoutine()
    {
        float elapsed = 0f;
        float startTime = trailRenderer.time;
        float startWidth = trailRenderer.widthMultiplier;

        while (elapsed < retractDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / retractDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            if (trailRenderer != null)
            {
                trailRenderer.time = Mathf.Lerp(startTime, 0f, easedT);
                trailRenderer.widthMultiplier = Mathf.Lerp(startWidth, retractWidthMultiplier, easedT);
            }

            yield return null;
        }

        retractCoroutine = null;
        FinishRetract();
    }
#endif
}
