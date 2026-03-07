using System.Collections.Generic;
using TimeClone.Player;
using UnityEngine;

namespace TimeClone.Recording
{
    public sealed class MovementRecorder : MonoBehaviour, IRecordable
    {
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private Transform trackedTransform;

        private readonly List<MovementFrame> recordedFrames = new List<MovementFrame>();
        private bool isRecording;
        private float recordingStartTime;

        private void Awake()
        {
            if (inputHandler == null)
            {
                inputHandler = GetComponent<PlayerInputHandler>();
            }

            if (trackedTransform == null)
            {
                trackedTransform = transform;
            }
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
        }

        /// <summary>
        /// Sets the input handler source used to receive movement input events.
        /// </summary>
        /// <param name="handler">The input handler that emits movement events.</param>
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

        private void HandleMoveInput(Vector2 inputDirection)
        {
            if (!isRecording)
            {
                return;
            }

            Vector3 worldPosition = trackedTransform != null ? trackedTransform.position : transform.position;
            recordedFrames.Add(new MovementFrame(Time.time - recordingStartTime, inputDirection, worldPosition));
        }
    }
}
