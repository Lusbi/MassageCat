#if UNITY_STANDALONE_WIN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class WindowsInputHookManager : MonoBehaviour
{
    private static IntPtr _keyboardHookId = IntPtr.Zero;
    private static IntPtr _mouseHookId = IntPtr.Zero;
    private static LowLevelProc _keyboardProc = KeyboardHookCallback;
    private static LowLevelProc _mouseProc = MouseHookCallback;

    public static event Action<int> OnGlobalKeyDown;
    public static event Action<int> OnGlobalMouseDown;

    private static readonly HashSet<int> _pressedKeys = new(); // 防止重複觸發

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SetHooks();
    }

    private void OnDestroy()
    {
        UnhookWindowsHookEx(_keyboardHookId);
        UnhookWindowsHookEx(_mouseHookId);
    }

    private static void SetHooks()
    {
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule curModule = curProcess.MainModule;

        _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc,
            GetModuleHandle(curModule.ModuleName), 0);

        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc,
            GetModuleHandle(curModule.ModuleName), 0);
    }

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            //KeyCode key = (KeyCode)vkCode;

            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                if (_pressedKeys.Add(vkCode)) // 第一次按下才觸發
                {
                    UnityMainThreadDispatcher.Enqueue(() => OnGlobalKeyDown?.Invoke(vkCode));
                }
            }
            else if (wParam == (IntPtr)WM_KEYUP)
            {
                _pressedKeys.Remove(vkCode); // 鍵放開就允許再次觸發
            }
        }

        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int button = -1;
            if (wParam == (IntPtr)WM_LBUTTONDOWN) button = 0;
            else if (wParam == (IntPtr)WM_RBUTTONDOWN) button = 1;
            else if (wParam == (IntPtr)WM_MBUTTONDOWN) button = 2;

            if (button >= 0)
            {
                UnityMainThreadDispatcher.Enqueue(() => OnGlobalMouseDown?.Invoke(button));
            }
        }

        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    // 常數定義
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;

    // Win32 API
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
#endif
