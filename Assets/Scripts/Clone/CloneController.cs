using System.Collections;
using System.Collections.Generic;
using TimeClone.Recording;
using UnityEngine;

#if DOTWEEN
using DG.Tweening;
#endif

public class CloneController : MonoBehaviour, IActorTag
{
    [SerializeField] private MeshRenderer visualRenderer;
    [SerializeField] private MeshRenderer rimRenderer;
    [SerializeField] private MeshRenderer[] additionalBodyRenderers;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private Vector3 obstacleCheckHalfExtents = new Vector3(0.35f, 0.5f, 0.35f);
    [SerializeField, Min(0f)] private float obstacleCheckVerticalOffset = 0.3f;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float groundFloorY = 0.5f;
    [SerializeField] private float upperFloorY = 1.6f;
    [SerializeField] private float upperFloorThresholdY = 1.5f;

    [Header("Visual Polish")]
    [SerializeField] private float spawnOvershootScale = 1.15f;
    [SerializeField] private float spawnGrowDuration = 0.15f;
    [SerializeField] private float spawnSettleDuration = 0.1f;
    [SerializeField] private Color stepFlashColor = Color.cyan;
    [SerializeField] private float stepFlashDuration = 0.1f;

    private string actorId = "Clone_1";
    private List<MovementFrame> frames;
    private bool isReplaying;
    private float moveSpeed = 8f;
    private Material bodyRuntimeMaterial;
    private Material[] additionalRuntimeMaterials;
    private Vector3 spawnRestScale = Vector3.one;
    private Coroutine spawnRoutine;
    private Coroutine flashRoutine;

#if DOTWEEN
    private Tween spawnTween;
    private Tween flashTween;
#endif

    public string ActorId => actorId;

    private void Awake()
    {
        TryAssignWallLayerIfUnset();
        TryAssignGroundLayerIfUnset();
        spawnRestScale = transform.localScale;
    }

    private void Start()
    {
        PlaySpawnAnimation();
    }

    private void OnDisable()
    {
        StopVisualTweens();
    }

    /// <summary>
    /// Initializes this clone with identity, frame data, visuals, and movement speed.
    /// </summary>
    public void Initialize(string actorId, List<MovementFrame> frames, Material bodyMat, Material rimMat, float moveSpeed)
    {
        this.actorId = actorId;
        this.frames = frames;
        this.moveSpeed = Mathf.Max(0.01f, moveSpeed);

        if (bodyMat != null)
        {
            if (visualRenderer != null)
            {
                visualRenderer.material = bodyMat;
                bodyRuntimeMaterial = visualRenderer.material;
            }

            if (additionalBodyRenderers != null)
            {
                additionalRuntimeMaterials = new Material[additionalBodyRenderers.Length];
                for (int i = 0; i < additionalBodyRenderers.Length; i++)
                {
                    if (additionalBodyRenderers[i] != null)
                    {
                        additionalBodyRenderers[i].material = bodyMat;
                        additionalRuntimeMaterials[i] = additionalBodyRenderers[i].material;
                    }
                }
            }
        }

        if (rimRenderer != null && rimMat != null)
        {
            rimRenderer.material = rimMat;
            stepFlashColor = rimRenderer.material.HasProperty("_EmissionColor")
                ? rimRenderer.material.GetColor("_EmissionColor")
                : rimRenderer.material.color;
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

            Vector3 target = frame.worldPosition;

            PushableBox box = GetBoxAt(target);
            if (box != null)
            {
                if (!CanWalkAcrossBox(box))
                {
                    bool pushed = box.TryPush(isoDirection);
                    if (!pushed)
                    {
                        continue;
                    }
                }
            }
            else if (IsBlocked(target))
            {
                continue;
            }

            PlayStepFlash();
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
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(start, target, smoothT);
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

    private PushableBox GetBoxAt(Vector3 position)
    {
        Vector3 checkCenter = new Vector3(
            Mathf.Round(position.x),
            groundFloorY + obstacleCheckVerticalOffset,
            Mathf.Round(position.z));
        Collider[] hits = Physics.OverlapBox(
            checkCenter,
            obstacleCheckHalfExtents,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            PushableBox box = hits[i].GetComponent<PushableBox>();
            if (box == null)
            {
                box = hits[i].GetComponentInParent<PushableBox>();
            }

            if (box != null)
            {
                return box;
            }
        }

        return null;
    }

    private bool CanWalkAcrossBox(PushableBox box)
    {
        return box != null
            && transform.position.y >= upperFloorThresholdY
            && GetBoxTopStandingY(box) > groundFloorY;
    }

    private float GetBoxTopStandingY(PushableBox box)
    {
        return box.StandingY;
    }

    /// <summary>
    /// Checks if there is a RampTile at the given position and input direction.
    /// Returns the ramp component if found and usable, null otherwise.
    /// </summary>
    private RampTile GetRampAt(Vector3 position, Vector2 inputDirection)
    {
        Collider[] hits = Physics.OverlapBox(
            position + (Vector3.up * 0.1f),
            new Vector3(0.4f, 0.3f, 0.4f),
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            RampTile ramp = hits[i].GetComponent<RampTile>();
            if (ramp == null)
            {
                ramp = hits[i].GetComponentInParent<RampTile>();
            }

            if (ramp != null && ramp.CanUse(inputDirection))
            {
                return ramp;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks available floor tiles at target X/Z and resolves the correct target Y level.
    /// </summary>
    private float GetTargetFloorY(Vector3 targetXZ)
    {
        Vector3 upperCheck = new Vector3(targetXZ.x, upperFloorY + 0.1f, targetXZ.z);
        bool hasUpperTile = Physics.CheckBox(
            upperCheck,
            new Vector3(0.4f, 0.2f, 0.4f),
            Quaternion.identity,
            groundLayerMask,
            QueryTriggerInteraction.Ignore);

        if (hasUpperTile && transform.position.y >= upperFloorThresholdY)
        {
            return upperFloorY;
        }

        Vector3 groundCheck = new Vector3(targetXZ.x, 0.1f, targetXZ.z);
        bool hasGroundTile = Physics.CheckBox(
            groundCheck,
            new Vector3(0.4f, 0.2f, 0.4f),
            Quaternion.identity,
            groundLayerMask,
            QueryTriggerInteraction.Ignore);

        if (hasGroundTile)
        {
            return groundFloorY;
        }

        return transform.position.y;
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

    private void TryAssignGroundLayerIfUnset()
    {
        if (groundLayerMask.value != 0)
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            groundLayerMask = 1 << groundLayer;
        }
    }

    private void PlaySpawnAnimation()
    {
        StopSpawnAnimation();

#if DOTWEEN
        transform.localScale = Vector3.zero;
        spawnTween = DOTween.Sequence()
            .Append(transform.DOScale(spawnRestScale * spawnOvershootScale, spawnGrowDuration).SetEase(Ease.OutBack))
            .Append(transform.DOScale(spawnRestScale, spawnSettleDuration).SetEase(Ease.OutSine))
            .OnComplete(() => spawnTween = null);
#else
        spawnRoutine = StartCoroutine(SpawnRoutine());
#endif
    }

    private void PlayStepFlash()
    {
        if (bodyRuntimeMaterial == null)
        {
            return;
        }

#if DOTWEEN
        if (flashTween != null && flashTween.IsActive())
        {
            flashTween.Kill();
        }

        SetEmission(stepFlashColor);
        flashTween = DOTween.To(
                () => stepFlashColor,
                SetEmission,
                Color.black,
                stepFlashDuration)
            .SetEase(Ease.OutSine)
            .OnComplete(() => flashTween = null);
#else
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
#endif
    }

    private void SetEmission(Color color)
    {
        SetMaterialEmission(bodyRuntimeMaterial, color);

        if (additionalRuntimeMaterials == null)
        {
            return;
        }

        for (int i = 0; i < additionalRuntimeMaterials.Length; i++)
        {
            SetMaterialEmission(additionalRuntimeMaterials[i], color);
        }
    }

    private static void SetMaterialEmission(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color);
    }

    private void StopVisualTweens()
    {
        StopSpawnAnimation();

#if DOTWEEN
        if (flashTween != null && flashTween.IsActive())
        {
            flashTween.Kill();
            flashTween = null;
        }
#else
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
#endif

        SetEmission(Color.black);
    }

    private void StopSpawnAnimation()
    {
#if DOTWEEN
        if (spawnTween != null && spawnTween.IsActive())
        {
            spawnTween.Kill();
            spawnTween = null;
        }
#else
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
#endif
    }

#if !DOTWEEN
    private IEnumerator SpawnRoutine()
    {
        transform.localScale = Vector3.zero;
        yield return ScaleRoutine(Vector3.zero, spawnRestScale * spawnOvershootScale, spawnGrowDuration);
        yield return ScaleRoutine(transform.localScale, spawnRestScale, spawnSettleDuration);
        spawnRoutine = null;
    }

    private IEnumerator ScaleRoutine(Vector3 start, Vector3 target, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.001f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.LerpUnclamped(start, target, eased);
            yield return null;
        }

        transform.localScale = target;
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;

        while (elapsed < stepFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, stepFlashDuration));
            SetEmission(Color.Lerp(stepFlashColor, Color.black, t));
            yield return null;
        }

        SetEmission(Color.black);
        flashRoutine = null;
    }
#endif
}






