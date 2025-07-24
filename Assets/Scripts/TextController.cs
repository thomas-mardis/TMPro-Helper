using UnityEngine;
using TMPro; // Make sure you have TextMeshPro imported
using System; // For Enum.Parse

/// <summary>
/// This component allows a RectTransform and a TextMeshPro component (UI or 3D)
/// to be configured from a JSON object sent from JavaScript.
/// It correctly handles partial JSON data by only updating the specified properties.
/// </summary>
public class JsonConfigurator : MonoBehaviour
{
    // --- Data Structures to Match JSON ---
    [System.Serializable]
    private class RectSettings
    {
        public Vector3 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localPosition;
        public Vector2 pivot;
        public Vector3 localEulerAngles;
        public Vector3 localScale;
    }

    [System.Serializable]
    private class TextSettings
    {
        public string content;
        public float fontSize;
        public Color color;
        public float characterSpacing;
        public float wordSpacing;
        public float lineSpacing;
        public string alignment;
        public bool enableWordWrapping;
        public string overflowMode;
    }

    [System.Serializable]
    private class ElementConfig
    {
        public RectSettings rect;
        public TextSettings text;
    }

    // --- Private References ---
    private RectTransform rectTransform;
    private TMP_Text textMeshPro;

    void Awake()
    {
        textMeshPro = GetComponent<TMP_Text>();
        if (textMeshPro == null)
        {
            Debug.LogError("JsonConfigurator: No TextMeshPro or TextMeshProUGUI component found on this GameObject.");
        }
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// PUBLIC METHOD: Called from JavaScript. It now uses FromJsonOverwrite for robust partial updates.
    /// </summary>
    public void ApplyJsonConfig(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogWarning("ApplyJsonConfig received an empty or null string.");
            return;
        }

        Debug.Log($"Received JSON: {jsonString}");

        try
        {
            // 1. Create a config object that mirrors the component's CURRENT state.
            ElementConfig currentConfig = GetCurrentConfig();

            // 2. Overwrite the current state with any values from the incoming JSON.
            // Fields not present in the JSON will be left untouched in the currentConfig object.
            JsonUtility.FromJsonOverwrite(jsonString, currentConfig);

            // 3. Apply the final, merged configuration.
            if (currentConfig.rect != null)
            {
                ApplyTransformSettings(currentConfig.rect);
            }

            if (currentConfig.text != null && textMeshPro != null)
            {
                ApplyTextSettings(currentConfig.text);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON or applying settings: {e.Message}");
        }
    }

    /// <summary>
    /// Creates a new ElementConfig object populated with the component's current values.
    /// </summary>
    private ElementConfig GetCurrentConfig()
    {
        ElementConfig config = new ElementConfig();

        if (textMeshPro != null)
        {
            config.text = new TextSettings
            {
                content = textMeshPro.text,
                fontSize = textMeshPro.fontSize,
                color = textMeshPro.color,
                characterSpacing = textMeshPro.characterSpacing,
                wordSpacing = textMeshPro.wordSpacing,
                lineSpacing = textMeshPro.lineSpacing,
                alignment = textMeshPro.alignment.ToString(),
                enableWordWrapping = textMeshPro.enableWordWrapping,
                overflowMode = textMeshPro.overflowMode.ToString()
            };
        }

        config.rect = new RectSettings
        {
            localPosition = transform.localPosition,
            localEulerAngles = transform.localEulerAngles,
            localScale = transform.localScale
        };

        if (rectTransform != null)
        {
            config.rect.anchoredPosition = rectTransform.anchoredPosition3D;
            config.rect.sizeDelta = rectTransform.sizeDelta;
            config.rect.pivot = rectTransform.pivot;
        }

        return config;
    }

    /// <summary>
    /// Applies all settings from the RectSettings object to the transform.
    /// </summary>
    private void ApplyTransformSettings(RectSettings settings)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition3D = settings.anchoredPosition;
            rectTransform.sizeDelta = settings.sizeDelta;
            rectTransform.pivot = settings.pivot;
        }
        else
        {
            transform.localPosition = settings.localPosition;
        }
        transform.localEulerAngles = settings.localEulerAngles;
        transform.localScale = settings.localScale;
        Debug.Log("Successfully applied Transform settings.");
    }

    /// <summary>
    /// Applies all settings from the TextSettings object to the TMP component.
    /// </summary>
    private void ApplyTextSettings(TextSettings settings)
    {
        textMeshPro.text = settings.content;
        textMeshPro.fontSize = settings.fontSize;
        textMeshPro.color = settings.color;
        textMeshPro.characterSpacing = settings.characterSpacing;
        textMeshPro.wordSpacing = settings.wordSpacing;
        textMeshPro.lineSpacing = settings.lineSpacing;
        textMeshPro.enableWordWrapping = settings.enableWordWrapping;

        if (!string.IsNullOrEmpty(settings.alignment))
        {
            try { textMeshPro.alignment = (TextAlignmentOptions)Enum.Parse(typeof(TextAlignmentOptions), settings.alignment); }
            catch { Debug.LogWarning($"Invalid TextAlignmentOptions value: {settings.alignment}"); }
        }

        if (!string.IsNullOrEmpty(settings.overflowMode))
        {
            try { textMeshPro.overflowMode = (TextOverflowModes)Enum.Parse(typeof(TextOverflowModes), settings.overflowMode); }
            catch { Debug.LogWarning($"Invalid TextOverflowModes value: {settings.overflowMode}"); }
        }
        Debug.Log("Successfully applied TextMeshPro settings.");
    }
}
