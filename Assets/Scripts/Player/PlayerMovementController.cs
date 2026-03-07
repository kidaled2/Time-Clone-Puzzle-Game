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
        private static readonly Vector3 IsoForward = new Vector3(-1f, 0f, 1f);
        private static readonly Vector3 IsoRight = new Vector3(1f, 0f, 1f);

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

        [Header("Blocked Feedback")]
        [SerializeField] private bool playBlockedBump = true;
        [SerializeField, Min(0f)] private float bumpDistance = 0.08f;
        [SerializeField, Min(0.01f)] private float bumpDuration = 0.1f;

        [Header("Debug Grid")]
        [SerializeField] private bool drawGridGizmo = true;
        [SerializeField, Min(1)] private int gridGizmoRadius = 5;
        [SerializeField] private Color gridGizmoColor = new Color(0.2f, 0.8f, 1f, 0.65f);
        [SerializeField, Min(0f)] private float gridGizmoYOffset = 0.02f;

        private bool isMoving;
        private Coroutine moveRoutine;
        private Coroutine bumpRoutine;
        private Rigidbody rb;

        public string ActorId => "Player";
        public event Action<Vector2> OnMoveConfirmed;

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

            Vector3 targetPosition = GetCurrentPosition() + (worldDirection * gridStepDistance);
            targetPosition = new Vector3(
                Mathf.Round(targetPosition.x),
                GetCurrentPosition().y,
                Mathf.Round(targetPosition.z));
            if (IsBlocked(targetPosition))
            {
                TryPlayBlockedBump(worldDirection);
                return;
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            OnMoveConfirmed?.Invoke(inputDirection);
            StartMovement(targetPosition, worldDirection);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGridGizmo || gridGizmoRadius < 1)
            {
                return;
            }

            DrawGridGizmo();
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
                    .SetEase(Ease.Linear)
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
                    .SetEase(Ease.Linear)
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

            while ((GetCurrentPosition() - targetPosition).sqrMagnitude > 0.0001f)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float stepDistance = movementSpeed * deltaTime;
                Vector3 nextPosition = Vector3.MoveTowards(GetCurrentPosition(), targetPosition, stepDistance);
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

        private void DrawGridGizmo()
        {
            float step = Mathf.Max(0.01f, gridStepDistance);
            int radius = Mathf.Max(1, gridGizmoRadius);
            Vector3 axisForward = IsoForward.normalized * step;
            Vector3 axisRight = IsoRight.normalized * step;
            Vector3 origin = GetSnappedGridOrigin(step) + (Vector3.up * gridGizmoYOffset);

            Gizmos.color = gridGizmoColor;

            for (int i = -radius; i <= radius; i++)
            {
                Vector3 start = origin + (axisForward * i) - (axisRight * radius);
                Vector3 end = origin + (axisForward * i) + (axisRight * radius);
                Gizmos.DrawLine(start, end);
            }

            for (int i = -radius; i <= radius; i++)
            {
                Vector3 start = origin + (axisRight * i) - (axisForward * radius);
                Vector3 end = origin + (axisRight * i) + (axisForward * radius);
                Gizmos.DrawLine(start, end);
            }

            Gizmos.DrawWireSphere(origin, step * 0.1f);
        }

        private Vector3 GetSnappedGridOrigin(float step)
        {
            Vector3 forwardAxis = IsoForward.normalized;
            Vector3 rightAxis = IsoRight.normalized;
            Vector3 position = transform.position;

            float forwardCoord = Mathf.Round(Vector3.Dot(position, forwardAxis) / step) * step;
            float rightCoord = Mathf.Round(Vector3.Dot(position, rightAxis) / step) * step;

            Vector3 snapped = (forwardAxis * forwardCoord) + (rightAxis * rightCoord);
            snapped.y = transform.position.y;
            return snapped;
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
}
