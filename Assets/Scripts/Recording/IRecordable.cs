using System.Collections.Generic;

namespace TimeClone.Recording
{
    public interface IRecordable
    {
        /// <summary>
        /// Starts recording frames.
        /// </summary>
        void StartRecording();

        /// <summary>
        /// Stops recording frames.
        /// </summary>
        void StopRecording();

        /// <summary>
        /// Returns all recorded movement frames.
        /// </summary>
        /// <returns>A copy of recorded movement frames.</returns>
        List<MovementFrame> GetRecordedFrames();
    }
}
