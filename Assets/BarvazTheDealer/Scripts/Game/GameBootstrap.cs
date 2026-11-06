using UnityEngine;

namespace BarvazTheDealer.Game
{
    /// <summary>
    /// Makes the vertical slice playable with zero scene setup: press Play in any scene
    /// (even an empty one) and, if no BarvazGame exists yet, one is created.
    /// Set AutoStart to false once the game has its own scenes.
    /// </summary>
    public static class GameBootstrap
    {
        public static bool AutoStart = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!AutoStart) return;
#if UNITY_2023_1_OR_NEWER
            if (Object.FindFirstObjectByType<BarvazGame>() != null) return;
#else
            if (Object.FindObjectOfType<BarvazGame>() != null) return;
#endif
            new GameObject("Barvaz The Dealer").AddComponent<BarvazGame>();
        }
    }
}
