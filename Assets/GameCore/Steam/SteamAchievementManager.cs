#if ENABLE_STEAM && !DISABLE_STEAM
#define WALKBRAVE_STEAM_ENABLED
#endif

using UnityEngine;
#if WALKBRAVE_STEAM_ENABLED
using System.Runtime.InteropServices;
using System;
#endif

namespace GameCore.Steam
{
    public class SteamAchievementManager : MonoBehaviour
    {
        private static SteamAchievementManager s_instance;
        public static SteamAchievementManager Instance => s_instance;

#if WALKBRAVE_STEAM_ENABLED
        private bool m_statsReceived;
#endif

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
            DontDestroyOnLoad(gameObject);
#if WALKBRAVE_STEAM_ENABLED
            TryInitializeSteam();
#endif
        }

        private void Start()
        {
#if WALKBRAVE_STEAM_ENABLED
            if (SteamNative.IsInitialized)
            {
                RequestStats();
            }
#endif
        }

        private void Update()
        {
#if WALKBRAVE_STEAM_ENABLED
            SteamNative.RunCallbacks();
#endif
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
#if WALKBRAVE_STEAM_ENABLED
                SteamNative.Shutdown();
#endif
                s_instance = null;
            }
        }

#if WALKBRAVE_STEAM_ENABLED
        private void TryInitializeSteam()
        {
            if (!SteamNative.EnsureInitialized())
            {
                Debug.LogWarning("[SteamAchievementManager] Failed to initialize steam_api64.dll.");
            }
        }

        private void RequestStats()
        {
            if (!SteamNative.EnsureInitialized())
            {
                Debug.LogWarning("[SteamAchievementManager] Steam API not initialized, cannot request stats.");
                return;
            }

            m_statsReceived = SteamNative.RequestCurrentStats();
            if (m_statsReceived)
            {
                Debug.Log("[SteamAchievementManager] Stats ready via steam_api64.dll.");
            }
            else
            {
                Debug.LogWarning("[SteamAchievementManager] RequestCurrentStats failed.");
            }
        }
#endif

        /// <summary>
        /// Unlocks an achievement by its API Name.
        /// </summary>
        /// <param name="achievementApiName">The API Name of the achievement set in Steamworks.</param>
        public void UnlockAchievement(string achievementApiName)
        {
#if WALKBRAVE_STEAM_ENABLED
            if (!SteamNative.EnsureInitialized())
            {
                Debug.LogWarning("[SteamAchievementManager] Steam API not initialized. Cannot unlock achievement.");
                return;
            }

            if (!m_statsReceived)
            {
                RequestStats();
                if (!m_statsReceived)
                {
                    Debug.LogWarning("[SteamAchievementManager] Stats not available. Aborting achievement unlock.");
                    return;
                }
            }

            if (!SteamNative.GetAchievement(achievementApiName, out bool isAchieved))
            {
                Debug.LogWarning($"[SteamAchievementManager] Failed to retrieve achievement '{achievementApiName}'.");
                return;
            }

            if (isAchieved)
            {
                Debug.Log($"[SteamAchievementManager] Achievement '{achievementApiName}' already unlocked.");
                return;
            }

            if (!SteamNative.SetAchievement(achievementApiName))
            {
                Debug.LogWarning($"[SteamAchievementManager] Failed to set achievement '{achievementApiName}'.");
                return;
            }

            if (!SteamNative.StoreStats())
            {
                Debug.LogWarning($"[SteamAchievementManager] Failed to store stats after unlocking '{achievementApiName}'.");
                return;
            }

            Debug.Log($"[SteamAchievementManager] Unlocking achievement '{achievementApiName}'...");
#else
            Debug.Log($"[SteamAchievementManager] Steam disabled. Skipping unlock for '{achievementApiName}'.");
#endif
        }

        /// <summary>
        /// Clears an achievement (for testing purposes).
        /// </summary>
        public void ClearAchievement(string achievementApiName)
        {
#if WALKBRAVE_STEAM_ENABLED
            if (!SteamNative.EnsureInitialized())
                return;

            if (!SteamNative.ClearAchievement(achievementApiName))
            {
                Debug.LogWarning($"[SteamAchievementManager] Failed to clear achievement '{achievementApiName}'.");
                return;
            }

            if (!SteamNative.StoreStats())
            {
                Debug.LogWarning($"[SteamAchievementManager] Failed to store stats after clearing '{achievementApiName}'.");
                return;
            }

            Debug.Log($"[SteamAchievementManager] Cleared achievement '{achievementApiName}'.");
#endif
        }

#if WALKBRAVE_STEAM_ENABLED
        private static class SteamNative
        {
            private const string k_dllName = "steam_api64";
            private static bool s_initialized;
            private static IntPtr s_userStats = IntPtr.Zero;

            public static bool IsInitialized => s_initialized && s_userStats != IntPtr.Zero;

            public static bool EnsureInitialized()
            {
                if (IsInitialized)
                    return true;

                if (!SteamAPI_IsSteamRunning())
                    return false;

                if (!SteamAPI_Init())
                    return false;

                s_userStats = SteamAPI_SteamUserStats_v012();
                if (s_userStats == IntPtr.Zero)
                {
                    SteamAPI_Shutdown();
                    s_initialized = false;
                    return false;
                }

                s_initialized = true;
                return true;
            }

            public static bool RequestCurrentStats()
            {
                if (!EnsureInitialized())
                    return false;
                return SteamAPI_ISteamUserStats_RequestCurrentStats(s_userStats);
            }

            public static bool GetAchievement(string name, out bool achieved)
            {
                achieved = false;
                if (!EnsureInitialized())
                    return false;
                return SteamAPI_ISteamUserStats_GetAchievement(s_userStats, name, out achieved);
            }

            public static bool SetAchievement(string name)
            {
                if (!EnsureInitialized())
                    return false;
                return SteamAPI_ISteamUserStats_SetAchievement(s_userStats, name);
            }

            public static bool ClearAchievement(string name)
            {
                if (!EnsureInitialized())
                    return false;
                return SteamAPI_ISteamUserStats_ClearAchievement(s_userStats, name);
            }

            public static bool StoreStats()
            {
                if (!EnsureInitialized())
                    return false;
                return SteamAPI_ISteamUserStats_StoreStats(s_userStats);
            }

            public static void RunCallbacks()
            {
                if (!IsInitialized)
                    return;
                SteamAPI_RunCallbacks();
            }

            public static void Shutdown()
            {
                if (!IsInitialized)
                    return;
                SteamAPI_Shutdown();
                s_initialized = false;
                s_userStats = IntPtr.Zero;
            }

            [DllImport(k_dllName, EntryPoint = "SteamAPI_IsSteamRunning", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool SteamAPI_IsSteamRunning();

            [DllImport(k_dllName, EntryPoint = "SteamAPI_Init", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool SteamAPI_Init();

            [DllImport(k_dllName, EntryPoint = "SteamAPI_Shutdown", CallingConvention = CallingConvention.Cdecl)]
            private static extern void SteamAPI_Shutdown();

            [DllImport(k_dllName, EntryPoint = "SteamAPI_RunCallbacks", CallingConvention = CallingConvention.Cdecl)]
            private static extern void SteamAPI_RunCallbacks();

            [DllImport(k_dllName, EntryPoint = "SteamAPI_SteamUserStats_v012", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr SteamAPI_SteamUserStats_v012();

            [DllImport(k_dllName, EntryPoint = "SteamAPI_ISteamUserStats_RequestCurrentStats", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool SteamAPI_ISteamUserStats_RequestCurrentStats(IntPtr instance);

            [DllImport(k_dllName, EntryPoint = "SteamAPI_ISteamUserStats_GetAchievement", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool SteamAPI_ISteamUserStats_GetAchievement(IntPtr instance, string name, [MarshalAs(UnmanagedType.I1)] out bool achieved);

            [DllImport(k_dllName, EntryPoint = "SteamAPI_ISteamUserStats_SetAchievement", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool SteamAPI_ISteamUserStats_SetAchievement(IntPtr instance, string name);

            [DllImport(k_dllName, EntryPoint = "SteamAPI_ISteamUserStats_ClearAchievement", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool SteamAPI_ISteamUserStats_ClearAchievement(IntPtr instance, string name);

            [DllImport(k_dllName, EntryPoint = "SteamAPI_ISteamUserStats_StoreStats", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool SteamAPI_ISteamUserStats_StoreStats(IntPtr instance);
        }
#endif
    }
}
