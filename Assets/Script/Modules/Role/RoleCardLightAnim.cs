using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class RoleCardLightAnim : MonoBehaviour
{
    private Image m_image;
    private Material m_glowMaterial;

    // Color Lerp Variables
    private Color m_targetColor;
    private Color m_currentColor;
    private float m_lerpTime;
    private float m_currentLerpDuration;

    private const float k_minLerpDuration = 0.5f;
    private const float k_maxLerpDuration = 1.5f;
    private int k_colorPropertyId = Shader.PropertyToID("_ShadowLight_Color_1");

    private bool m_shaderValid;

    public void Apply(Image image)
    {
        m_image = image;
        if (image != null)
        {
            m_glowMaterial = m_image.material;
            ValidateShader();
        }
    }

    private void OnEnable()
    {
        if (m_image != null && m_glowMaterial == null)
        {
            m_glowMaterial = m_image.material;
        }

        ValidateShader();

        // Initialize color
        m_currentColor = Color.white;
        PickNewTargetColor();
    }

    private void PickNewTargetColor()
    {
        // Random vibrant color using HSV
        m_targetColor = Color.HSVToRGB(Random.value, 0.8f, 1.0f);
        m_lerpTime = 0f;
        m_currentLerpDuration = Random.Range(k_minLerpDuration, k_maxLerpDuration);
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_shaderValid || m_image == null || m_glowMaterial == null)
            return;

        // Update Color Lerp
        m_lerpTime += Time.deltaTime;
        float t = m_lerpTime / m_currentLerpDuration;

        Color newColor = Color.Lerp(m_currentColor, m_targetColor, t);

        if (t >= 1.0f)
        {
            m_currentColor = m_targetColor;
            PickNewTargetColor();
            newColor = m_currentColor;
        }

        // 立即更新該材質球的資料
        OnUpdateLight(newColor);

    }

    private void OnDisable() => ReleaseMaterial();
    private void OnDestroy() => ReleaseMaterial();

    private void ReleaseMaterial()
    {
        if (m_glowMaterial != null)
        {
            Destroy(m_glowMaterial);
            m_glowMaterial = null;
        }

        if (m_image != null)
        {
            m_image.material = null;
        }

        m_shaderValid = false;
    }

    [Button("Update Light")]
    private void OnUpdateLight(Color color)
    {
        if (m_image == null)
            return;

        
        if (m_glowMaterial.HasProperty(k_colorPropertyId))
        {
            m_glowMaterial.SetColor(k_colorPropertyId, color);
        }
        m_image.canvasRenderer.SetMaterial(m_glowMaterial, m_image.mainTexture);
        ValidateShader();
    }

    private void ValidateShader()
    {
        m_shaderValid = m_glowMaterial != null && m_glowMaterial.HasProperty(k_colorPropertyId);
    }
}
