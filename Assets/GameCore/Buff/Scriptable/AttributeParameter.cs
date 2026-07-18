using Sirenix.OdinInspector;
using System;

namespace GameCore.Attribute
{
    [Serializable]
    public class AttributeParameterInfo
    {
        [HideLabel, HorizontalGroup]
        public AttributeType attributeType;
        [HideLabel, HorizontalGroup]
        public float value;

        public void FromCSV(string text)
        {
            string[] split = text.Split('_');
            if (string.IsNullOrEmpty(text) || split.Length < 1)
            {
                return;
            }
            Enum.TryParse(split[0] , out attributeType);
            value = float.Parse(split[1]);
        }

        public string ToCSV()
        {
            return $"{attributeType}_{value}";
        }
    }

    [Serializable]
    public class AttributeParameter
    {
        public AttributeParameterInfo[] attributeParameterInfos;

        public void FromCSV(string text)
        {
            string[] texts = text.Split('&');
            attributeParameterInfos = new AttributeParameterInfo[texts.Length];
            for (int i = 0; i < texts.Length; i ++)
            {
                attributeParameterInfos[i] = new AttributeParameterInfo();
                attributeParameterInfos[i].FromCSV(texts[i]);
            }
        }

        public string ToCSV()
        {
            string result = string.Empty;
            for (int i = 0; i < attributeParameterInfos.Length; i ++)
            {
                result += attributeParameterInfos[i].ToCSV() + "&";
            }
            return result.Substring(0,result.Length-1);
        }
    }
}