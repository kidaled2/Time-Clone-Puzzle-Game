using System.Collections.Generic;

namespace TimeClone.Level
{
    public static class LevelBestTimeSession
    {
        private static readonly Dictionary<int, float> bestTimesBySceneIndex = new Dictionary<int, float>();

        public static bool TryGetBestTime(int levelSceneIndex, out float bestSeconds)
        {
            return bestTimesBySceneIndex.TryGetValue(levelSceneIndex, out bestSeconds);
        }

        public static bool TrySubmitBestTime(int levelSceneIndex, float elapsedSeconds)
        {
            if (levelSceneIndex < 0 || elapsedSeconds < 0f)
            {
                return false;
            }

            if (bestTimesBySceneIndex.TryGetValue(levelSceneIndex, out float currentBest)
                && elapsedSeconds >= currentBest)
            {
                return false;
            }

            bestTimesBySceneIndex[levelSceneIndex] = elapsedSeconds;
            return true;
        }
    }
}
