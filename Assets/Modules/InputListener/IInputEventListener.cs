using UnityEngine;

public interface IInputEventListener
{
    void OnKeyDown(KeyCode keyCode); // 鍵盤按下事件
    void OnKeyUp(KeyCode keyCode);   // 鍵盤釋放事件
    void OnMouseClick(int button, Vector3 position); // 滑鼠點擊事件
}
