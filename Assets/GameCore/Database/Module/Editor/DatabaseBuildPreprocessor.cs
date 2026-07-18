using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GameCore.Database.Editor
{
    public class DatabaseBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            DatabaseBuilder.BuildManagerScriptAsync().Wait(1000);
        }
    }
}