using GameCore.UI;
using MassageCat.Gallery;
using UnityEngine;

public class GameProcess : MonoBehaviour
{
    public static GameProcess Instance { get; private set; }
    public bool ForceNewGame = false;
    [SerializeField] private bool m_desktopPetMode = true;

    public bool DesktopPetMode => m_desktopPetMode;

    private void OnApplicationQuit()
    {
        StorageManager.instance.SaveStorageData();
    }

    private void Awake()
    {
        DontDestroyOnLoad(this);
        Instance = this;

        if (TryGetComponent(out GameInputGate _) == false)
            gameObject.AddComponent<GameInputGate>();

        if (m_desktopPetMode && TryGetComponent(out DesktopPetWindowController _) == false)
        {
            gameObject.AddComponent<DesktopPetWindowController>();
        }
    }

    private void Start()
    {
        GameCore.Database.DatabaseManager.instance.Initialize(
            () => LocalizationManager.instance.Initlization(
                () => Setup()));
    }

    private void Setup()
    {
        TomatoManager.instance.Initlization();
        SettingsManager.instance.Initlization(
            () =>
            {
                if (ForceNewGame == false)
                    StorageManager.instance.LoadStorageData();
                else
                    StorageManager.instance.SetStorageData(new StorageData());

                UIManager.instance.Initlization(ChangeToInitialView);
            }
        );
    }

    private void ChangeToInitialView()
    {
        UIManager.instance.ChangeView(UIEnum.GameMain);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(m_desktopPetMode ? "[Codex驗證] 已切換至桌寵主畫面，請檢查角色、設定入口與場景切換圖示。" : "[Codex驗證] 已切換至一般主畫面，請檢查場景背景與紙繪 UI。", this);
#endif

        if (m_desktopPetMode && TryGetComponent(out DesktopPetWindowController desktopPetWindowController))
        {
            desktopPetWindowController.EnsureRuntimeControls();
            if (TryGetComponent(out GalleryBootstrap galleryBootstrap) == false)
                galleryBootstrap = gameObject.AddComponent<GalleryBootstrap>();
            galleryBootstrap.InitializeIntegrated(desktopPetWindowController);
        }
    }
}
