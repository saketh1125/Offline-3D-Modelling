using UnityEngine;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Configuration component to ensure TouchOrbitCamera works properly with flutter_unity_widget.
    /// Place this on the same GameObject as TouchOrbitCamera.
    /// </summary>
    [RequireComponent(typeof(TouchOrbitCamera))]
    public class TouchOrbitCameraConfig : MonoBehaviour
    {
        [Header("Flutter Integration Settings")]
        [Tooltip("Enable to ensure touch events work correctly within flutter_unity_widget")]
        public bool flutterMode = true;
        
        [Tooltip("Minimum touch delta to register as movement (prevents jitter)")]
        public float touchThreshold = 0.01f;
        
        [Header("Mobile Performance Settings")]
        [Tooltip("Reduce update frequency for better mobile performance")]
        public bool optimizeForMobile = true;
        
        [Tooltip("Frames to skip between camera updates when not moving")]
        public int updateSkipFrames = 0;
        
        private TouchOrbitCamera _orbitCamera;
        private int _frameCount = 0;
        
        private void Awake()
        {
            _orbitCamera = GetComponent<TouchOrbitCamera>();
            
            if (flutterMode)
            {
                // CRITICAL: Configure touch settings for Flutter compatibility
                Input.simulateMouseWithTouches = false;
                
                // Ensure Unity receives raw touch input
                Debug.Log("[TouchOrbitCameraConfig] Flutter mode enabled - Input.simulateMouseWithTouches = false");
                
                // Additional Flutter-specific settings
                if (Application.platform == RuntimePlatform.Android)
                {
                    // Disable multi-touch emulation to ensure real touch events
                    Input.multiTouchEnabled = true;
                    Debug.Log("[TouchOrbitCameraConfig] Multi-touch enabled for Android");
                }
            }
        }
        
        private void Start()
        {
            // Apply mobile optimizations
            if (optimizeForMobile && Application.platform == RuntimePlatform.Android)
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = 60;
            }
        }
        
        private void Update()
        {
            if (!optimizeForMobile) return;
            
            // Skip updates on mobile when not interacting
            if (Input.touchCount == 0 && !Input.GetMouseButton(0))
            {
                _frameCount++;
                if (_frameCount % (updateSkipFrames + 1) != 0)
                {
                    return;
                }
            }
            else
            {
                _frameCount = 0;
            }
        }
        
        private void OnEnable()
        {
            Debug.Log("[TouchOrbitCameraConfig] Camera controls enabled with Flutter integration.");
        }
        
        private void OnDisable()
        {
            Debug.Log("[TouchOrbitCameraConfig] Camera controls disabled.");
        }
    }
}
