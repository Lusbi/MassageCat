#if ENABLE_STEAM && !DISABLE_STEAM
#define WALKBRAVE_STEAM_ENABLED
#endif

using UnityEngine;
#if WALKBRAVE_STEAM_ENABLED
using System;
using Steamworks;
#endif

namespace GameCore.Steam
{
    public class SteamManager : MonoBehaviour
    {
        private static SteamManager s_instance;
        public static SteamManager Instance => s_instance;

        private bool m_isInitialized;
        public bool IsInitialized => m_isInitialized;

        private SteamScriptableObject m_config;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);

            LoadConfig();

            if (m_config != null && m_config.AutoInitialize)
            {
                InitializeSteam();
            }
        }

        private void LoadConfig()
        {
            m_config = UnityEngine.Resources.Load<SteamScriptableObject>("SteamConfig");
            if (m_config == null)
            {
                Debug.LogWarning("[SteamManager] SteamConfig not found in Resources. Please create one.");
            }
        }

        public void InitializeSteam()
        {
            if (m_isInitialized) return;

            // Only run in Steam builds. Local test builds define DISABLE_STEAM and compile this path out.
#if WALKBRAVE_STEAM_ENABLED
            if (Application.isEditor)
            {
                Debug.Log("[SteamManager] Skipping initialization in Editor (ENABLE_STEAM defined but Editor execution disabled).");
                return;
            }

            if (m_config == null)
            {
                LoadConfig();
                if (m_config == null) return;
            }

            try
            {
                // Check if Steamworks.NET is available and Steam client is running
                if (!Packsize.Test())
                {
                    Debug.LogError("[SteamManager] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.");
                }

                if (!DllCheck.Test())
                {
                    Debug.LogError("[SteamManager] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.");
                }

                if (SteamAPI.RestartAppIfNecessary((AppId_t)m_config.AppId))
                {
                    Debug.Log("[SteamManager] Restarting App via Steam...");
                    Application.Quit();
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[SteamManager] Error checking Steam requirements: " + e.Message);
                return;
            }

            m_isInitialized = SteamAPI.Init();

            if (!m_isInitialized)
            {
                Debug.LogError("[SteamManager] SteamAPI.Init() failed. Is Steam running?");
                return;
            }

            Debug.Log($"[SteamManager] Steam Initialized. AppID: {m_config.AppId}");
#else
            Debug.Log("[SteamManager] Steam integration disabled for this build.");
#endif
        }

        private void Update()
        {
            if (!m_isInitialized) return;

#if WALKBRAVE_STEAM_ENABLED
            // Run Steam callbacks
            SteamAPI.RunCallbacks();
#endif
        }

        private void OnApplicationQuit()
        {
            if (m_isInitialized)
            {
#if WALKBRAVE_STEAM_ENABLED
                SteamAPI.Shutdown();
#endif
                m_isInitialized = false;
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}
