using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace MassageCat.Gallery
{
    internal static class GalleryProcessLauncher
    {
        private const string k_galleryExecutableName = "MassageCatGallery.exe";
        private const string k_desktopPetExecutableName = "MassageCat.exe";
        private const string k_legacyDesktopPetExecutableName = "WalkBrave.exe";

        internal static bool OpenGallery()
        {
            if (DesktopPetGalleryBridge.Instance != null
                && DesktopPetGalleryBridge.Instance.RequestGalleryFocus())
            {
                return true;
            }

            return TryStartExecutable(k_galleryExecutableName);
        }

        internal static bool OpenDesktopPet()
        {
            if (TryStartExecutable(k_desktopPetExecutableName))
                return true;

            return TryStartExecutable(k_legacyDesktopPetExecutableName);
        }

        private static bool TryStartExecutable(string executableName)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"Editor 模式不會啟動外部執行檔：{executableName}");
            return false;
#else
            string executablePath = Path.Combine(AppContext.BaseDirectory, executableName);
            if (File.Exists(executablePath) == false)
            {
                UnityEngine.Debug.LogWarning($"找不到外部執行檔：{executablePath}");
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true,
                    WorkingDirectory = AppContext.BaseDirectory,
                });
                return true;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"啟動外部執行檔失敗：{exception.Message}");
                return false;
            }
#endif
        }
    }
}
