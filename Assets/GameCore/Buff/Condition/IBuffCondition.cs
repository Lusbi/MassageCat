using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Buff
{
    public interface IBuffCondition
    {
        bool Valid(object parameters);
    }
}