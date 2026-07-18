using UnityEngine;

namespace GameCore.Steam
{
    [CreateAssetMenu(fileName = "SteamConfig", menuName = "GameCore/Steam/SteamConfig")]
    public class SteamScriptableObject : ScriptableObject
    {
        [Tooltip("The Steam App ID for your game.")]
        public uint AppId = 480; // Default to Spacewar for testing

        [Tooltip("Should SteamManager initialize on start?")]
        public bool AutoInitialize = true;
    }
}
