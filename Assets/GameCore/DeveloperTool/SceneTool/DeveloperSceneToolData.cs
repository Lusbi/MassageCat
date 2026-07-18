#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SceneToolData", menuName = "Developer/SceneToolData")]
public class DeveloperSceneToolData : ScriptableObject
{
    [TableList]
    public List<DeveloperSceneInfo> developerSceneInfos = new List<DeveloperSceneInfo>();
}
#endif