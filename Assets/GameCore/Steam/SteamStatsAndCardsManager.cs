#if ENABLE_STEAM && !DISABLE_STEAM
#define WALKBRAVE_STEAM_ENABLED
#endif

using UnityEngine;
#if WALKBRAVE_STEAM_ENABLED
using Steamworks;
#endif

namespace GameCore.Steam
{
    /// <summary>
    /// Manages Steam Stats.
    /// Note: Steam Trading Cards are dropped automatically by the Steam Client based on playtime.
    /// There is no specific API call to "drop a card". 
    /// Ensure the App ID is correct and the game is running via Steam.
    /// This manager handles the Stats system which is often used alongside achievements.
    /// </summary>
    public class SteamStatsAndCardsManager : MonoBehaviour
    {
        private static SteamStatsAndCardsManager s_instance;
        public static SteamStatsAndCardsManager Instance => s_instance;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Sets a stat value (Integer).
        /// </summary>
        /// <param name="statName">API Name of the stat.</param>
        /// <param name="value">Value to set.</param>
        public void SetStat(string statName, int value)
        {
#if WALKBRAVE_STEAM_ENABLED
            if (!SteamManager.Instance.IsInitialized) return;

            bool success = SteamUserStats.SetStat(statName, value);
            if (success)
            {
                SteamUserStats.StoreStats();
                Debug.Log($"[SteamStatsAndCardsManager] Set Stat (Int) '{statName}' to {value}");
            }
            else
            {
                Debug.LogWarning($"[SteamStatsAndCardsManager] Failed to set Stat '{statName}'");
            }
#else
            Debug.Log($"[SteamStatsAndCardsManager] Steam disabled. Skipping SetStat '{statName}'.");
#endif
        }

        /// <summary>
        /// Sets a stat value (Float).
        /// </summary>
        /// <param name="statName">API Name of the stat.</param>
        /// <param name="value">Value to set.</param>
        public void SetStat(string statName, float value)
        {
#if WALKBRAVE_STEAM_ENABLED
            if (!SteamManager.Instance.IsInitialized) return;

            bool success = SteamUserStats.SetStat(statName, value);
            if (success)
            {
                SteamUserStats.StoreStats();
                Debug.Log($"[SteamStatsAndCardsManager] Set Stat (Float) '{statName}' to {value}");
            }
            else
            {
                Debug.LogWarning($"[SteamStatsAndCardsManager] Failed to set Stat '{statName}'");
            }
#else
            Debug.Log($"[SteamStatsAndCardsManager] Steam disabled. Skipping SetStat '{statName}'.");
#endif
        }

        /// <summary>
        /// Gets a stat value (Integer).
        /// </summary>
        public int GetStatInt(string statName)
        {
#if WALKBRAVE_STEAM_ENABLED
            if (!SteamManager.Instance.IsInitialized) return 0;

            int value;
            SteamUserStats.GetStat(statName, out value);
            return value;
#else
            return 0;
#endif
        }

        /// <summary>
        /// Gets a stat value (Float).
        /// </summary>
        public float GetStatFloat(string statName)
        {
#if WALKBRAVE_STEAM_ENABLED
            if (!SteamManager.Instance.IsInitialized) return 0f;

            float value;
            SteamUserStats.GetStat(statName, out value);
            return value;
#else
            return 0f;
#endif
        }
    }
}
