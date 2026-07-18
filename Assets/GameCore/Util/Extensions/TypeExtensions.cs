using System;
using System.Collections.Generic;

namespace GameCore.SystemType.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Supports both open generic types and closed constructed generic types.
        /// False if <paramref name="derivedType"/> and <paramref name="baseType"/> are equal.
        /// </summary>
        public static bool IsDerivedFrom(this Type derivedType, Type baseType)
        {
            if (baseType.IsGenericTypeDefinition)
                return IsGenericDerived(derivedType, baseType);

            return derivedType.IsSubclassOf(baseType);
        }

        public static bool IsGenericList(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

        public static bool TryGetElementTypeOfArrayOrList(this Type type, out Type elementType)
        {
            if (type.IsArray)
                elementType = type.GetElementType();
            else if (type.IsGenericList())
                elementType = type.GetGenericArguments()[0];
            else
                elementType = null;
            return elementType != null;
        }

        private static bool IsGenericDerived(Type type, Type genericType)
        {
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                    return true;

                type = type.BaseType;
            }
            return false;
        }
    }
}