using System;
using UnityEngine;

namespace GameCore.UI
{
    public class UIUtils
    {
        public static readonly Vector2 V_CAMERA_SIZE = new Vector2(1080, 1920);
        public static readonly Vector2 H_CAMERA_SIZE_4K = new Vector2(2560, 1440);
        public static readonly Vector2 H_CAMERA_SIZE_2K = new Vector2(1920, 1680);
        public const float DESIGNER_WIDTH = 640;
        public const float DESIGNER_HEIGHT = 480f;
        public static readonly Vector2 DESIGNER_SIZE = new Vector2(DESIGNER_WIDTH, DESIGNER_HEIGHT);

        public static long GetStateID(params Enum[] enums)
        {
            long stateId = 0;

            foreach (Enum e in enums)
            {
                stateId |= GetEnumStateId(e);
            }
            return stateId;
        }

        private static long GetEnumStateId(Enum value)
        {
            return 1 << Convert.ToInt32(value);
        }

        public static long GetAllStateId<T>() where T : struct, IConvertible
        {
            T[] enumValues = (T[])Enum.GetValues(typeof(T));
            long stateId = 0;

            foreach (T t in enumValues)
            {
                stateId |= GetEnumStateId(t);
            }

            return stateId;
        }
        private static long GetEnumStateId<T>(T value) where T : struct, IConvertible
        {
            return 1 << Convert.ToInt32(value);
        }
    }
}