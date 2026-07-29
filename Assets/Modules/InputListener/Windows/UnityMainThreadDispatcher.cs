using System.Collections.Concurrent;
using System;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static volatile bool s_inputListenerState = true;
    private static readonly ConcurrentQueue<Action> s_queue = new();
    private static readonly object s_inputStateLock = new();
    private const int k_maxPerFrame = 10;
    private static bool s_hasPendingInputRestore;
    private static bool s_pendingInputRestoreState;
    private static float s_pendingInputRestoreTime;

    public static void Enqueue(Action action)
    {
        if (action == null)
            return;

        // 與停用／清空操作共用鎖，避免 Hook 執行緒在清空後補入舊輸入。
        lock (s_inputStateLock)
        {
            if (!s_inputListenerState)
                return;

            s_queue.Enqueue(action);
        }
    }

    private void LateUpdate()
    {
        TryRestoreInputListenerState();

        if (!s_inputListenerState)
            return;

        int count = 0;
        while (count++ < k_maxPerFrame && s_queue.TryDequeue(out Action action))
        {
            if (!s_inputListenerState)
                return;

            action?.Invoke();
        }
    }

    /// <summary>
    /// 按鍵監聽開關
    /// </summary>
    /// <param name="state"></param>
    public static void SetInputListenerState(bool state)
    {
        // 明確狀態切換優先於 UI 的短暫抑制，避免舊延遲回呼覆寫教學或彈窗狀態。
        s_hasPendingInputRestore = false;
        ApplyInputListenerState(state);
    }

    private static void ApplyInputListenerState(bool state)
    {
        bool previousState;
        int clearedCount = 0;

        lock (s_inputStateLock)
        {
            previousState = s_inputListenerState;
            s_inputListenerState = state;

            if (!state)
            {
                while (s_queue.TryDequeue(out _))
                    clearedCount++;
            }
        }

        if (previousState != state)
        {
            TutorialFlowDiagnostics.Log(
                $"輸入監聽狀態切換：previous={previousState}, current={state}, clearedPending={clearedCount}",
                null);
        }
    }

    /// <summary>
    /// 開關按鍵監聽，延遲恢復
    /// </summary>
    /// <param name="state"></param>
    /// <param name="delay"></param>
    public static void DelaySetInputListenerState(bool state, float delay)
    {
        // 由 Dispatcher 自己管理恢復時間，不依賴可能被場景或物件生命週期取消的 Tween。
        s_hasPendingInputRestore = false;
        ApplyInputListenerState(state);
        s_pendingInputRestoreState = !state;
        s_pendingInputRestoreTime = Time.unscaledTime + Mathf.Max(0f, delay);
        s_hasPendingInputRestore = true;
    }

    private static void TryRestoreInputListenerState()
    {
        if (!s_hasPendingInputRestore || Time.unscaledTime < s_pendingInputRestoreTime)
            return;

        bool restoreState = s_pendingInputRestoreState;
        s_hasPendingInputRestore = false;
        ApplyInputListenerState(restoreState);
    }
}
