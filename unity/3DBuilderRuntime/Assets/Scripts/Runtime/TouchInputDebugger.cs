using UnityEngine;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Debug script to verify touch input is being received from Flutter.
    /// Attach to any GameObject in the scene to see touch logs.
    /// </summary>
    public class TouchInputDebugger : MonoBehaviour
    {
        private void Update()
        {
            // Log touch count changes
            if (Input.touchCount > 0)
            {
                Debug.Log($"[TouchInputDebugger] Touch count: {Input.touchCount}");
                
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    Debug.Log($"[TouchInputDebugger] Touch {i}: phase={touch.phase}, pos={touch.position}, delta={touch.deltaPosition}");
                }
            }
            
            // Log mouse input for testing in editor
            if (Application.isEditor && Input.GetMouseButtonDown(0))
            {
                Debug.Log("[TouchInputDebugger] Mouse down detected");
            }
        }
        
        private void OnEnable()
        {
            Debug.Log("[TouchInputDebugger] Touch input debugger enabled");
            Debug.Log($"[TouchInputDebugger] Input.simulateMouseWithTouches: {Input.simulateMouseWithTouches}");
        }
    }
}
