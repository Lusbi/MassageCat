using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace GameCore.Database.Editor
{
    public static class EditorDatabaseHelper
    {
        public const string REPOSITORY_PATH =  "Assets/AssetResources/Database/AssetBundleResources";

        public static IEnumerable<string> EnumerateDataClassBriefNames() => TypeCache.GetTypesDerivedFrom<Data>()
                                                                                     .Select(x => x.Name)
                                                                                     .Select(x => x[..x.LastIndexOf(nameof(Data))])
                                                                                     .OrderBy(x => x);

        public static IEnumerable<string> EnumerateDataClassNameOfAssets()
        {
            if (!Directory.Exists(REPOSITORY_PATH))
                return Enumerable.Empty<string>();

            return Directory.EnumerateFiles(REPOSITORY_PATH, "*.asset", SearchOption.AllDirectories)
                            .Select(x => Path.GetFileName(x))
                            .Select(x => x[..x.LastIndexOf(nameof(Repository))] + nameof(Data))
                            .Distinct();
        }

        public static IEnumerable<string> EnumerateUserNames()
        {
            if (!Directory.Exists(REPOSITORY_PATH))
                return Enumerable.Empty<string>();

            return Directory.EnumerateDirectories(REPOSITORY_PATH).Select(x => Path.GetFileName(x)).Distinct();
        }
    }
}