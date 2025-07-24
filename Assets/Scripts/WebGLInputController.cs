using UnityEngine;

/// <summary>
/// This script resolves the common issue where the Unity WebGL canvas
/// captures all keyboard input, preventing HTML input fields on the same
/// page from working correctly.
/// </summary>
public class WebGLInputController : MonoBehaviour
{
	void Awake()
	{
#if !UNITY_EDITOR && UNITY_WEBGL
        WebGLInput.captureAllKeyboardInput = false;
#endif
	}
}