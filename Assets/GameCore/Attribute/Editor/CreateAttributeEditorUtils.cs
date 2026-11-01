using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

namespace GameCore.Attribute.Editor
{
    public class CreateAttributeUtils
    {
        private const string ROOT_PATH = "Assets/GameCore/Attribute";
        private const string TEMPLATE_PATH = ROOT_PATH + "/Template/";
        private const string CREATE_PATH = ROOT_PATH + "/Scripts";

        private const string TEMPLATE_FACTORY_PATH = TEMPLATE_PATH + "TemplateAttributeFactory.txt";
        private const string TEMPLATE_CLASS_PATH = TEMPLATE_PATH + "TemplateAttributeClass.txt";
        private const string TEMPLATE_TYPE_PATH = TEMPLATE_PATH + "TemplateAttributeType.txt";

        private const string TEMPLATE_SUMMARY = "@{summary}";
        private const string TEMPLATE_TYPE = "@{Type}";
        private const string TEMPLATE_FACTORY = "@{FactoryCase}";
        private const string TEMPLATE_ATTRIBUTETYPE = "@{attribute}";
        private const string TEMPLATE_FORMULA = "@{Formula}";

        public static void Create(string name, List<AttributeTypeInfo> attributeTypeInfos, string formula = null , string summary = null)
        {
            CreateRoot();
            CreateType(attributeTypeInfos);
            CreateFactory(attributeTypeInfos);
            CreateScript(name , formula, summary);
        }

        public static void Remove(List<AttributeTypeInfo> attributeTypeInfos)
        {
            CreateRoot();

            CreateType(attributeTypeInfos);
            CreateFactory(attributeTypeInfos);

            foreach (AttributeTypeInfo attributeTypeInfo in attributeTypeInfos)
            {
                if (attributeTypeInfo.removeToggle)
                {
                    RemoveScript(attributeTypeInfo.typeName);
                }
            }

            AssetDatabase.Refresh();
        }

        public static void CreateRoot()
        {
            if (!Directory.Exists(CREATE_PATH))
            {
                Directory.CreateDirectory(CREATE_PATH);
            }
        }

        public static string GetScriptPath(string dataName)
        {
            return Path.Combine(ROOT_PATH , dataName);
        }

        private static void CreateType(List<AttributeTypeInfo> attributeTypeInfos)
        {
            string path = ROOT_PATH;
            path = Path.Combine(path, "Attribute");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, "AttributeType" + ".cs");

            StreamWriter sr = File.CreateText(path);
            string text = GetTemplate(TEMPLATE_TYPE_PATH);
            text = text.Replace(TEMPLATE_ATTRIBUTETYPE, GetAttributeTypeString(attributeTypeInfos));
            sr.WriteLine(text);
            sr.Close();

            AssetDatabase.Refresh();
        }

        private static string GetAttributeTypeString(List<AttributeTypeInfo> attributeTypeInfos)
        {
            string result = string.Empty;

            attributeTypeInfos.Sort((x, y) =>
            {
                if (x.index <= y.index)
                {
                    return 1;
                }
                return 0;
            });

            foreach (AttributeTypeInfo attributeTypeInfo in attributeTypeInfos)
            {
                if (attributeTypeInfo.removeToggle)
                {
                    continue;
                }
                result += $"\t\t{attributeTypeInfo.typeName} = {attributeTypeInfo.index} ,\n";
            }
            return result;
        }

        private static void CreateFactory(List<AttributeTypeInfo> attributeTypeInfos)
        {
            string path = ROOT_PATH;
            path = Path.Combine(path, "Attribute");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, "AttributeFactory" + ".cs");

            StreamWriter sr = File.CreateText(path);
            string text = GetTemplate(TEMPLATE_FACTORY_PATH);
            text = text.Replace(TEMPLATE_FACTORY, GetAttributeFactoryString(attributeTypeInfos));
            sr.WriteLine(text);
            sr.Close();

            AssetDatabase.Refresh();
        }

        private static string GetAttributeFactoryString(List<AttributeTypeInfo> attributeTypeInfos)
        {
            string result = string.Empty;

            attributeTypeInfos.Sort((x, y) =>
            {
                if (x.index <= y.index)
                {
                    return 1;
                }
                return 0;
            });

            foreach (AttributeTypeInfo attributeTypeInfo in attributeTypeInfos)
            {
                if (attributeTypeInfo.removeToggle)
                {
                    continue;
                }
                result += $"\t\t\t\tcase AttributeType.{attributeTypeInfo.typeName}: \t return new {attributeTypeInfo.typeName}Attribute(); \n";
            }
            return result;
        }

        private static void CreateScript(string name, string formula = null,string summary = null)
        {
            string path = CREATE_PATH;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, $"{name}Attribute" + ".cs");

            StreamWriter sr = File.CreateText(path);
            string text = GetTemplate(TEMPLATE_CLASS_PATH);
            text = text.Replace(TEMPLATE_TYPE, name);
            text = text.Replace(TEMPLATE_SUMMARY, summary);
            if (string.IsNullOrEmpty(formula))
            {
                text = text.Replace(TEMPLATE_FORMULA, "base.GetValueFormula();");
            }
            else
            {
                text = text.Replace(TEMPLATE_FORMULA, GetFormulaString(formula));
            }
            sr.WriteLine(text);
            sr.Close();

            AssetDatabase.Refresh();
        }

        private const string ATTRIBUTE_FORMULA = "Get(AttributeType.{0})";
        private static string GetFormulaString(string formula)
        {
            return ConvertFormulaWithEnum(formula);
        }

        /// <summary>
        /// AI
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public static string ConvertFormulaWithEnum(string formula)
        {
            Regex enumRegex = new Regex(@"\b(?<match>[a-zA-Z]+)\b");
            MatchCollection matches = enumRegex.Matches(formula);

            string result = enumRegex.Replace(formula, match =>
            {
                if (Enum.TryParse<AttributeType>(match.Groups["match"].Value, out AttributeType enumValue))
                {
                    return string.Format(ATTRIBUTE_FORMULA, enumValue);
                }
                else
                {
                    return match.Value;
                }
            });

            return result;
        }


        private static string GetTemplate(string path)
        {
            TextAsset txt = (TextAsset)AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset));
            if (txt == null)
            {
                Debug.LogError($"{path}");
                return string.Empty;
            }
            return txt.text;
        }

        private static void RemoveScript(string name)
        {
            string scriptName = $"{name}Attribute";
            string[] guids = AssetDatabase.FindAssets(scriptName);
            if (guids == null)
            {
                return;
            }
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string check = path.Remove( 0 , path.LastIndexOf("/")+1);
                if (check.Equals($"{scriptName}.cs"))
                {
                    string localPath = Application.dataPath.Replace("Assets", path);

                    File.Delete(localPath);
                    //JSLDebug.LogError($"Remove {path}");
                }
            }
        }
    }
}