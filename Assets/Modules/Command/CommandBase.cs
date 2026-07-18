using GameCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandBase : MonoBehaviour , ICommand
{
    public virtual CommandType CommandType => CommandType.Battle;
    public virtual void Execute()
    {
        Debug.Log("Executing Base Command");
        // 在這裡添加 Base 指令的具體邏輯
    }


    [SerializeField] private Image m_icon;
    [SerializeField] private TextMeshProUGUI m_text;
    [SerializeField] private UIButton m_btn;

    private GameMainView m_gameMainView;
    private void Reset()
    {
        if (m_icon == null)
            m_icon = GetComponentInChildren<Image>();
        if (m_text == null)
            m_text = GetComponentInChildren<TextMeshProUGUI>();
        if (m_btn == null)
            m_btn = GetComponentInChildren<UIButton>();

        if (m_text != null)
        {
            m_text.text = CommandType.ToString();
        }
        this.gameObject.name = $"Command [{CommandType.ToString()}]";
    }

    private void Awake()
    {
        if (m_btn != null)
        {
            m_btn.onClicked = OnClick;
        }
    }

    public void Apply(GameMainView gameMainView)
    {
        m_gameMainView = gameMainView;
    }

    private void OnClick(UIButton button)
    {
        m_gameMainView.ActionCommand(CommandType);
    }
}
