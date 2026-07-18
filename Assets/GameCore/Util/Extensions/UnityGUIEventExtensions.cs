using UnityEngine;

namespace JSL.UnityGUIEvent.Extensions
{
    public static class UnityGUIEventExtensions
    {
        public static bool IsKeyDown(this Event e, KeyCode keyCode) => e.type == EventType.KeyDown && e.keyCode == keyCode;

        public static bool IsKeyUp(this Event e, KeyCode keyCode) => e.type == EventType.KeyUp && e.keyCode == keyCode;

        public static bool IsMouseDown(this Event e, int button) => e.type == EventType.MouseDown && e.button == button;

        public static bool IsMouseUp(this Event e, int button) => e.type == EventType.MouseUp && e.button == button;
    }
}