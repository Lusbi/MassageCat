using System;

namespace MassageCat.Gallery
{
    internal static class GalleryProtocol
    {
        internal const int Version = 1;
        internal const string PipeName = "MassageCat.Gallery.V1";

        internal const string Hello = "hello";
        internal const string StateSnapshot = "state_snapshot";
        internal const string SelectRoleRequest = "select_role_request";
        internal const string SelectRoleResult = "select_role_result";
        internal const string FocusGallery = "focus_gallery";
    }

    [Serializable]
    internal sealed class GalleryEnvelope
    {
        public int protocolVersion = GalleryProtocol.Version;
        public string type;
        public string payload;
    }

    [Serializable]
    internal sealed class GalleryHelloMessage
    {
        public string processRole;
    }

    [Serializable]
    internal sealed class GalleryRoleState
    {
        public string roleKey;
        public bool unlocked;
        public bool cardReward;
        public bool tomatoClear;
    }

    [Serializable]
    internal sealed class GalleryStateSnapshot
    {
        public string currentRoleKey;
        public int battleModel;
        public GalleryRoleState[] roles;
    }

    [Serializable]
    internal sealed class GallerySelectRoleRequest
    {
        public int requestId;
        public string roleKey;
    }

    [Serializable]
    internal sealed class GallerySelectRoleResult
    {
        public int requestId;
        public bool accepted;
        public string reason;
    }
}
