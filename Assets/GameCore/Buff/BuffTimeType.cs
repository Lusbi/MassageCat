using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace GameCore.Buff
{
    public enum BuffTimeType
    {
        [Description("立即性")]
        Immediately,
        [Description("持續性")]
        Interval,
        [Description("常駐型")]
        Permanent,
    }
}