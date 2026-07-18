using UnityEngine;

namespace GameCore
{
    public enum OperationType
    {
        /// <summary>
        /// ==
        /// </summary>
        Equality,
        /// <summary>
        /// !=
        /// </summary>
        Inequality,
        /// <summary>
        /// <
        /// </summary>
        LessThan,
        /// <summary>
        /// >
        /// </summary>
        GreaterThan,
        /// <summary>
        /// <=
        /// </summary>
        LessThanOrEqual,
        /// <summary>
        /// >=
        /// </summary>
        GreaterThanOrEqual,
    }

    public static class OperationTypeUtils
    {
        public static bool Valid(this OperationType operationType, int a, int b) => operationType switch
        {
            OperationType.Equality => a == b,
            OperationType.Inequality => a != b,
            OperationType.GreaterThan => a > b,
            OperationType.LessThan => a < b,
            OperationType.GreaterThanOrEqual => a >= b,
            OperationType.LessThanOrEqual => a <= b,
            _ => true,
        };

        public static bool Valid(this OperationType operationType, float a, float b) => operationType switch
        {
            OperationType.Equality => Mathf.Approximately( a, b),
            OperationType.Inequality => a != b,
            OperationType.GreaterThan => a > b,
            OperationType.LessThan => a < b,
            OperationType.GreaterThanOrEqual => a >= b,
            OperationType.LessThanOrEqual => a <= b,
            _ => true,
        };
    }
}