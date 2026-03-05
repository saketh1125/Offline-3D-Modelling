using UnityEngine;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Attaches to Camera.main to allow interactive orbiting around a procedural scene.
    /// Supports mouse and touch controls natively.
    /// </summary>
    public class OrbitCameraController : MonoBehaviour
    {
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

        private void Start()
        {
            _currentDistance = initialDistance;

            // Attempt to derive initial angles from the camera's spawn rotation (e.g. from JSON settings)
            Vector3 angles = transform.eulerAngles;
            _pitch = angles.x;
            _yaw = angles.y;
        }

        private void LateUpdate()
        {
            HandleInput();
            UpdateCameraTransform();
        }

        private void HandleInput()
        {
            // --- Editor/Desktop Mouse Input ---
            // Left or Right click drag rotates yaw and pitch
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                // Note: Time.deltaTime isn't strictly necessary for Input.GetAxis("Mouse X") but handles smoothing generically
                _yaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                _pitch -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            }

            // Mouse scroll wheel adjusts distance
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _currentDistance -= scroll * zoomSpeed * 10f; // Multiplier added as standard scroll units are very small
            }

            // --- Mobile Touch Input ---
            if (Input.touchCount == 1)
            {
                // Single finger drag rotates camera
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    _yaw += touch.deltaPosition.x * rotationSpeed * Time.deltaTime * 0.05f;
                    _pitch -= touch.deltaPosition.y * rotationSpeed * Time.deltaTime * 0.05f;
                }
            }
            else if (Input.touchCount == 2)
            {
                // Two finger pinch adjusts zoom distance
                Touch touch1 = Input.GetTouch(0);
                Touch touch2 = Input.GetTouch(1);

                if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
                {
                    _lastPinchDistance = Vector2.Distance(touch1.position, touch2.position);
                }
                else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
                {
                    float currentPinchDistance = Vector2.Distance(touch1.position, touch2.position);
                    float pinchDelta = currentPinchDistance - _lastPinchDistance;
                    
                    _currentDistance -= pinchDelta * zoomSpeed * 0.05f;
                    _lastPinchDistance = currentPinchDistance;
                }
            }

            // Clamp values safely
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
        }

        private void UpdateCameraTransform()
        {
            // Convert spherical coordinates to Cartesian Position
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 position = targetPosition - (rotation * Vector3.forward * _currentDistance);

            transform.position = position;
            transform.LookAt(targetPosition);
        }
    }
}
