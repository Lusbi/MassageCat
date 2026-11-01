using UnityEditor;
using UnityEngine;

namespace GameCore.Steam.Editor
{
    public class SteamDefineManager
    {
        private const string DEFINE_SYMBOL = "ENABLE_STEAM";

        [MenuItem("GameCore/Steam/Enable Steam Support")]
        public static void EnableSteam()
        {
            AddDefineSymbols(DEFINE_SYMBOL);
        }

        [MenuItem("GameCore/Steam/Disable Steam Support")]
        public static void DisableSteam()
        {
            RemoveDefineSymbols(DEFINE_SYMBOL);
        }

        private static void AddDefineSymbols(string symbol)
        {
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!definesString.Contains(symbol))
            {
                if (definesString.Length > 0) definesString += ";";
                definesString += symbol;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, definesString);
                Debug.Log($"[SteamDefineManager] Added {symbol} to Scripting Define Symbols.");
            }
            else
            {
                Debug.Log($"[SteamDefineManager] {symbol} is already defined.");
            }
        }

        private static void RemoveDefineSymbols(string symbol)
        {
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (definesString.Contains(symbol))
            {
                if (definesString.Contains(";" + symbol))
                {
                    definesString = definesString.Replace(";" + symbol, "");
                }
                else if (definesString.Contains(symbol + ";"))
                {
                    definesString = definesString.Replace(symbol + ";", "");
                }
                else
                {
                    definesString = definesString.Replace(symbol, "");
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, definesString);
                Debug.Log($"[SteamDefineManager] Removed {symbol} from Scripting Define Symbols.");
            }
            else
            {
                Debug.Log($"[SteamDefineManager] {symbol} is not defined.");
            }
        }
    }
}
