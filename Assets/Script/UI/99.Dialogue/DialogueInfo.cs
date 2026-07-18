using UnityEngine;

public struct DialogueInfo
{
    /// <summary>
    /// 說話者名稱
    /// </summary>
    public string talkName;
    /// <summary>
    /// 對話內容
    /// </summary>
    public string content;
    /// <summary>
    /// 持續時間
    /// </summary>
    public float durationTime;
    /// <summary>
    /// 是否可以點擊取消
    /// </summary>
    public bool blockClick;
}
