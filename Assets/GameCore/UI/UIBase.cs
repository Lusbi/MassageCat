using UnityEngine;
using System;
using GameCore.Resources;

namespace GameCore.UI
{
    public abstract class UIBase : IInitlization
    {
        public UIBase view;
        public long stateID;
        public int identifier;

        public UIBase(System.Action action = null, params Enum[] enums)
        {
            
        }

        public virtual void OnUpdate(float time) { }

        public virtual void Destory()
        {
            UIManager.instance.UnRegistered(this);
        }

        public virtual void Initlization(Action callBack = null)
        {
            
        }

        public virtual void Active(bool active)
        {
            
        }
    }
}