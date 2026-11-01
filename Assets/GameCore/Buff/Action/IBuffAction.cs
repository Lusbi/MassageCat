using GameCore.Attribute;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Buff
{
    public interface IBuffAction
    {
        public void Execute(object parameters);
    }
}