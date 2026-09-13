#if UNITY_STANDALONE_WIN

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public sealed class WindowsInputHookManager : MonoBehaviour
{
    public readonly struct PointerEvent
    {
        public readonly int Button;
        public readonly Vector2 ScreenPosition;

        internal PointerEvent(int button, Vector2 screenPosition)
        {
            Button = button;
            ScreenPosition = screenPosition;
        }
    }

    private enum InputEventType : byte
    {
        KeyDown,
        MouseDown,
        MouseUp,
        MouseMove,
    }

    private readonly struct InputEvent
    {
        public readonly InputEventType Type;
        public readonly int Value;
        public readonly Vector2 ScreenPosition;

        public InputEvent(InputEventType type, int value, Vector2 screenPosition)
        {
            Type = type;
            Value = value;
            ScreenPosition = screenPosition;
        }
    }

    private static readonly LowLevelProc s_keyboardProc = KeyboardHookCallback;
    private static readonly LowLevelProc s_mouseProc = MouseHookCallback;
    private static readonly bool[] s_pressedKeys = new bool[256];
    private static readonly ConcurrentQueue<InputEvent> s_inputEvents = new();
    private static bool s_leftMouseButtonDown;
    private static IntPtr s_keyboardHookId = IntPtr.Zero;
    private static IntPtr s_mouseHookId = IntPtr.Zero;

    public static event Action<int> OnGlobalKeyDown;
    public static event Action<int> OnGlobalMouseDown;
    public static event Action<PointerEvent> OnGlobalPointerDown;
    public static event Action<PointerEvent> OnGlobalPointerUp;
    public static event Action<PointerEvent> OnGlobalPointerMove;

    private Thread m_hookThread;
    private ManualResetEventSlim m_hookReady;
    private Exception m_hookStartupException;
    private volatile uint m_hookThreadId;
    private int m_keyboardHookError;
    private int m_mouseHookError;

    private void Update()
    {
        int processedCount = 0;
        while (processedCount++ < k_maxInputEventsPerFrame && s_inputEvents.TryDequeue(out InputEvent inputEvent))
        {
            switch (inputEvent.Type)
            {
                case InputEventType.KeyDown:
                    OnGlobalKeyDown?.Invoke(inputEvent.Value);
                    break;
                case InputEventType.MouseDown:
                {
                    PointerEvent pointerEvent = new PointerEvent(inputEvent.Value, inputEvent.ScreenPosition);
                    OnGlobalPointerDown?.Invoke(pointerEvent);
                    OnGlobalMouseDown?.Invoke(inputEvent.Value);
                    break;
                }
                case InputEventType.MouseUp:
                    OnGlobalPointerUp?.Invoke(new PointerEvent(inputEvent.Value, inputEvent.ScreenPosition));
                    break;
                case InputEventType.MouseMove:
                    OnGlobalPointerMove?.Invoke(new PointerEvent(-1, inputEvent.ScreenPosition));
                    break;
            }
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartHookThread();
    }

    private void OnDestroy()
    {
        StopHookThread();
    }

    private void StartHookThread()
    {
        m_hookReady = new ManualResetEventSlim(false);
        m_hookThread = new Thread(RunHookMessageLoop)
        {
            IsBackground = true,
            Name = "MassageCat Windows Input Hook",
        };
        m_hookThread.Start();

        if (m_hookReady.Wait(k_hookStartupTimeoutMilliseconds) == false)
        {
            Debug.LogError("Windows 全域輸入 Hook 執行緒啟動逾時。");
            return;
        }

        if (m_hookStartupException != null)
        {
            Debug.LogException(m_hookStartupException);
            return;
        }

        if (m_keyboardHookError != 0)
            Debug.LogError($"Windows 全域鍵盤 Hook 安裝失敗，錯誤碼：{m_keyboardHookError}。");
        if (m_mouseHookError != 0)
            Debug.LogError($"Windows 全域滑鼠 Hook 安裝失敗，錯誤碼：{m_mouseHookError}。");
    }

    private void RunHookMessageLoop()
    {
        try
        {
            m_hookThreadId = GetCurrentThreadId();

            // 先建立執行緒訊息佇列，確保主執行緒可用 WM_QUIT 安全結束 Hook。
            NativeMessage unusedMessage;
            PeekMessage(out unusedMessage, IntPtr.Zero, 0, 0, k_pmNoRemove);
            Array.Clear(s_pressedKeys, 0, s_pressedKeys.Length);

            IntPtr moduleHandle = GetModuleHandle(null);
            s_keyboardHookId = SetWindowsHookEx(
                k_whKeyboardLowLevel,
                s_keyboardProc,
                moduleHandle,
                0);
            if (s_keyboardHookId == IntPtr.Zero)
                m_keyboardHookError = Marshal.GetLastWin32Error();

            s_mouseHookId = SetWindowsHookEx(
                k_whMouseLowLevel,
                s_mouseProc,
                moduleHandle,
                0);
            if (s_mouseHookId == IntPtr.Zero)
                m_mouseHookError = Marshal.GetLastWin32Error();
        }
        catch (Exception exception)
        {
            m_hookStartupException = exception;
        }
        finally
        {
            m_hookReady.Set();
        }

        if (m_hookStartupException != null ||
            (s_keyboardHookId == IntPtr.Zero && s_mouseHookId == IntPtr.Zero))
        {
            ReleaseHooks();
            return;
        }

        try
        {
            NativeMessage message;
            while (GetMessage(out message, IntPtr.Zero, 0, 0) > 0)
            {
                // 低階 Hook 回呼會由 GetMessage 派送，不需額外 DispatchMessage。
            }
        }
        finally
        {
            ReleaseHooks();
            m_hookThreadId = 0;
        }
    }

    private void StopHookThread()
    {
        Thread hookThread = m_hookThread;
        uint hookThreadId = m_hookThreadId;
        bool hookThreadStopped = hookThread == null || hookThread.IsAlive == false;

        if (hookThreadStopped == false && hookThreadId != 0)
        {
            PostThreadMessage(hookThreadId, k_wmQuit, IntPtr.Zero, IntPtr.Zero);
            if (Thread.CurrentThread != hookThread)
            {
                hookThreadStopped = hookThread.Join(k_hookShutdownTimeoutMilliseconds);
                if (hookThreadStopped == false)
                    Debug.LogWarning("Windows 全域輸入 Hook 執行緒未在期限內結束。");
            }
        }

        if (hookThreadStopped)
            m_hookReady?.Dispose();

        m_hookReady = null;
        m_hookThread = null;
    }

    private static void ReleaseHooks()
    {
        if (s_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(s_keyboardHookId);
            s_keyboardHookId = IntPtr.Zero;
        }

        if (s_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(s_mouseHookId);
            s_mouseHookId = IntPtr.Zero;
        }
    }

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int virtualKey = Marshal.ReadInt32(lParam);
            if (wParam == (IntPtr)k_wmKeyDown)
            {
                if ((uint)virtualKey < s_pressedKeys.Length && s_pressedKeys[virtualKey] == false)
                {
                    s_pressedKeys[virtualKey] = true;
                    s_inputEvents.Enqueue(new InputEvent(InputEventType.KeyDown, virtualKey, Vector2.zero));
                }
            }
            else if (wParam == (IntPtr)k_wmKeyUp && (uint)virtualKey < s_pressedKeys.Length)
            {
                s_pressedKeys[virtualKey] = false;
            }
        }

        return CallNextHookEx(s_keyboardHookId, nCode, wParam, lParam);
    }

    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            NativeMouseHookStruct mouseHookStruct = Marshal.PtrToStructure<NativeMouseHookStruct>(lParam);
            Vector2 screenPosition = new Vector2(mouseHookStruct.Point.X, mouseHookStruct.Point.Y);
            int button = -1;
            InputEventType inputEventType = InputEventType.MouseMove;
            if (wParam == (IntPtr)k_wmLeftButtonDown)
            {
                button = 0;
                inputEventType = InputEventType.MouseDown;
            }
            else if (wParam == (IntPtr)k_wmRightButtonDown)
            {
                button = 1;
                inputEventType = InputEventType.MouseDown;
            }
            else if (wParam == (IntPtr)k_wmMiddleButtonDown)
            {
                button = 2;
                inputEventType = InputEventType.MouseDown;
            }
            else if (wParam == (IntPtr)k_wmLeftButtonUp)
            {
                button = 0;
                inputEventType = InputEventType.MouseUp;
            }
            else if (wParam == (IntPtr)k_wmRightButtonUp)
            {
                button = 1;
                inputEventType = InputEventType.MouseUp;
            }
            else if (wParam == (IntPtr)k_wmMiddleButtonUp)
            {
                button = 2;
                inputEventType = InputEventType.MouseUp;
            }
            else if (wParam != (IntPtr)k_wmMouseMove)
            {
                return CallNextHookEx(s_mouseHookId, nCode, wParam, lParam);
            }

            if (inputEventType == InputEventType.MouseDown && button == 0)
                s_leftMouseButtonDown = true;
            else if (inputEventType == InputEventType.MouseUp && button == 0)
                s_leftMouseButtonDown = false;
            else if (inputEventType == InputEventType.MouseMove && s_leftMouseButtonDown == false)
                return CallNextHookEx(s_mouseHookId, nCode, wParam, lParam);

            s_inputEvents.Enqueue(new InputEvent(inputEventType, button, screenPosition));
        }

        return CallNextHookEx(s_mouseHookId, nCode, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMouseHookStruct
    {
        public NativePoint Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr WindowHandle;
        public uint Message;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public NativePoint Point;
        public uint PrivateData;
    }

    private const int k_whKeyboardLowLevel = 13;
    private const int k_whMouseLowLevel = 14;
    private const int k_wmKeyDown = 0x0100;
    private const int k_wmKeyUp = 0x0101;
    private const int k_wmLeftButtonDown = 0x0201;
    private const int k_wmLeftButtonUp = 0x0202;
    private const int k_wmRightButtonDown = 0x0204;
    private const int k_wmRightButtonUp = 0x0205;
    private const int k_wmMiddleButtonDown = 0x0207;
    private const int k_wmMiddleButtonUp = 0x0208;
    private const int k_wmMouseMove = 0x0200;
    private const uint k_wmQuit = 0x0012;
    private const uint k_pmNoRemove = 0x0000;
    private const int k_hookStartupTimeoutMilliseconds = 2000;
    private const int k_hookShutdownTimeoutMilliseconds = 2000;
    private const int k_maxInputEventsPerFrame = 64;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelProc hookProcedure,
        IntPtr moduleHandle,
        uint threadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hookHandle,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(
        out NativeMessage message,
        IntPtr windowHandle,
        uint minimumFilter,
        uint maximumFilter,
        uint removeMessage);

    [DllImport("user32.dll")]
    private static extern int GetMessage(
        out NativeMessage message,
        IntPtr windowHandle,
        uint minimumFilter,
        uint maximumFilter);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(
        uint threadId,
        uint message,
        IntPtr wParam,
        IntPtr lParam);
}
#endif
