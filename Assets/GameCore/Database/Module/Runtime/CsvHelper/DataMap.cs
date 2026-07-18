using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using MemberTypes = CsvHelper.Configuration.MemberTypes;

namespace GameCore.Database.Editor
{
    /// <summary>解決預設ClassMap會解析ScriptableObject等引擎內建類別之成員的問題</summary>
    public class DataMap<T> : ClassMap<T> where T : Data
    {
        private static readonly List<Type> enumerableConverters = new()
        {
            typeof(ArrayConverter),
            typeof(CollectionGenericConverter),
            typeof(EnumerableConverter),
            typeof(IDictionaryConverter),
            typeof(IDictionaryGenericConverter),
            typeof(IEnumerableConverter),
            typeof(IEnumerableGenericConverter)
        };

        public DataMap(CsvContext context)
        {
            AutoMap(context);
        }

        protected override void AutoMapMembers(ClassMap map, CsvContext context, LinkedList<Type> mapParents, int indexStart = 0)
        {
            Type genericType = GetGenericType(map);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (context.Configuration.IncludePrivateMembers)
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            List<MemberInfo> list = new();
            if ((context.Configuration.MemberTypes & MemberTypes.Properties) == MemberTypes.Properties)
            {
                List<PropertyInfo> list2 = new();
                foreach (PropertyInfo property in GetUniqueProperties(genericType, bindingFlags))
                {
                    if (!list2.Any((PropertyInfo p) => p.Name == property.Name))
                    {
                        list2.Add(GetDeclaringProperty(genericType, property, bindingFlags));
                    }
                }

                list.AddRange(list2);
            }

            if ((context.Configuration.MemberTypes & MemberTypes.Fields) == MemberTypes.Fields)
            {
                List<MemberInfo> list3 = new();
                foreach (FieldInfo field in GetUniqueFields(genericType, bindingFlags))
                {
                    if (!list3.Any((MemberInfo p) => p.Name == field.Name) && !field.GetCustomAttributes(typeof(CompilerGeneratedAttribute), inherit: false).Any())
                    {
                        list3.Add(GetDeclaringField(genericType, field, bindingFlags));
                    }
                }

                list.AddRange(list3);
            }

            foreach (MemberInfo item in list)
            {
                if (item.GetCustomAttribute<IgnoreAttribute>() != null)
                {
                    continue;
                }

                Type type = context.TypeConverterCache.GetConverter(item).GetType();
                if (context.Configuration.HasHeaderRecord && enumerableConverters.Contains(type))
                {
                    continue;
                }

                TypeInfo typeInfo = item.MemberType().GetTypeInfo();
                if (type == typeof(DefaultTypeConverter))
                {
                    if (context.Configuration.IgnoreReferences || CheckForCircularReference(item.MemberType(), mapParents))
                    {
                        continue;
                    }

                    mapParents.AddLast(genericType);
                    Type type2 = typeof(DefaultClassMap<>).MakeGenericType(item.MemberType());
                    ClassMap classMap = (ClassMap)ObjectResolver.Current.Resolve(type2);
                    if (typeInfo.HasConstructor() && !typeInfo.HasParameterlessConstructor() && !typeInfo.IsUserDefinedStruct())
                    {
                        AutoMapConstructorParameters(classMap, context, mapParents, Math.Max(map.GetMaxIndex() + 1, indexStart));
                    }

                    AutoMapMembers(classMap, context, mapParents, Math.Max(map.GetMaxIndex() + 1, indexStart));
                    while (mapParents.Count > 0)
                    {
                        var last = mapParents.Last;
                        mapParents.RemoveLast();
                        if (last.Value == genericType)
                        {
                            break;
                        }
                    }
                    if (classMap.MemberMaps.Count > 0 || classMap.ReferenceMaps.Count > 0)
                    {
                        MemberReferenceMap memberReferenceMap = new(item, classMap);
                        if (context.Configuration.ReferenceHeaderPrefix != null)
                        {
                            ReferenceHeaderPrefixArgs args = new(item.MemberType(), item.Name);
                            memberReferenceMap.Data.Prefix = context.Configuration.ReferenceHeaderPrefix(args);
                        }

                        ApplyAttributes(memberReferenceMap);
                        map.ReferenceMaps.Add(memberReferenceMap);
                    }
                }
                else
                {
                    MemberMap memberMap = MemberMap.CreateGeneric(mapParents.First?.Value ?? map.ClassType, item);
                    memberMap.Data.TypeConverterOptions = TypeConverterOptions.Merge(new TypeConverterOptions(), context.TypeConverterOptionsCache.GetOptions(item.MemberType()), memberMap.Data.TypeConverterOptions);
                    memberMap.Data.Index = map.GetMaxIndex() + 1;
                    ApplyAttributes(memberMap);
                    map.MemberMaps.Add(memberMap);
                }
            }

            map.ReIndex(indexStart);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<PropertyInfo> GetUniqueProperties(Type type, BindingFlags flags)
        {
            flags |= BindingFlags.DeclaredOnly;
            HashSet<PropertyInfo> set = new();
            while (type != null && typeof(Data).IsAssignableFrom(type))
            {
                foreach (PropertyInfo propertyInfo in type.GetProperties(flags))
                {
                    if (set.Add(propertyInfo))
                        yield return propertyInfo;
                }
                type = type.BaseType;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<FieldInfo> GetUniqueFields(Type type, BindingFlags flags)
        {
            flags |= BindingFlags.DeclaredOnly;
            HashSet<FieldInfo> set = new();
            while (type != null && typeof(Data).IsAssignableFrom(type))
            {
                foreach (FieldInfo fieldInfo in type.GetFields(flags))
                {
                    if (set.Add(fieldInfo))
                    {
                        if (fieldInfo.IsPublic && fieldInfo.IsDefined(typeof(NonSerializedAttribute)))
                            continue;
                        if (!fieldInfo.IsPublic && !fieldInfo.IsDefined(typeof(SerializeField)))
                            continue;
                        yield return fieldInfo;
                    }
                }
                type = type.BaseType;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PropertyInfo GetDeclaringProperty(Type type, PropertyInfo property, BindingFlags flags)
        {
            if (property.DeclaringType != type)
            {
                PropertyInfo property2 = property.DeclaringType.GetProperty(property.Name, flags);
                return GetDeclaringProperty(property.DeclaringType, property2, flags);
            }

            return property;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FieldInfo GetDeclaringField(Type type, FieldInfo field, BindingFlags flags)
        {
            if (field.DeclaringType != type)
            {
                FieldInfo field2 = field.DeclaringType.GetField(field.Name, flags);
                return GetDeclaringField(field.DeclaringType, field2, flags);
            }

            return field;
        }

        private static Type GetGenericType(ClassMap classMap)
        {
            Type baseType = classMap.GetType().GetTypeInfo().BaseType;
            return (baseType?.GetGenericArguments()[0]) ?? throw new ConfigurationException();
        }
    }
}