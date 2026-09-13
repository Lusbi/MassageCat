namespace GameCore.Database.Editor
{
    public static class TextConst
    {
        public const string CANCEL = "取消";
        public const string DELETE_DECISION_KEY = "GameCore.Database.DeleteDialog";
        public const string DELETE_TITLE = "刪除所選資料?";
        public const string DELETE_MSG_FORMAT = "你即將刪除{0}的資料 {1}.";
        public const string DELETE_OK = "刪除";
        public const string DEPENDENCY_RM_TITLE = "Dependencies Removal";
        public const string DEPENDENCY_RM_MESSAGE = "Are you sure you want to remove all dependencies for this Data?";
        public const string ERRLOG_TITLE = "操作失敗";
        public const string ERRLOG_MSG_FORMAT = "此資料含有錯誤訊息 (error logs)!\n\nError Messages:\n{0}";
        public const string DATA_EXIST_TITLE = "資料已存在";
        public const string DATA_EXIST_MSG_FORMAT = "{0}的庫{1}已存在此資料，是否更新它?\n資料Key: {2} - {3}";
        public const string DATA_EXIST_OK = "更新";
        public const string OK = "OK";
        public const string SAVE_CHANGES_MESSAGE = "離開前是否儲存編輯中的資料?";
        public const string WARNLOG_TITLE = "警告!";
        public const string WARNLOG_MSG_FORMAT = "此資料含有警告訊息 (warning logs)!\n是否繼續?\n\nWarning Messages:\n{0}";
        public const string WARNLOG_OK = "繼續";
    }
}