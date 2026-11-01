using JSL.UnityGUIEvent.Extensions;
using UnityEngine;

namespace JSL.UnityRect.Extensions
{
    public static class UnityRectExtensions
    {
        public static bool MouseDown(this Rect rect, int button)
        {
            Event e = Event.current;
            return e.IsMouseDown(button) && rect.Contains(e.mousePosition);
        }
    }
}