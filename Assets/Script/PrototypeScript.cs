using GameCore.Database;
using GameCore.Log;
using Sirenix.OdinInspector;
using UnityEngine;

public class PrototypeScript : MonoBehaviour
{
    [SerializeField] private RoleReference roleReference;
    [Button()]
    private void PrototypeButton()
    {
        eLog.Log($"{roleReference.Load().key}");
    }
}
