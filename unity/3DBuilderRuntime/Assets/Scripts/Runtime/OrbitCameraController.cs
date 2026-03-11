using UnityEngine;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// DEPRECATED: This controller is disabled in favor of TouchOrbitCamera.
    /// TouchOrbitCamera provides better mobile gesture support and performance optimizations.
    /// </summary>
    public class OrbitCameraController : MonoBehaviour
    {
        [Header("DEPRECATED - Use TouchOrbitCamera instead")]
        public bool disabled = true;
        
        private void Start()
        {
            if (disabled)
            {
                Debug.LogWarning("[OrbitCameraController] This component is deprecated. Please use TouchOrbitCamera for mobile gesture support.");
                this.enabled = false;
                return;
            }
            
            // Original initialization code preserved but disabled
            _currentDistance = initialDistance;
            Vector3 angles = transform.eulerAngles;
            _pitch = angles.x;
            _yaw = angles.y;
        }

        private void LateUpdate()
        {
            if (disabled) return;
            
            HandleInput();
            UpdateCameraTransform();
        }

        // ... rest of original code preserved but disabled
        public Vector3 targetPosition = Vector3.zero;

        [Header("Camera Parameters")]
        public float rotationSpeed = 120.0f;
        public float zoomSpeed = 10.0f;
        public float minDistance = 5.0f;
        public float maxDistance = 150.0f;
        public float initialDistance = 30.0f;

        private float _yaw = 0f;
        private float _pitch = 30f;
        private float _currentDistance;
        private float _lastPinchDistance = 0f;

        private void HandleInput()
        {
            if (disabled) return;
            
            // Original input handling code...
        }

        private void UpdateCameraTransform()
        {
            if (disabled) return;
            
            // Original transform code...
        }
    }
}
