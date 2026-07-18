using UnityEngine;

namespace GameCore
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T m_instance;
        public static T instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = GameObject.FindAnyObjectByType<T>();

                    if (m_instance == null)
                        m_instance = new GameObject(typeof(T).ToString()).AddComponent<T>();
                }

                return m_instance;
            }
        }
    }
}