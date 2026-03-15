using System;
using System.Collections;
using UnityEngine;

#if DOTWEEN
using DG.Tweening;
#endif

namespace TimeClone.Player
{
    public sealed class PlayerMovementController : MonoBehaviour, IActorTag
    {
        [Header("References")]
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private Transform visualTransform;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float gridStepDistance = 1f;
        [SerializeField, Min(0.01f)] private float movementSpeed = 8f;
        [SerializeField, Min(1f)] private float rotationSpeedDegreesPerSecond = 720f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Collision")]
        [SerializeField] private LayerMask wallLayerMask;
        [SerializeField] private Vector3 obstacleCheckHalfExtents = new Vector3(0.35f, 0.5f, 0.35f);
        [SerializeField, Min(0f)] private float obstacleCheckVerticalOffset = 0.5f;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private float groundFloorY = 0.5f;
        [SerializeField] private float upperFloorY = 2.5f;
        [SerializeField] private float upperFloorThresholdY = 1.5f;

        [Header("Blocked Feedback")]
        [SerializeField] private bool playBlockedBump = true;
        [SerializeField, Min(0f)] private float bumpDistance = 0.08f;
        [SerializeField, Min(0.01f)] private float bumpDuration = 0.1f;

        private bool isMoving;
        private Coroutine moveRoutine;
        private Coroutine bumpRoutine;
        private Rigidbody rb;

        public string ActorId => "Player";
        public event Action<Vector2, Vector3> OnMoveConfirmed;

#if DOTWEEN
        private Tween movementTween;
        private Tween rotationTween;
        private Tween bumpTween;
#endif

        private void Awake()
        {
            SnapToGrid();

            if (inputHandler == null)
            {
                inputHandler = GetComponent<PlayerInputHandler>();
            }

            if (visualTransform == null)
            {
                visualTransform = transform;
            }

            rb = GetComponent<Rigidbody>();
            TryAssignWallLayerIfUnset();
            TryAssignGroundLayerIfUnset();
        }

        private void OnEnable()
        {
            if (inputHandler != null)
            {
                inputHandler.OnMoveInput += HandleMoveInput;
            }
        }

        private void OnDisable()
        {
            if (inputHandler != null)
            {
                inputHandler.OnMoveInput -= HandleMoveInput;
            }

            StopMovement();
            StopBump();
            isMoving = false;
        }

        /// <summary>
        /// Sets the input handler source used for movement events.
        /// </summary>
        /// <param name="handler">The input handler that emits move directions.</param>
        public void SetInputHandler(PlayerInputHandler handler)
        {
            if (inputHandler == handler)
            {
                return;
            }

            if (isActiveAndEnabled && inputHandler != null)
            {
                inputHandler.OnMoveInput -= HandleMoveInput;
            }

            inputHandler = handler;

            if (isActiveAndEnabled && inputHandler != null)
            {
                inputHandler.OnMoveInput += HandleMoveInput;
            }
        }

        private void HandleMoveInput(Vector2 inputDirection)
        {
            if (isMoving)
            {
                return;
            }

            Vector3 worldDirection = GetIsometricDirection(inputDirection);
            if (worldDirection.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

            if (movementSpeed <= 0f || gridStepDistance <= 0f)
            {
                return;
            }

            Vector3 currentPosition = GetCurrentPosition();
            Vector3 targetPosition = currentPosition + (worldDirection * gridStepDistance);
            targetPosition = new Vector3(
                Mathf.Round(targetPosition.x),
                currentPosition.y,
                Mathf.Round(targetPosition.z));

            RampTile ramp = GetRampAtCurrentPosition(inputDirection);
            if (ramp != null)
            {
                targetPosition.y = ramp.GetTargetY(inputDirection);
            }
            else
            {
                targetPosition.y = GetFloorYAtTarget(new Vector3(targetPosition.x, 0f, targetPosition.z), currentPosition.y);
            }

            if (IsBlocked(targetPosition))
            {
                TryPlayBlockedBump(worldDirection);
                return;
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            OnMoveConfirmed?.Invoke(inputDirection, targetPosition);
            StartMovement(targetPosition, worldDirection);
        }


        private void StartMovement(Vector3 targetPosition, Vector3 worldDirection)
        {
            if (movementSpeed <= 0f || gridStepDistance <= 0f)
            {
                return;
            }

            isMoving = true;

#if DOTWEEN
            StopMovement();

            float moveDuration = gridStepDistance / movementSpeed;
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);

            if (rb != null)
            {
                movementTween = rb
                    .DOMove(targetPosition, moveDuration)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(useUnscaledTime)
                    .OnComplete(() =>
                    {
                        rb.MovePosition(targetPosition);
                        isMoving = false;
                        movementTween = null;
                    })
                    .OnKill(() => movementTween = null);
            }
            else
            {
                movementTween = transform
                    .DOMove(targetPosition, moveDuration)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(useUnscaledTime)
                    .OnComplete(() =>
                    {
                        transform.position = targetPosition;
                        isMoving = false;
                        movementTween = null;
                    })
                    .OnKill(() => movementTween = null);
            }

            StartRotationTween(targetRotation);
#else
            moveRoutine = StartCoroutine(MoveRoutine(targetPosition, worldDirection));
#endif
        }

        private IEnumerator MoveRoutine(Vector3 targetPosition, Vector3 worldDirection)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            FaceDirection(worldDirection);

            float elapsed = 0f;
            float duration = gridStepDistance / movementSpeed;
            Vector3 startPosition = GetCurrentPosition();

            while (elapsed < duration)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, smoothT);
                MoveToPosition(nextPosition);
                FaceDirection(worldDirection);

                yield return null;
            }

            MoveToPosition(targetPosition);
            if (visualTransform != null)
            {
                visualTransform.rotation = targetRotation;
            }

            isMoving = false;
            moveRoutine = null;
        }

#if DOTWEEN
        private void StartRotationTween(Quaternion targetRotation)
        {
            if (visualTransform == null)
            {
                return;
            }

            if (rotationTween != null && rotationTween.IsActive())
            {
                rotationTween.Kill();
                rotationTween = null;
            }

            float angle = Quaternion.Angle(visualTransform.rotation, targetRotation);
            if (angle <= Mathf.Epsilon)
            {
                visualTransform.rotation = targetRotation;
                return;
            }

            float rotationDuration = Mathf.Max(0.01f, angle / rotationSpeedDegreesPerSecond);
            rotationTween = visualTransform
                .DORotateQuaternion(targetRotation, rotationDuration)
                .SetEase(Ease.OutSine)
                .SetUpdate(useUnscaledTime)
                .OnComplete(() => rotationTween = null)
                .OnKill(() => rotationTween = null);
        }
#endif

        /// <summary>
        /// Checks if there is a usable RampTile at the actor's CURRENT position.
        /// </summary>
        private RampTile GetRampAtCurrentPosition(Vector2 inputDirection)
        {
            Vector3 checkCenter = GetCurrentPosition() + (Vector3.up * 0.2f);
            Collider[] hits = Physics.OverlapBox(
                checkCenter,
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
        /// When on upper floor, checks if there is an upper tile at the target x/z.
        /// If not, the actor drops to ground floor.
        /// </summary>
        private float GetFloorYAtTarget(Vector3 targetXZ, float currentY)
        {
            if (currentY >= upperFloorThresholdY)
            {
                bool hasUpperTile = Physics.CheckBox(
                    new Vector3(targetXZ.x, (upperFloorY - groundFloorY) + 0.1f, targetXZ.z),
                    new Vector3(0.4f, 0.15f, 0.4f),
                    Quaternion.identity,
                    groundLayerMask,
                    QueryTriggerInteraction.Ignore);

                if (hasUpperTile)
                {
                    return upperFloorY;
                }

                bool hasGroundTile = Physics.CheckBox(
                    new Vector3(targetXZ.x, 0.1f, targetXZ.z),
                    new Vector3(0.4f, 0.15f, 0.4f),
                    Quaternion.identity,
                    groundLayerMask,
                    QueryTriggerInteraction.Ignore);

                if (hasGroundTile)
                {
                    return groundFloorY;
                }
            }

            return currentY;
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

        private Vector3 GetCurrentPosition()
        {
            return rb != null ? rb.position : transform.position;
        }

        private void MoveToPosition(Vector3 position)
        {
            if (rb != null)
            {
                rb.MovePosition(position);
                return;
            }

            transform.position = position;
        }

        private void FaceDirection(Vector3 worldDirection)
        {
            if (worldDirection == Vector3.zero || visualTransform == null)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float maxDegrees = rotationSpeedDegreesPerSecond * deltaTime;
            visualTransform.rotation = Quaternion.RotateTowards(visualTransform.rotation, targetRotation, maxDegrees);
        }

        private Vector3 GetIsometricDirection(Vector2 input)
        {
            if (input == Vector2.up) return new Vector3(0f, 0f, 1f);
            if (input == Vector2.down) return new Vector3(0f, 0f, -1f);
            if (input == Vector2.left) return new Vector3(-1f, 0f, 0f);
            if (input == Vector2.right) return new Vector3(1f, 0f, 0f);
            return Vector3.zero;
        }

        private void SnapToGrid()
        {
            Vector3 pos = transform.position;
            transform.position = new Vector3(
                Mathf.Round(pos.x),
                pos.y,
                Mathf.Round(pos.z));
        }

        private void TryPlayBlockedBump(Vector3 worldDirection)
        {
            if (!playBlockedBump || bumpDistance <= 0f || visualTransform == null || visualTransform == transform)
            {
                return;
            }

            StartBlockedBump(worldDirection);
        }

        private void StartBlockedBump(Vector3 worldDirection)
        {
#if DOTWEEN
            StopBump();

            Vector3 startLocalPosition = visualTransform.localPosition;
            Vector3 localBumpOffset = visualTransform.InverseTransformDirection(-worldDirection).normalized * bumpDistance;
            Vector3 peakLocalPosition = startLocalPosition + localBumpOffset;
            float halfDuration = bumpDuration * 0.5f;

            bumpTween = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .Append(visualTransform.DOLocalMove(peakLocalPosition, halfDuration).SetEase(Ease.OutSine))
                .Append(visualTransform.DOLocalMove(startLocalPosition, halfDuration).SetEase(Ease.InSine))
                .OnComplete(() => bumpTween = null)
                .OnKill(() =>
                {
                    if (visualTransform != null)
                    {
                        visualTransform.localPosition = startLocalPosition;
                    }

                    bumpTween = null;
                });
#else
            if (bumpRoutine != null)
            {
                StopCoroutine(bumpRoutine);
            }

            bumpRoutine = StartCoroutine(BumpRoutine(worldDirection));
#endif
        }

        private IEnumerator BumpRoutine(Vector3 worldDirection)
        {
            Vector3 startLocalPosition = visualTransform.localPosition;
            Vector3 localBumpOffset = visualTransform.InverseTransformDirection(-worldDirection) * bumpDistance;
            Vector3 peakLocalPosition = startLocalPosition + localBumpOffset;

            float halfDuration = bumpDuration * 0.5f;
            float bumpSpeed = bumpDistance / Mathf.Max(halfDuration, 0.0001f);

            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += deltaTime;
                float maxDistanceDelta = bumpSpeed * deltaTime;
                visualTransform.localPosition = Vector3.MoveTowards(visualTransform.localPosition, peakLocalPosition, maxDistanceDelta);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += deltaTime;
                float maxDistanceDelta = bumpSpeed * deltaTime;
                visualTransform.localPosition = Vector3.MoveTowards(visualTransform.localPosition, startLocalPosition, maxDistanceDelta);
                yield return null;
            }

            visualTransform.localPosition = startLocalPosition;
            bumpRoutine = null;
        }

        private void StopMovement()
        {
#if DOTWEEN
            if (movementTween != null && movementTween.IsActive())
            {
                movementTween.Kill();
            }

            if (rotationTween != null && rotationTween.IsActive())
            {
                rotationTween.Kill();
            }
#endif

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }
        }

        private void StopBump()
        {
#if DOTWEEN
            if (bumpTween != null && bumpTween.IsActive())
            {
                bumpTween.Kill();
            }
#endif

            if (bumpRoutine != null)
            {
                StopCoroutine(bumpRoutine);
                bumpRoutine = null;
            }
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
    }
}






