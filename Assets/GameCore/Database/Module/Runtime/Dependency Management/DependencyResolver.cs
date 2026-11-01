using System;
using System.Diagnostics;

namespace GameCore.Database.Dependency
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DependencyResolverAttribute : System.Attribute
    {
        public Type ParameterType { get; }

        public DependencyResolverAttribute(Type parameterType)
        {
            ParameterType = parameterType;
        }
    }
}