using UnityEngine;

internal static class TutorialFlowDiagnostics
{
    private const string k_logPrefix = "[TutorialFlow] ";

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    internal static void Log(string message, Object context = null)
    {
        Debug.Log(k_logPrefix + message, context);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    internal static void LogWarning(string message, Object context = null)
    {
        Debug.LogWarning(k_logPrefix + message, context);
    }
}
