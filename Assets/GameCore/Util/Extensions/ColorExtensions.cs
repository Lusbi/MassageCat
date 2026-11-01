using System;
using UnityEngine;

namespace JSL.UnityColor.Extensions
{
    public static class ColorExtensions
    {
        public static Color Blend(this Color target, BlendOp blendOp, Color source) => blendOp switch
        {
            BlendOp.Override => source,
            BlendOp.Add => target + source,
            BlendOp.Subtract => target - source,
            BlendOp.Multiply => target * source,
            BlendOp.MultiplyAlpha => target * source.a,
            _ => throw new ArgumentOutOfRangeException(nameof(blendOp)),
        };
    }
}