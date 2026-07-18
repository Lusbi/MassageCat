#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameCore.Database
{
    public class DatabaseFindDependencyUseCase
    {
        public Data[] FindDependency<T,U>(string key) where T:Data where U:DataReference<T>
        {
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(Repository).Name));
            List<Data> dataList = new List<Data>();
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Repository datas = AssetDatabase.LoadAssetAtPath<Repository>(assetPath);
                dataList.AddRange(datas.GetBaseDataArray());
            }

            List<Data> dependencies = new List<Data>();

            for (int i = 0; i < dataList.Count; i++)
            {
                Data baseData = dataList[i];
                FieldInfo[] fieldInfos = baseData.GetType().GetFields();

                for (int j = 0; j < fieldInfos.Length; j++)
                {
                    Type fieldType = fieldInfos[j].FieldType;

                    if (!fieldType.IsArray)
                    {
                        if (fieldType == typeof(U))
                        {
                            object fieldValue = fieldInfos[j].GetValue(baseData);
                            MethodInfo methodInfo = fieldType.GetMethod("GetKey");

                            if ((string)methodInfo.Invoke(fieldValue, null) == key)
                            {
                                dependencies.Add(baseData);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        Type elementType = fieldType.GetElementType();

                        if (elementType == typeof(U))
                        {
                            object fieldValue = fieldInfos[j].GetValue(baseData);
                            Array array = fieldValue as Array;

                            foreach (var element in array)
                            {
                                MethodInfo methodInfo = elementType.GetMethod("GetKey");

                                if ((string)methodInfo.Invoke(element, null) == key)
                                {
                                    dependencies.Add(baseData);
                                    continue;
                                }
                            }
                        }
                    }
                }
            }

            return dependencies.ToArray();
        }
    }
}
#endif