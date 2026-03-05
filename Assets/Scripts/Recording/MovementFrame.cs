using System;
using UnityEngine;

namespace TimeClone.Recording
{
    [Serializable]
    public struct MovementFrame
    {
        public float timestamp;
        public Vector2 inputDirection;
        public Vector3 worldPosition;

        public MovementFrame(float timestamp, Vector2 inputDirection, Vector3 worldPosition)
        {
            this.timestamp = timestamp;
            this.inputDirection = inputDirection;
            this.worldPosition = worldPosition;
        }
    }
}
