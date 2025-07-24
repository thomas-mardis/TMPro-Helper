// Global variable to hold the Unity instance
let unityInstance = null;
let quill = null;

// Configuration constants
const gameObjectName = "DisplayText";
const methodName = "ApplyJsonConfig";

// DOM element references
const tmpOutput = document.getElementById('tmp-output');
const copyButton = document.getElementById('copy-button');

// --- Quill Customization ---
// Define a custom whitelist of named font sizes for the toolbar.
// This list MUST match the values used in the toolbar and CSS.
const Size = Quill.import('attributors/class/size');
Size.whitelist = ['tiny', 'small', 'large', 'extra-large']; // 'normal' is the default
Quill.register(Size, true);


/**
 * This function is called from index.html once the Unity instance is created.
 * It stores the instance and sets up all the event listeners.
 * @param {object} instance - The Unity game instance.
 */
function initializeApp(instance) {
    unityInstance = instance;

    // --- Style Injection for Quill Dropdown Labels ---
    // This dynamically adds CSS to make the font size dropdown show readable names.
    const style = document.createElement('style');
    style.innerHTML = `
    .ql-snow .ql-picker.ql-size .ql-picker-label[data-value="tiny"]::before,
    .ql-snow .ql-picker.ql-size .ql-picker-item[data-value="tiny"]::before {
      content: 'Tiny';
    }
    .ql-snow .ql-picker.ql-size .ql-picker-label[data-value="small"]::before,
    .ql-snow .ql-picker.ql-size .ql-picker-item[data-value="small"]::before {
      content: 'Small';
    }
    /* Default 'Normal' size */
    .ql-snow .ql-picker.ql-size .ql-picker-label::before,
    .ql-snow .ql-picker.ql-size .ql-picker-item::before {
      content: 'Normal';
    }
    .ql-snow .ql-picker.ql-size .ql-picker-label[data-value="large"]::before,
    .ql-snow .ql-picker.ql-size .ql-picker-item[data-value="large"]::before {
      content: 'Large';
    }
    .ql-snow .ql-picker.ql-size .ql-picker-label[data-value="extra-large"]::before,
    .ql-snow .ql-picker.ql-size .ql-picker-item[data-value="extra-large"]::before {
      content: 'Extra Large';
    }
    `;
    document.head.appendChild(style);


    // Initialize the Quill editor with the customized toolbar
    quill = new Quill('#editor-container', {
        theme: 'snow',
        modules: {
            toolbar: [
                // Use the new named sizes. `false` corresponds to 'Normal'.
                [{ 'size': ['tiny', 'small', false, 'large', 'extra-large'] }],
                ['bold', 'italic', 'underline', 'strike'],
                [{ 'color': [] }, { 'background': [] }],
                [{ 'list': 'ordered'}, { 'list': 'bullet' }],
                ['clean']
            ]
        },
        placeholder: 'Start typing here...',
    });

    // Attach event listeners
    quill.on('text-change', () => updateTmpAndSendToUnity());
    if (copyButton) {
        copyButton.addEventListener('click', copyTmpText);
    }

    // Perform the initial conversion and send to Unity on load
    updateTmpAndSendToUnity();
}

/**
 * Main function to convert Quill content to TMP, display it, and send it to Unity.
 */
function updateTmpAndSendToUnity() {
    if (!quill || !tmpOutput) return;

    const htmlContent = quill.root.innerHTML;
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = htmlContent;

    let result = '';
    for (const child of tempDiv.childNodes) {
        result += processNode(child);
    }

    // Post-process to fix fragmented tags created by Quill
    result = mergeAdjacentTags(result);

    // This is the text with newlines (\n) that will be sent to Unity.
    const unityText = result.trim();

    // This is the text for the raw output display, with newlines replaced by the literal string "<br>".
    const displayText = unityText.replace(/\n/g, '<br>');

    // Use `textContent` to ensure tags like <br> are displayed as text, not rendered as HTML.
    tmpOutput.textContent = displayText || 'Your TextMesh Pro output will appear here...';

    // Send the text with actual newlines (\n) to Unity so it renders correctly.
    const config = {
        text: { content: unityText }
    };
    sendToUnity(config);
}

/**
 * Sends a JSON configuration object to the specified GameObject in Unity.
 */
function sendToUnity(configObject) {
    if (!unityInstance) {
        console.warn("Unity instance not ready yet. Message not sent.");
        return;
    }
    const jsonString = JSON.stringify(configObject);
    unityInstance.SendMessage(gameObjectName, methodName, jsonString);
}

/**
 * Copies the content of the TextMesh Pro output view to the user's clipboard.
 */
function copyTmpText() {
    if (!tmpOutput || !copyButton) return;

    // The textContent of the output box is now the exact raw string we want to copy.
    const textToCopy = tmpOutput.textContent;

    const tempTextArea = document.createElement('textarea');
    tempTextArea.value = textToCopy;
    document.body.appendChild(tempTextArea);
    tempTextArea.select();
    document.execCommand('copy');
    document.body.removeChild(tempTextArea);

    copyButton.textContent = 'Copied!';
    setTimeout(() => {
        copyButton.textContent = 'Copy Raw RTF';
    }, 2000);
}

// --- HTML to TextMesh Pro Conversion Logic ---

/**
 * Merges adjacent identical tags to fix nesting issues from Quill.
 * For example, turns "</b><b>" into "" and "</size=2><size=2>" into "".
 * @param {string} htmlString - The string with potentially fragmented tags.
 * @returns {string} The cleaned string.
 */
function mergeAdjacentTags(htmlString) {
    const regex = /<\/([^>]+)><\1>/g;
    let previousString;
    let newString = htmlString;
    do {
        previousString = newString;
        newString = previousString.replace(regex, '');
    } while (newString !== previousString);
    return newString;
}

/**
 * Intelligently prepends a prefix to a string, placing it inside the first
 * HTML-like tag if one exists. This ensures list bullets inherit styling.
 * @param {string} text - The text to which the prefix will be added.
 * @param {string} prefix - The string to prepend (e.g., a list bullet).
 * @returns {string} The combined string.
 */
function prependInsideFirstTag(text, prefix) {
    const trimmedText = text.trim();
    // Check if the text starts with a tag
    if (trimmedText.startsWith('<')) {
        const firstTagEnd = trimmedText.indexOf('>');
        if (firstTagEnd !== -1) {
            // Insert the prefix right after the first opening tag
            return trimmedText.slice(0, firstTagEnd + 1) + prefix + trimmedText.slice(firstTagEnd + 1);
        }
    }
    // If no tag is found at the start, just prepend normally
    return prefix + trimmedText;
}

function rgbToHex(rgb) {
    if (!rgb) return '';
    const result = rgb.match(/\d+/g);
    if (!result) return '';
    return "#" + result.map(x => {
        const hex = parseInt(x).toString(16);
        return hex.length === 1 ? "0" + hex : hex;
    }).join('').toUpperCase();
}

/**
 * Recursively processes a DOM node and its children to convert to TextMesh Pro format.
 * @param {Node} node - The DOM node to process.
 * @returns {string} The converted TextMesh Pro string.
 */
function processNode(node) {
    if (node.nodeType === Node.TEXT_NODE) {
        return node.textContent;
    }

    if (node.nodeType === Node.ELEMENT_NODE) {
        let content = '';
        for (const child of node.childNodes) {
            content += processNode(child);
        }

        // Start with the processed content of the children
        let result = content;

        // 1. Wrap with basic format tags based on the current node's tag name
        switch (node.tagName) {
            case 'STRONG': case 'B': result = `<b>${result}</b>`; break;
            case 'EM': case 'I': result = `<i>${result}</i>`; break;
            case 'U': result = `<u>${result}</u>`; break;
            case 'S': result = `<s>${result}</s>`; break;
        }

        // 2. Wrap with style tags that are on the current node itself
        if (node.classList.contains('ql-size-tiny')) {
            result = `<size=2>${result}</size>`;
        } else if (node.classList.contains('ql-size-small')) {
            result = `<size=3>${result}</size>`;
        } else if (node.classList.contains('ql-size-large')) {
            result = `<size=5>${result}</size>`;
        } else if (node.classList.contains('ql-size-extra-large')) {
            result = `<size=6>${result}</size>`;
        }

        if (node.style.color) {
            result = `<color=${rgbToHex(node.style.color)}>${result}</color>`;
        }
        if (node.style.backgroundColor) {
            result = `<mark=${rgbToHex(node.style.backgroundColor)}80>${result}</mark>`;
        }

        // 3. Handle block-level elements that add structure and newlines
        switch (node.tagName) {
            case 'P': return `${result.trim()}\n`;
            case 'LI':
                const parent = node.parentElement;
                let bullet = '• '; // Default bullet
                if (parent && parent.tagName === 'OL') {
                    const listItems = Array.from(parent.children).filter(child => child.tagName === 'LI');
                    const index = listItems.indexOf(node);
                    if (index !== -1) bullet = `${index + 1}. `;
                }
                // Use the helper to inject the bullet *inside* any existing tags.
                const liContent = prependInsideFirstTag(result, bullet);
                return `<indent=2em>${liContent}</indent>\n`;
            case 'OL': case 'UL':
                return `${result.trim()}\n`;
        }

        return result;
    }
    return '';
}
