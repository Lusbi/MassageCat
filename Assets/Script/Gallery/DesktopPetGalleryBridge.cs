using System;
using UnityEngine;

namespace MassageCat.Gallery
{
    public sealed class DesktopPetGalleryBridge : MonoBehaviour
    {
        internal static DesktopPetGalleryBridge Instance { get; private set; }

        private GalleryPipeEndpoint m_endpoint;
        private BattlePanel m_battlePanel;
        private bool m_snapshotDirty;

        internal bool RequestGalleryFocus()
        {
            if (m_endpoint == null || m_endpoint.IsConnected == false)
                return false;

            m_endpoint.Send(GalleryProtocol.FocusGallery, new GalleryHelloMessage { processRole = "desktop_pet" });
            return true;
        }

        private void Awake()
        {
            Instance = this;
            m_battlePanel = FindFirstObjectByType<BattlePanel>();
            m_endpoint = new GalleryPipeEndpoint(true);
            GalleryStateChangeNotifier.StateChanged += MarkSnapshotDirty;
            m_endpoint.Start();
        }

        private void Update()
        {
            while (m_endpoint.TryDequeueConnectionChange(out bool connected))
            {
                if (connected)
                {
                    m_endpoint.Send(
                        GalleryProtocol.Hello,
                        new GalleryHelloMessage { processRole = "desktop_pet" });
                    m_snapshotDirty = true;
                }
            }

            while (m_endpoint.TryDequeueMessage(out GalleryEnvelope envelope))
                HandleMessage(envelope);

            if (m_snapshotDirty && m_endpoint.IsConnected)
            {
                m_snapshotDirty = false;
                SendSnapshot();
            }
        }

        private void HandleMessage(GalleryEnvelope envelope)
        {
            if (envelope.type != GalleryProtocol.SelectRoleRequest
                || GalleryPipeEndpoint.TryReadPayload(envelope, out GallerySelectRoleRequest request) == false)
            {
                return;
            }

            m_battlePanel ??= FindFirstObjectByType<BattlePanel>();
            string reason;
            bool accepted;
            if (m_battlePanel == null)
            {
                reason = "not_ready";
                accepted = false;
            }
            else
            {
                accepted = m_battlePanel.TrySelectRoleFromGallery(request.roleKey, out reason);
            }

            m_endpoint.Send(
                GalleryProtocol.SelectRoleResult,
                new GallerySelectRoleResult
                {
                    requestId = request.requestId,
                    accepted = accepted,
                    reason = accepted ? string.Empty : reason,
                });

            if (accepted)
                m_snapshotDirty = true;
        }

        private void SendSnapshot()
        {
            m_battlePanel ??= FindFirstObjectByType<BattlePanel>();
            BattleModel battleModel = m_battlePanel != null
                ? m_battlePanel.CurrentBattleModel
                : BattleModel.Normal;
            m_endpoint.Send(GalleryProtocol.StateSnapshot, GalleryStateUtility.Capture(battleModel));
        }

        private void MarkSnapshotDirty()
        {
            m_snapshotDirty = true;
        }

        private void OnDestroy()
        {
            GalleryStateChangeNotifier.StateChanged -= MarkSnapshotDirty;
            m_endpoint?.Dispose();
            if (Instance == this)
                Instance = null;
        }
    }
}
