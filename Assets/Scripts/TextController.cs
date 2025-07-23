using UnityEngine;
using TMPro; // Make sure you have TextMeshPro imported
using System; // For Enum.Parse

/// <summary>
/// This component allows a RectTransform and TextMeshProUGUI component
/// to be configured from a JSON object sent from JavaScript.
/// Attach this script to a UI GameObject that has both a RectTransform and a TextMeshProUGUI component.
/// </summary>
public class JsonConfigurator : MonoBehaviour
{
    // --- Data Structures to Match JSON ---
    // These classes are designed to be deserialized from JSON.
    // They must be marked [System.Serializable] for Unity's JsonUtility to work.

    [System.Serializable]
    private class RectSettings
    {
        // We use nullable types (e.g., Vector3?) to detect if a key was present in the JSON.
        // However, Unity's JsonUtility does not support nullable types directly.
        // A common workaround is to include a boolean flag for each property.
        // For simplicity here, we'll assume if a section (like 'rect') is present,
        // you want to apply all its values. If a value is omitted, it will get the default
        // for its type (e.g., 0 for numbers).

        public Vector3 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 pivot = new Vector2(-1, -1); // Use an impossible value to check if it was set
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.zero; // Use Vector3.zero to check if it was set
    }

    [System.Serializable]
    private class TextSettings
    {
        public string content;
        public float fontSize;
        public Color color = Color.clear; // Use an impossible value to check if it was set
        public float characterSpacing;
        public float wordSpacing;
        public float lineSpacing;
        public string alignment; // "MiddleCenter", "TopLeft", etc.
        public bool enableWordWrapping;
        public string overflowMode; // "Overflow", "Truncate", etc.
    }

    [System.Serializable]
    private class ElementConfig
    {
        // These will be null if the corresponding JSON object ("rect" or "text") is not present.
        public RectSettings rect;
        public TextSettings text;
    }

    // --- Private References ---
    private RectTransform rectTransform;
    private TextMeshProUGUI textMeshPro;

    void Awake()
    {
        // Cache the component references for performance
        rectTransform = GetComponent<RectTransform>();
        textMeshPro = GetComponent<TextMeshProUGUI>();

        if (rectTransform == null)
        {
            Debug.LogError("JsonConfigurator: No RectTransform component found on this GameObject.");
        }
        if (textMeshPro == null)
        {
            Debug.LogError("JsonConfigurator: No TextMeshProUGUI component found on this GameObject.");
        }
    }

    /// <summary>
    /// PUBLIC METHOD: This is called from JavaScript using SendMessage.
    /// It accepts a JSON string, parses it, and applies the settings.
    /// </summary>
    /// <param name="jsonString">The JSON data from JavaScript.</param>
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
            ElementConfig config = JsonUtility.FromJson<ElementConfig>(jsonString);

            // Apply RectTransform settings if they exist in the JSON
            if (config.rect != null)
            {
                ApplyRectSettings(config.rect);
            }

            // Apply TextMeshPro settings if they exist in the JSON
            if (config.text != null)
            {
                ApplyTextSettings(config.text);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON or applying settings: {e.Message}");
        }
    }

    private void ApplyRectSettings(RectSettings settings)
    {
        if (rectTransform == null) return;

        // We apply each setting individually.
        // A more robust system for optional keys might check against a sentinel value.
        rectTransform.anchoredPosition3D = settings.anchoredPosition;
        rectTransform.sizeDelta = settings.sizeDelta;

        // For pivot, we check against the "impossible" default value we set.
        if (settings.pivot.x != -1)
        {
            rectTransform.pivot = settings.pivot;
        }

        rectTransform.localEulerAngles = settings.localEulerAngles;

        // For scale, we check against Vector3.zero.
        if (settings.localScale != Vector3.zero)
        {
            rectTransform.localScale = settings.localScale;
        }
        Debug.Log("Successfully applied RectTransform settings.");
    }

    private void ApplyTextSettings(TextSettings settings)
    {
        if (textMeshPro == null) return;

        // Apply text content
        if (!string.IsNullOrEmpty(settings.content))
        {
            textMeshPro.text = settings.content;
        }

        // Apply font size (only if it's a positive number)
        if (settings.fontSize > 0)
        {
            textMeshPro.fontSize = settings.fontSize;
        }

        // Apply color (only if it's not the 'clear' sentinel value)
        if (settings.color != Color.clear)
        {
            textMeshPro.color = settings.color;
        }

        // Apply spacing
        textMeshPro.characterSpacing = settings.characterSpacing;
        textMeshPro.wordSpacing = settings.wordSpacing;
        textMeshPro.lineSpacing = settings.lineSpacing;

        // Apply alignment by parsing the string to the enum type
        if (!string.IsNullOrEmpty(settings.alignment))
        {
            try
            {
                textMeshPro.alignment = (TextAlignmentOptions)Enum.Parse(typeof(TextAlignmentOptions), settings.alignment);
            }
            catch { Debug.LogWarning($"Invalid TextAlignmentOptions value: {settings.alignment}"); }
        }

        // Apply wrapping
        textMeshPro.enableWordWrapping = settings.enableWordWrapping;

        // Apply overflow mode by parsing the string to the enum type
        if (!string.IsNullOrEmpty(settings.overflowMode))
        {
            try
            {
                textMeshPro.overflowMode = (TextOverflowModes)Enum.Parse(typeof(TextOverflowModes), settings.overflowMode);
            }
            catch { Debug.LogWarning($"Invalid TextOverflowModes value: {settings.overflowMode}"); }
        }
        Debug.Log("Successfully applied TextMeshPro settings.");
    }
}
