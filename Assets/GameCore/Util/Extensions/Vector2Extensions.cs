using UnityEngine;

namespace JSL.UnityVector2.Extensions
{
    public static class Vector2Extensions
    {
        public static float RandomRange(this Vector2 v) => Random.Range(v.x, v.y);
    }
}