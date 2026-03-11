using UnityEngine;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// A mobile-optimized orbit camera controller for procedural 3D scenes.
    /// Supports:
    /// - 1-finger drag: Orbit around the target (pitch/yaw)
    /// - 2-finger pinch: Zoom in/out
    /// - 2-finger drag: Pan the camera target
    /// - Double-tap: Reset the camera target and zoom
    /// 
    /// Optimized for flutter_unity_widget with proper touch handling.
    /// </summary>
    public class TouchOrbitCamera : MonoBehaviour
    {
        [Header("Orbit Target")]
        [Tooltip("The point in space the camera orbits around")]
        public Vector3 targetPoint = Vector3.zero;
        private Vector3 _initialTargetPoint;

        [Header("Distance & Zoom (Pinch)")]
        public float distance = 30f;
        public float minDistance = 5f;
        public float maxDistance = 150f;
        [Tooltip("Zoom sensitivity for pinch gesture")]
        public float zoomSpeed = 0.5f;
        private float _initialDistance;

        [Header("Orbit Speeds (1-Finger Drag)")]
        [Tooltip("Horizontal rotation speed")]
        public float orbitSpeed = 120f;
        [Tooltip("Vertical rotation speed")]
        public float verticalSpeed = 120f;
        public float yMinLimit = -89f;
        public float yMaxLimit = 89f;

        [Header("Pan Speeds (2-Finger Drag)")]
        [Tooltip("Pan sensitivity for 2-finger drag")]
        public float panSpeed = 0.5f;

        // Internal rotation state
        private float _x = 0f;
        private float _y = 0f;

        // Input state tracking
        private float _lastPinchDistance = 0f;
        private Vector2 _lastPanPosition;
        private float _lastTapTime;
        private const float DOUBLE_TAP_THRESHOLD = 0.3f;
        
        // Performance optimization - only update when input changes
        private bool _needsUpdate = false;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private float _lastDistance;
        private Vector3 _lastTargetPoint;

        private void Start()
        {
            _initialDistance = distance;
            _initialTargetPoint = targetPoint;

            Vector3 angles = transform.eulerAngles;
            _x = angles.y;
            _y = angles.x;

            UpdateCameraTransform();
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            _lastDistance = distance;
            _lastTargetPoint = targetPoint;
        }

        private void LateUpdate()
        {
            _needsUpdate = false;
            
            HandleDesktopTestingInput();
            HandleMobileInput();
            
            // Only update camera transform if something changed
            if (_needsUpdate || 
                Vector3.Distance(targetPoint, _lastTargetPoint) > 0.001f ||
                Mathf.Abs(distance - _lastDistance) > 0.001f)
            {
                UpdateCameraTransform();
                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
                _lastDistance = distance;
                _lastTargetPoint = targetPoint;
            }
        }

        private void HandleMobileInput()
        {
            // Early exit for no touches
            if (Input.touchCount == 0) return;

            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                // Check for double tap
                if (touch.phase == TouchPhase.Began)
                {
                    if (Time.unscaledTime - _lastTapTime < DOUBLE_TAP_THRESHOLD)
                    {
                        ResetCamera();
                        _needsUpdate = true;
                    }
                    _lastTapTime = Time.unscaledTime;
                }
                // 1-finger drag: Orbit
                else if (touch.phase == TouchPhase.Moved)
                {
                    _x += touch.deltaPosition.x * orbitSpeed * 0.005f;
                    _y -= touch.deltaPosition.y * verticalSpeed * 0.005f;
                    _y = ClampAngle(_y, yMinLimit, yMaxLimit);
                    _needsUpdate = true;
                }
            }
            else if (Input.touchCount == 2)
            {
                Touch t1 = Input.GetTouch(0);
                Touch t2 = Input.GetTouch(1);

                // Initialize pinch and pan on begin
                if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
                {
                    _lastPinchDistance = Vector2.Distance(t1.position, t2.position);
                    _lastPanPosition = (t1.position + t2.position) / 2f;
                }
                // Handle pinch and pan on move
                else if (t1.phase == TouchPhase.Moved || t2.phase == TouchPhase.Moved)
                {
                    // Calculate zoom
                    float currentPinchDist = Vector2.Distance(t1.position, t2.position);
                    float pinchDelta = currentPinchDist - _lastPinchDistance;

                    float newDistance = distance - pinchDelta * zoomSpeed;
                    if (Mathf.Abs(newDistance - distance) > 0.001f)
                    {
                        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
                        _needsUpdate = true;
                    }

                    _lastPinchDistance = currentPinchDist;

                    // Calculate pan (2-finger drag)
                    Vector2 currentPanPos = (t1.position + t2.position) / 2f;
                    Vector2 panDelta = currentPanPos - _lastPanPosition;

                    if (panDelta.sqrMagnitude > 0.001f)
                    {
                        // Transform pan delta into world space relative to camera orientation
                        Vector3 panMove = (transform.right * -panDelta.x + transform.up * -panDelta.y) * panSpeed * 0.01f;
                        targetPoint += panMove;
                        _needsUpdate = true;
                    }

                    _lastPanPosition = currentPanPos;
                }
            }
        }

        private void HandleDesktopTestingInput()
        {
            // Fallback for Unity Editor / Desktop testing
            if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftShift)) // Left click drag: Orbit
            {
                float deltaX = Input.GetAxis("Mouse X");
                float deltaY = Input.GetAxis("Mouse Y");
                
                if (Mathf.Abs(deltaX) > 0.001f || Mathf.Abs(deltaY) > 0.001f)
                {
                    _x += deltaX * orbitSpeed * 0.05f;
                    _y -= deltaY * verticalSpeed * 0.05f;
                    _y = ClampAngle(_y, yMinLimit, yMaxLimit);
                    _needsUpdate = true;
                }
            }
            else if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftShift)) // Shift + Left click drag: Pan
            {
                float panDeltaX = Input.GetAxis("Mouse X");
                float panDeltaY = Input.GetAxis("Mouse Y");
                
                if (Mathf.Abs(panDeltaX) > 0.001f || Mathf.Abs(panDeltaY) > 0.001f)
                {
                    Vector3 panMove = (transform.right * -panDeltaX + transform.up * -panDeltaY) * panSpeed;
                    targetPoint += panMove;
                    _needsUpdate = true;
                }
            }

            // Mouse scroll: Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                distance -= scroll * zoomSpeed * 50f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
                _needsUpdate = true;
            }
        }

        private void UpdateCameraTransform()
        {
            Quaternion rotation = Quaternion.Euler(_y, _x, 0f);
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + targetPoint;

            transform.rotation = rotation;
            transform.position = position;
        }

        /// <summary>
        /// Call this externally to initialize the camera targeting a specific bounds center.
        /// Automatically adjusts distance based on scene size.
        /// </summary>
        public void SetTarget(Vector3 newTargetPoint, float newDistance)
        {
            _initialTargetPoint = newTargetPoint;
            _initialDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
            ResetCamera();
        }

        /// <summary>
        /// Applies configuration from SceneConfig for JSON-driven camera setup.
        /// </summary>
        public void ApplyConfig(CameraConfig config)
        {
            if (config == null) return;

            // Apply target point
            if (config.target != null && config.target.Length >= 3)
            {
                targetPoint = new Vector3(config.target[0], config.target[1], config.target[2]);
                _initialTargetPoint = targetPoint;
                _needsUpdate = true;
            }

            // Apply distance
            if (config.distance > 0)
            {
                distance = Mathf.Clamp(config.distance, minDistance, maxDistance);
                _initialDistance = distance;
                _needsUpdate = true;
            }

            // Apply initial elevation
            if (config.elevation > 0)
            {
                _y = config.elevation;
                _needsUpdate = true;
            }

            // Update camera transform immediately
            UpdateCameraTransform();
        }

        /// <summary>
        /// Reset camera to initial position and framing.
        /// </summary>
        public void ResetCamera()
        {
            targetPoint = _initialTargetPoint;
            distance = _initialDistance;
            
            // Reset to a nice default viewing angle
            _x = 20f; // slight horizontal offset
            _y = 25f; // elevation angle
            _needsUpdate = true;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
