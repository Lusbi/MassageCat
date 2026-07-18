using System.Collections.Concurrent;
using System;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static volatile bool s_inputListenerState = true;
    private static readonly ConcurrentQueue<Action> s_queue = new();
    private static readonly object s_inputStateLock = new();
    private const int k_maxPerFrame = 10;

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
        SetInputListenerState(state);
        PrimeTween.Tween.Delay(delay, () =>
                {
                    SetInputListenerState(!state);
                }, true);
    }
}
