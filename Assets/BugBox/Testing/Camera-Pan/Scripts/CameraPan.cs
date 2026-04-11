using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPan : MonoBehaviour
{
    [Header("Pivot")]
    [SerializeField] Transform m_Pivot;

    [Header("Pan Settings")]
    [SerializeField] float m_MaxAngleRight = 45f;
    [SerializeField] float m_MaxAngleLeft = -45f;
    [SerializeField] float m_MouseEdgeThreshold = 0.1f;
    [SerializeField] float m_PanSpeed = 45f;
    [SerializeField] float m_PauseAngle = 0f;
    [SerializeField] float m_PauseSnapDistance = 1f;

    public bool InvertControls = false;

    [Header("Camera Positions")]
    [SerializeField] Vector3 m_SquareOnPosition = new Vector3(0, 11, -21.5f);
    [SerializeField] Vector3 m_CornerPosition = new Vector3(0, 11, -25f);
    [SerializeField] Vector3 m_CameraRotation = new Vector3(15, 0, 0);

    float m_CurrentAngle = 0f;
    bool m_InputLocked = false;
    InputAction m_MousePositionAction;

    void Awake()
    {
        m_MousePositionAction = new InputAction(
            binding: "<Mouse>/position",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );
    }

    void OnEnable() => m_MousePositionAction.Enable();
    void OnDisable() => m_MousePositionAction.Disable();
    void OnDestroy() => m_MousePositionAction.Dispose();

    void Update()
    {
        float input = GetEdgeInput();

        // Unlock once the mouse has left the edge zone
        if (m_InputLocked && input == 0f)
            m_InputLocked = false;

        if (!m_InputLocked && input != 0f)
        {
            bool isPaused = Mathf.Abs(m_CurrentAngle - m_PauseAngle) < 0.01f;
            bool movingAwayFromPause = (input > 0f && m_CurrentAngle >= m_PauseAngle) ||
                                      (input < 0f && m_CurrentAngle <= m_PauseAngle);

            if (!isPaused || movingAwayFromPause)
            {
                m_CurrentAngle += input * m_PanSpeed * Time.deltaTime;
                m_CurrentAngle = Mathf.Clamp(m_CurrentAngle, m_MaxAngleLeft, m_MaxAngleRight);
            }

            bool movingTowardPause = (input < 0f && m_CurrentAngle > m_PauseAngle) ||
                                     (input > 0f && m_CurrentAngle < m_PauseAngle);

            if (movingTowardPause && Mathf.Abs(m_CurrentAngle - m_PauseAngle) < m_PauseSnapDistance)
            {
                m_CurrentAngle = m_PauseAngle;
                m_InputLocked = true;
            }
        }

        ApplyCameraTransform();
    }

    float GetEdgeInput()
    {
        Vector2 mousePos = m_MousePositionAction.ReadValue<Vector2>();

        float mouseX = mousePos.x / Screen.width;

        if (InvertControls)
        {
            if (mouseX >= 1f - m_MouseEdgeThreshold) return 1f;
            if (mouseX <= m_MouseEdgeThreshold) return -1f;
        }
        else
        {
            if (mouseX >= 1f - m_MouseEdgeThreshold) return -1f;
            if (mouseX <= m_MouseEdgeThreshold) return 1f;
        }

        return 0f;
    }

    void ApplyCameraTransform()
    {
        float t = Mathf.Abs(m_CurrentAngle) / m_MaxAngleRight;

        Vector3 localPos = Vector3.Lerp(m_SquareOnPosition, m_CornerPosition, t);

        m_Pivot.localRotation = Quaternion.Euler(0, m_CurrentAngle, 0);
        transform.localPosition = localPos;
        transform.localRotation = Quaternion.Euler(m_CameraRotation);
    }
}