#if UNITY_EDITOR
using GameCore.CSV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;

namespace GameCore.Database
{
    public class DatabaseCSV
    {
        private const string TAG = "DatabaseCSV";
        private const string LOG_SERIALIZE_FINISHED = "Serialize {0} to csv finished, cost {1} seconds.";
        private const string LOG_DESERIALIZE_FINISHED = "Deserialize {0} from csv finished, cost {1} seconds.";
        private static readonly string[] DEFAULT_FIELD_NAMES = { "key", "m_titlePath", "m_comment" };

        public static string ToCSV<T>(T[] datas) where T : Data
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            using var _ = ListPool<FieldInfo>.Get(out var fieldInfos);
            GetFieldInfoList<T>(fieldInfos);

            StringBuilder stringBuilder = new();
            FieldInfo fieldInfo;
            for (int i = 0; i < fieldInfos.Count; i++)
            {
                fieldInfo = fieldInfos[i];

                if (fieldInfo == null)
                    continue;
                if (i != 0)
                {
                    stringBuilder.Append(CSVUtils.SEPARATOR);
                }

                stringBuilder.Append(fieldInfo.Name);
            }
            stringBuilder.AppendLine();

            for (int i = 0; i < datas.Length; i++)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("Serializing CSV...{0}/{1}", i + 1, datas.Length),
                    string.Empty,
                    (float)(i + 1) / datas.Length))
                {
                    break;
                }

                T data = datas[i];

                for (int j = 0; j < fieldInfos.Count; j++)
                {
                    fieldInfo = fieldInfos[j];
                    if (fieldInfo == null)
                        continue;

                    if (j != 0)
                    {
                        stringBuilder.Append(CSVUtils.SEPARATOR);
                    }

                    object fieldValue = fieldInfo.GetValue(data);
                    if (fieldValue == null)
                        continue;

                    Type fieldType = fieldInfo.FieldType;
                    string output = GetSerializedFieldOutput(fieldValue, fieldType);

                    stringBuilder.Append(output);
                }

                stringBuilder.AppendLine();
            }

            UnityEditor.EditorUtility.ClearProgressBar();

            stopwatch.Stop();
            //JSLDebug.LogFormatTag(TAG, LOG_SERIALIZE_FINISHED, typeof(T).Name, stopwatch.ElapsedMilliseconds * 0.001f);

            return stringBuilder.ToString();
        }

        public static IList<IList<object>> ToArrays<T>(IEnumerable<T> data) where T : Data
        {
            using var _ = ListPool<FieldInfo>.Get(out var fieldInfoList);
            GetFieldInfoList<T>(fieldInfoList);
            using var __ = ListPool<IList<object>>.Get(out var rows);
            rows.Add(fieldInfoList.Select(x => x.Name).ToArray());

            object fieldValue;
            Type fieldType;
            foreach (T t in data)
            {
                using var ___ = ListPool<string>.Get(out var row);
                foreach (var fieldInfo in fieldInfoList)
                {
                    fieldValue = fieldInfo.GetValue(t);
                    if (fieldValue == null)
                        continue;

                    fieldType = fieldInfo.FieldType;
                    row.Add(GetSerializedFieldOutput(fieldValue, fieldType));
                }
                rows.Add(row.ToArray());
            }

            return rows.ToArray();
        }

        public static T[] FromCSV<T>(string text) where T : Data
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            string[] fieldNames = CSVUtils.GetFieldNames(text);
            int columnLength = CSVUtils.GetColumnLength(text);
            using var _ = ListPool<T>.Get(out var list);
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<string, FieldInfo> fieldInfos = fields.ToDictionary(x => x.Name);

            for (int col = 0; col < columnLength; col++)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("Deserializing CSV...{0}/{1}", col + 1, columnLength),
                        string.Format("Deserializing column {0}...", col),
                        (float)(col + 1) / columnLength))
                {
                    break;
                }

                string key = (string)CSVUtils.Parse(typeof(string), text, col, "key");
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                T data = Editor.DatabaseEditorUtils.LoadData<T>(key);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<T>();
                }

                for (int j = 0; j < fieldNames.Length; j++)
                {
                    string fieldName = fieldNames[j];

                    if (!fieldInfos.TryGetValue(fieldName, out var fieldInfo))
                    {
                        continue;
                    }

                    Type valueType = fieldInfo.FieldType;

                    if (!valueType.IsArray)
                    {
                        if (valueType.IsSubclassOf(typeof(SerializedCSV)))
                        {
                            object instance = Activator.CreateInstance(fieldInfo.FieldType);

                            string value = (string)CSVUtils.Parse(typeof(string), text, col, fieldName);
                            if (value.Contains(CSVUtils.SEPARATOR))
                            {
                                value = value.Trim('"');
                            }

                            MethodInfo methodInfo = valueType.GetMethod(nameof(SerializedCSV.FromCSV));
                            methodInfo.Invoke(instance, new object[] { value });

                            fieldInfo.SetValue(data, instance);
                        }
                        else
                        {
                            fieldInfo.SetValue(data, CSVUtils.Parse(valueType, text, col, fieldName));
                        }
                    }
                    else
                    {
                        Type elementType = valueType.GetElementType();

                        if (elementType.IsSubclassOf(typeof(SerializedCSV)))
                        {
                            Array keys = (Array)CSVUtils.ParseArray(typeof(string), text, col, fieldName);
                            if (keys != null)
                            {
                                Array array = Array.CreateInstance(elementType, keys.Length);
                                for (int k = 0; k < keys.Length; k++)
                                {
                                    object instance = Activator.CreateInstance(elementType);
                                    MethodInfo methodInfo = elementType.GetMethod(nameof(SerializedCSV.FromCSV));
                                    methodInfo.Invoke(instance, new object[] { keys.GetValue(k) });

                                    array.SetValue(instance, k);
                                }

                                fieldInfo.SetValue(data, array);
                            }
                        }
                        else
                        {
                            fieldInfo.SetValue(data, CSVUtils.ParseArray(elementType, text, col, fieldName));
                        }
                    }
                }

                list.Add(data);
            }

            UnityEditor.EditorUtility.ClearProgressBar();

            stopwatch.Stop();
            //JSLDebug.LogFormatTag(TAG, LOG_DESERIALIZE_FINISHED, typeof(T).Name, stopwatch.ElapsedMilliseconds * 0.001f);

            return list.ToArray();
        }

        public static void ProcessField<T>(T data, FieldInfo fieldInfo, string rawValue) where T : Data
        {
            if (rawValue.Contains(CSVUtils.SEPARATOR))
                rawValue = rawValue.Trim('"');

            if (string.IsNullOrEmpty(rawValue))
                return;

            Type fieldType = fieldInfo.FieldType;
            if (fieldType.IsArray)
            {
                string[] elementRawValues = rawValue.Split(CSVUtils.ARRAY_SEPARATOR);
                if (elementRawValues.Length == 1 && string.IsNullOrEmpty(elementRawValues[0]))
                    return;
                Type elementType = fieldType.GetElementType();
                var array = fieldInfo.GetValue(data) as Array ?? Array.CreateInstance(elementType, elementRawValues.Length);
                if (array.Length < elementRawValues.Length)
                {
                    var newArray = Array.CreateInstance(elementType, elementRawValues.Length);
                    Array.Copy(array, newArray, array.Length);
                    array = newArray;
                }

                object element;
                for (int i = 0; i < elementRawValues.Length; i++)
                {
                    if (string.IsNullOrEmpty(elementRawValues[i]))
                        continue;
                    element = Deserialize(elementType, elementRawValues[i], () => array.GetValue(i));
                    array.SetValue(element, i);
                }
                fieldInfo.SetValue(data, array);
            }
            else
            {
                object value = Deserialize(fieldType, rawValue, () => fieldInfo.GetValue(data));
                fieldInfo.SetValue(data, value);
            }
        }

        private static object Deserialize(Type memberType, string rawValue, Func<object> instanceGetter = null)
        {
            object instance;
            if (memberType.IsSubclassOf(typeof(SerializedCSV)))
            {
                instance = instanceGetter?.Invoke() ?? Activator.CreateInstance(memberType);
                MethodInfo methodInfo = memberType.GetMethod(nameof(SerializedCSV.FromCSV));
                methodInfo.Invoke(instance, new object[] { rawValue });
            }
            else
            {
                instance = CSVUtils.Parse(memberType, rawValue);
            }
            return instance;
        }

        private static void GetFieldInfoList<T>(List<FieldInfo> list) where T : Data
        {
            list.AddRange(typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            FieldInfo fieldInfo;
            for (int i = list.Count - 1; i > -1; i--)
            {
                fieldInfo = list[i];
                if (fieldInfo == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                Type fieldType = fieldInfo.FieldType;
                if (fieldType == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (!fieldInfo.IsPublic && fieldInfo.GetCustomAttribute<SerializeField>() == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (fieldInfo.IsPublic && fieldInfo.IsDefined(typeof(NonSerializedAttribute), false))
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (!fieldType.IsEnum &&
                    !fieldType.ToString().Contains("System") &&
                    fieldType != typeof(Vector2) &&
                    fieldType != typeof(Vector3) &&
                    fieldType != typeof(Vector4) &&
                    fieldType != typeof(Color) &&
                    !fieldType.IsSubclassOf(typeof(SerializedCSV)) &&
                    (fieldType.GetElementType() == null ||
                    !fieldType.GetElementType().IsSubclassOf(typeof(SerializedCSV))))
                {
                    //JSLDebug.LogError(fieldType.FullName);
                    list.RemoveAt(i);
                    continue;
                }
            }

            int index;
            for (int i = 0; i < DEFAULT_FIELD_NAMES.Length; i++)
            {
                index = list.FindIndex(x => x.Name == DEFAULT_FIELD_NAMES[i]);
                if (index != -1 && index != i)
                {
                    var item = list[index];
                    list.RemoveAt(index);
                    list.Insert(i, item);
                }
            }
        }

        private static string GetSerializedFieldOutput(object fieldValue, Type fieldType)
        {
            string output = string.Empty;

            if (!fieldType.IsArray)
            {
                if (fieldType.IsSubclassOf(typeof(SerializedCSV)))
                {
                    MethodInfo methodInfo = fieldType.GetMethod(nameof(SerializedCSV.ToCSV));
                    output += methodInfo.Invoke(fieldValue, null).ToString();
                }
                else if (fieldType == typeof(Color) && fieldValue is Color c)
                {
                    output = ColorUtility.ToHtmlStringRGBA(c);
                }
                else
                {
                    output += fieldValue.ToString();
                }
            }
            else
            {
                Array array = fieldValue as Array;
                Type elementType = fieldType.GetElementType();

                foreach (var element in array)
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        output += CSVUtils.ARRAY_SEPARATOR;
                    }

                    if (elementType.IsSubclassOf(typeof(SerializedCSV)))
                    {
                        MethodInfo methodInfo = elementType.GetMethod(nameof(SerializedCSV.ToCSV));
                        output += methodInfo.Invoke(element, null).ToString();
                    }
                    else if (elementType == typeof(Color) && element is Color c)
                    {
                        output += ColorUtility.ToHtmlStringRGBA(c);
                    }
                    else
                    {
                        output += element.ToString();
                    }
                }
            }

            if (output.Contains(CSVUtils.SEPARATOR))
            {
                output = string.Format("\"{0}\"", output);
            }

            return output;
        }
    }
}
#endif