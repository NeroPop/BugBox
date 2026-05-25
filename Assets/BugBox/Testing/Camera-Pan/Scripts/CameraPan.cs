using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BugBox.Camera
{
    /// <summary>
    /// Handles camera pan around a pivot point and tray zoom view when mouse moves to bottom of screen.
    /// </summary>
    public class CameraPan : MonoBehaviour
    {
        #region Pivot and Pan Settings

        [Header("Pivot")]
        [SerializeField] private Transform _pivot;

        [Header("Pan Settings")]
        [SerializeField] private float _maxAngleRight = 45f;
        [SerializeField] private float _maxAngleLeft = -45f;
        [SerializeField] private float _mouseEdgeThreshold = 0.1f;
        [SerializeField] private float _panSpeed = 45f;
        [SerializeField] private float _pauseAngle = 0f;
        [SerializeField] private float _pauseSnapDistance = 1f;

        [SerializeField] public bool InvertControls = false;

        #endregion

        #region Camera Positions

        [Header("Camera Positions")]
        [SerializeField] private Vector3 _squareOnPosition = new Vector3(0, 11, -21.5f);
        [SerializeField] private Vector3 _cornerPosition = new Vector3(0, 11, -25f);
        [SerializeField] private Vector3 _cameraRotation = new Vector3(15, 0, 0);

        #endregion

        #region Tray Zoom

        [Header("Tray Zoom")]
        [SerializeField] private float _trayZoomOutDistance = 10f;
        [SerializeField] private float _bottomEdgeThreshold = 0.15f;
        [SerializeField] private float _trayZoomSpeed = 5f;

        #endregion

        #region State

        private float _currentAngle = 0f;
        private bool _inputLocked = false;
        private float _targetPivotZ = 0f;
        private InputAction _mousePositionAction;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _mousePositionAction = new InputAction(
                binding: "<Mouse>/position",
                type: InputActionType.Value,
                expectedControlType: "Vector2"
            );

            // Initialize target Z to square-on position Z so pivot doesn't jump on first frame
            _targetPivotZ = _squareOnPosition.z;
        }

        private void OnEnable()
        {
            _mousePositionAction.Enable();
        }

        private void OnDisable()
        {
            _mousePositionAction.Disable();
        }

        private void OnDestroy()
        {
            _mousePositionAction.Dispose();
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            float input = GetEdgeInput();
            UpdateTrayZoom();

            // Unlock once the mouse has left the edge zone
            if (_inputLocked && input == 0f)
            {
                _inputLocked = false;
            }

            if (!_inputLocked && input != 0f)
            {
                bool isPaused = Mathf.Abs(_currentAngle - _pauseAngle) < 0.01f;
                bool movingAwayFromPause = (input > 0f && _currentAngle >= _pauseAngle) ||
                                          (input < 0f && _currentAngle <= _pauseAngle);

                if (!isPaused || movingAwayFromPause)
                {
                    _currentAngle += input * _panSpeed * Time.deltaTime;
                    _currentAngle = Mathf.Clamp(_currentAngle, _maxAngleLeft, _maxAngleRight);
                }

                bool movingTowardPause = (input < 0f && _currentAngle > _pauseAngle) ||
                                         (input > 0f && _currentAngle < _pauseAngle);

                if (movingTowardPause && Mathf.Abs(_currentAngle - _pauseAngle) < _pauseSnapDistance)
                {
                    _currentAngle = _pauseAngle;
                    _inputLocked = true;
                }
            }

            ApplyCameraTransform();
        }

        #endregion

        #region Input

        private float GetEdgeInput()
        {
            Vector2 mousePos = _mousePositionAction.ReadValue<Vector2>();
            float mouseX = mousePos.x / Screen.width;

            if (InvertControls)
            {
                if (mouseX >= 1f - _mouseEdgeThreshold)
                {
                    return 1f;
                }

                if (mouseX <= _mouseEdgeThreshold)
                {
                    return -1f;
                }
            }
            else
            {
                if (mouseX >= 1f - _mouseEdgeThreshold)
                {
                    return -1f;
                }

                if (mouseX <= _mouseEdgeThreshold)
                {
                    return 1f;
                }
            }

            return 0f;
        }

        #endregion

        #region Camera Transform

        private void UpdateTrayZoom()
        {
            Vector2 mousePos = _mousePositionAction.ReadValue<Vector2>();
            float mouseY = mousePos.y / Screen.height;

            // If mouse is at bottom of screen, zoom out to tray view
            if (mouseY <= _bottomEdgeThreshold)
            {
                _targetPivotZ = _squareOnPosition.z + _trayZoomOutDistance;
            }
            else
            {
                // Otherwise, return to normal position
                _targetPivotZ = _squareOnPosition.z;
            }
        }

        private void ApplyCameraTransform()
        {
            float t = Mathf.Abs(_currentAngle) / _maxAngleRight;
            Vector3 localPos = Vector3.Lerp(_squareOnPosition, _cornerPosition, t);

            // Smoothly lerp the camera's Z position toward target for tray zoom
            float currentCameraZ = Mathf.Lerp(
                transform.localPosition.z,
                _targetPivotZ,
                _trayZoomSpeed * Time.deltaTime
            );

            _pivot.localRotation = Quaternion.Euler(0, _currentAngle, 0);
            // Pivot stays at origin
            _pivot.localPosition = Vector3.zero;

            // Camera follows the lerped position from pan angle, with Z controlled by tray zoom
            transform.localPosition = new Vector3(localPos.x, localPos.y, currentCameraZ);
            transform.localRotation = Quaternion.Euler(_cameraRotation);
        }

        #endregion
    }
}