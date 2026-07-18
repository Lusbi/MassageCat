namespace GameCore.Resources.Utils
{
    public static class AddressableUtils
    {
        /// <summary>
        /// 回傳 Group Name
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static string GetGroupNameFromPath(string assetPath)
        {
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            return directory.Replace("Assets/", "").Replace("/", "_").Replace("\\", "-");
        }

        /// <summary>
        /// 回傳 物件名稱
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static string GetAddressFromPath(string assetPath)
        {
            int lastIndex = assetPath.LastIndexOf("/") + 1;
            string newGroupKey = assetPath.Substring(lastIndex, assetPath.Length - lastIndex);
            newGroupKey = System.IO.Path.ChangeExtension(newGroupKey, null);
            return newGroupKey;
        }
    }
}