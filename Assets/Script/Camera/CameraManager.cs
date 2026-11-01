using UnityEngine;
using GameCore;
using Cinemachine;

public class CameraManager : MonoSingleton<CameraManager>
{
    private CinemachineVirtualCamera m_cinemachineVirtualCamera;

    public CameraManager()
    {
        m_cinemachineVirtualCamera = GameObject.FindFirstObjectByType<CinemachineVirtualCamera>();
    }

    public void SetLookAt(Transform targetTransform)
    {
        m_cinemachineVirtualCamera.LookAt = targetTransform;
    }

    public void SetFollow(Transform targetTransform)
    {
        m_cinemachineVirtualCamera.Follow = targetTransform;
    }
}
