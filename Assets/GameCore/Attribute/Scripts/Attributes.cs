using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Attribute
{
    public class Attributes : MonoBehaviour
    {
        private List<AttributeBase> attributeBases = new List<AttributeBase>();

        public int instanceID { get; private set; }

        public Attributes Setup(int instanceID)
        {
            this.instanceID = instanceID;
            return this;
        }

        public Attributes Initlization()
        {
            foreach (AttributeBase attributeBase in attributeBases)
            {
                attributeBase.SetCurrentValue(0);
            }
            return this;
        }

        public Attributes SetAttribute(AttributeType attributeType , float value)
        {
            AttributeBase attributeBase = GetAttributeBase(attributeType);
            if (attributeBase != null)
            {
                attributeBase.Setup(this);
                attributeBase.SetCurrentValue(value);
            }
            return this;
        }

        private AttributeBase GetAttributeBase(AttributeType attributeType)
        {
            foreach (AttributeBase attributeBase in attributeBases)
            {
                if (attributeBase.attributeType == attributeType)
                {
                    return attributeBase;
                }
            }

            return AttributeFactory.Get(attributeType);
        }

        public float GetAttributeValue(AttributeType attributeType)
        {
            foreach (AttributeBase attributeBase in attributeBases)
            {
                if (attributeType == attributeBase.attributeType)
                {
                    return attributeBase.GetValueFormula();
                }
            }
            return 0;
        }

    }
}