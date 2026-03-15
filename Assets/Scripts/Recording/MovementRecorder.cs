using System.Collections.Generic;
using TimeClone.Player;
using UnityEngine;

namespace TimeClone.Recording
{
    public sealed class MovementRecorder : MonoBehaviour, IRecordable
    {
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private Transform trackedTransform;

        private readonly List<MovementFrame> recordedFrames = new List<MovementFrame>();
        private bool isRecording;
        private float recordingStartTime;

        private void Awake()
        {
            if (movementController == null)
            {
                movementController = GetComponent<PlayerMovementController>();
            }

            if (trackedTransform == null)
            {
                trackedTransform = transform;
            }
        }

        private void OnEnable()
        {
            if (movementController != null)
            {
                movementController.OnMoveConfirmed += RecordFrame;
            }
        }

        private void OnDisable()
        {
            if (movementController != null)
            {
                movementController.OnMoveConfirmed -= RecordFrame;
            }
        }

        /// <summary>
        /// Sets the movement controller source used to receive confirmed movement events.
        /// </summary>
        /// <param name="controller">The movement controller that emits confirmed move events.</param>
        public void SetMovementController(PlayerMovementController controller)
        {
            if (movementController == controller)
            {
                return;
            }

            if (isActiveAndEnabled && movementController != null)
            {
                movementController.OnMoveConfirmed -= RecordFrame;
            }

            movementController = controller;

            if (isActiveAndEnabled && movementController != null)
            {
                movementController.OnMoveConfirmed += RecordFrame;
            }
        }

        /// <summary>
        /// Sets the transform whose world position is captured in recorded frames.
        /// </summary>
        /// <param name="target">The transform to sample for position.</param>
        public void SetTrackedTransform(Transform target)
        {
            trackedTransform = target;
        }

        /// <summary>
        /// Starts recording movement frames.
        /// </summary>
        public void StartRecording()
        {
            recordedFrames.Clear();
            recordingStartTime = Time.time;
            isRecording = true;
        }

        /// <summary>
        /// Stops recording movement frames.
        /// </summary>
        public void StopRecording()
        {
            isRecording = false;
        }

        /// <summary>
        /// Clears all recorded movement frames.
        /// </summary>
        public void ClearRecording()
        {
            recordedFrames.Clear();
        }

        /// <summary>
        /// Returns a copy of all recorded movement frames.
        /// </summary>
        /// <returns>A new list containing all recorded frames.</returns>
        public List<MovementFrame> GetRecordedFrames()
        {
            return new List<MovementFrame>(recordedFrames);
        }

        private void RecordFrame(Vector2 inputDirection, Vector3 targetPosition)
        {
            if (!isRecording)
            {
                return;
            }

            recordedFrames.Add(new MovementFrame(Time.time - recordingStartTime, inputDirection, targetPosition));
        }
    }
}

