using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class ColorGammaToLinnerUseCase
{
    [SerializeField, OnValueChanged(nameof(ChangeToLinner))]
    private string colorHex;

    [SerializeField, ColorPalette, LabelText("Gamma"), OnValueChanged(nameof(ChangeToLinner), true)]
    private Color m_gammaColor;

    [SerializeField, ColorPalette, LabelText("Linear"), OnValueChanged(nameof(ChangeToLinner), true)]
    private Color m_linearColor;

    public Color gammaColor => m_gammaColor;
    public Color linearColor => m_linearColor;

    private void ChangeToLinner()
    {
        if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            m_gammaColor = color;
            m_linearColor = color.linear;
        }
    }
}
