using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameCore.Steam.Editor
{
    public static class WalkBraveBuildTool
    {
        private const string k_enableSteamDefine = "ENABLE_STEAM";
        private const string k_disableSteamDefine = "DISABLE_STEAM";
        private const string k_steamBuildDirectory = "Steam";
        private const string k_localTestBuildDirectory = "LocalTest";
        private const string k_executableName = "WalkBrave.exe";

        private enum BuildMode
        {
            SteamRelease,
            LocalTestNoSteam
        }

        [MenuItem("GameCore/Build/Windows/Steam Release")]
        public static void BuildSteamRelease()
        {
            BuildWindows(BuildMode.SteamRelease);
        }

        [MenuItem("GameCore/Build/Windows/Local Test - No Steam")]
        public static void BuildLocalTestNoSteam()
        {
            BuildWindows(BuildMode.LocalTestNoSteam);
        }

        private static void BuildWindows(BuildMode buildMode)
        {
            string locationPathName = GetLocationPathName(buildMode);
            string directoryName = Path.GetDirectoryName(locationPathName);
            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = locationPathName,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = GetBuildOptions(buildMode),
                extraScriptingDefines = GetExtraScriptingDefines(buildMode)
            };

            Debug.Log(
                $"[WalkBraveBuildTool] Build started. Mode: {buildMode}, Output: {locationPathName}, Defines: {string.Join(";", buildPlayerOptions.extraScriptingDefines)}");

            BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = buildReport.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception(
                    $"[WalkBraveBuildTool] Build failed. Result: {summary.result}, Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}");
            }

            Debug.Log(
                $"[WalkBraveBuildTool] Build succeeded. Output: {summary.outputPath}, Size: {summary.totalSize} bytes");
        }

        private static string[] GetEnabledScenePaths()
        {
            EditorBuildSettingsScene[] editorScenes = EditorBuildSettings.scenes;
            int enabledSceneCount = 0;
            for (int i = 0; i < editorScenes.Length; i++)
            {
                if (editorScenes[i].enabled)
                {
                    enabledSceneCount++;
                }
            }

            if (enabledSceneCount == 0)
            {
                throw new Exception("[WalkBraveBuildTool] No enabled scenes found in EditorBuildSettings.");
            }

            string[] scenePaths = new string[enabledSceneCount];
            int sceneIndex = 0;
            for (int i = 0; i < editorScenes.Length; i++)
            {
                if (!editorScenes[i].enabled)
                {
                    continue;
                }

                scenePaths[sceneIndex] = editorScenes[i].path;
                sceneIndex++;
            }

            return scenePaths;
        }

        private static string GetLocationPathName(BuildMode buildMode)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string buildDirectory = buildMode == BuildMode.SteamRelease
                ? k_steamBuildDirectory
                : k_localTestBuildDirectory;

            return Path.Combine(projectRoot, "Builds", buildDirectory, k_executableName);
        }

        private static BuildOptions GetBuildOptions(BuildMode buildMode)
        {
            return buildMode == BuildMode.LocalTestNoSteam
                ? BuildOptions.Development
                : BuildOptions.None;
        }

        private static string[] GetExtraScriptingDefines(BuildMode buildMode)
        {
            return buildMode == BuildMode.SteamRelease
                ? new[] { k_enableSteamDefine }
                : new[] { k_disableSteamDefine };
        }
    }
}
