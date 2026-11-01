using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace GameCore.CSV
{
    public class CSVUtils
    {
        public const string NEW_LINE = "\r\n";
        public const char SEPARATOR = ',';
        public const char ARRAY_SEPARATOR = ';';
        public const char ARRAYS_SEPARATOR = '-';

        public static string[] GetFieldNames(string text)
        {
            return GetLines(text)[0].Split(new char[] { SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static int GetColumnLength(string text)
        {
            return GetLines(text).Length - 1;
        }

        private static string[] GetLines(string text)
        {
            return text.Split(new string[] { NEW_LINE }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static object Parse(Type fieldType, string csv, int col, string fieldName)
        {
            string fieldValue = GetFieldValue(csv, col, fieldName);
            return Parse(fieldType, fieldValue);
        }

        public static object Parse(Type fieldType, string value)
        {
            if (fieldType == typeof(Vector2))
            {
                return ParseVector2(value);
            }
            else if (fieldType == typeof(Vector3))
            {
                return ParseVector3(value);
            }
            else if (fieldType == typeof(Vector4))
            {
                return ParseVector4(value);
            }
            else if (fieldType == typeof(Color))
            {
                if (!value.StartsWith('#'))
                    value = "#" + value;
                if (ColorUtility.TryParseHtmlString(value, out Color c))
                    return c;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(fieldType);
            if (converter != null && converter.IsValid(value))
            {
                return converter.ConvertFromString(value);
            }

            return null;
        }

        private static Vector2 ParseVector2(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Vector2.zero;
            }

            string[] splits = ParseVector(value);

            Vector2 vector;
            float.TryParse(splits[0], out vector.x);
            float.TryParse(splits[1], out vector.y);
            return vector;
        }

        private static Vector3 ParseVector3(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Vector3.zero;
            }

            string[] splits = ParseVector(value);

            Vector3 vector;
            float.TryParse(splits[0], out vector.x);
            float.TryParse(splits[1], out vector.y);
            float.TryParse(splits[2], out vector.z);
            return vector;
        }

        private static Vector4 ParseVector4(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Vector4.zero;
            }

            string[] splits = ParseVector(value);

            Vector4 vector;
            float.TryParse(splits[0], out vector.x);
            float.TryParse(splits[1], out vector.y);
            float.TryParse(splits[2], out vector.z);
            float.TryParse(splits[3], out vector.w);
            return vector;
        }

        private static string[] ParseVector(string value)
        {
            value = value.Replace("\"", string.Empty);
            value = value.Replace("(", string.Empty);
            value = value.Replace(")", string.Empty);
            return value.Split(SEPARATOR);
        }

        public static object ParseArray(Type fieldType, string csv, int col, string fieldName)
        {
            string fieldValue = GetFieldValue(csv, col, fieldName);
            if (string.IsNullOrEmpty(fieldValue))
            {
                return null;
            }

            fieldValue = fieldValue.Trim('"');

            string[] fieldValues = fieldValue.Split(ARRAY_SEPARATOR);
            if (fieldValues == null || fieldValues.Length == 0)
            {
                return null;
            }

            Array array = Array.CreateInstance(fieldType, fieldValues.Length);
            for (int i = 0; i < fieldValues.Length; i++)
            {
                array.SetValue(Parse(fieldType, fieldValues[i]), i);
            }

            return array;
        }

        private static string GetFieldValue(string text, int col, string fieldName)
        {
            return FromCSV(text)[fieldName][col];
        }

        private static Dictionary<string, string[]> FromCSV(string text)
        {
            string[] fields = GetFieldNames(text);
            string[] lines = GetLines(text);

            Dictionary<string, string[]> csv = new Dictionary<string, string[]>();

            for (int i = 0; i < fields.Length; i++)
            {
                string[] datas = new string[lines.Length - 1];
                csv.Add(fields[i], datas);
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string[] datas = lines[i].Split(SEPARATOR);
                int fieldIndex = -1;
                string data = string.Empty;

                for (int j = 0; j < datas.Length; j++)
                {
                    if (string.IsNullOrEmpty(data) || data[0] != '\"')
                    {
                        fieldIndex++;
                    }
                    else
                    {
                        data += SEPARATOR;
                    }

                    data += datas[j];

                    if (string.IsNullOrEmpty(data) ||
                        !data.Contains("\"") ||
                        (data[0] == '\"' && data[data.Length - 1] == '\"'))
                    {
                        csv[fields[fieldIndex]][i - 1] = data;
                        data = string.Empty;
                    }
                }
            }

            return csv;
        }
    }
}