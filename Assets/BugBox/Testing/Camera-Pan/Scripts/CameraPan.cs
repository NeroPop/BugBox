using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPan : MonoBehaviour
{
    [Header("Pivot")]
    [SerializeField] Transform m_Pivot;

    [Header("Pan Settings")]
    [SerializeField] float m_MaxAngle = 45f;
    [SerializeField] float m_MinAngle = 0f;
    [SerializeField] float m_EdgeThreshold = 0.1f;
    [SerializeField] float m_PanSpeed = 45f;

    public bool InvertControls = false;

    [Header("Camera Positions")]
    [SerializeField] Vector3 m_SquareOnPosition = new Vector3(0, 11, -21.5f);
    [SerializeField] Vector3 m_CornerPosition = new Vector3(0, 11, -25f);
    [SerializeField] Vector3 m_CameraRotation = new Vector3(15, 0, 0);

    float m_CurrentAngle = 0f;
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
        if (input != 0f)
        {
            m_CurrentAngle += input * m_PanSpeed * Time.deltaTime;
            m_CurrentAngle = Mathf.Clamp(m_CurrentAngle, m_MinAngle, m_MaxAngle);
        }

        ApplyCameraTransform();
    }

    float GetEdgeInput()
    {
        Vector2 mousePos = m_MousePositionAction.ReadValue<Vector2>();

        float mouseX = mousePos.x / Screen.width;

        if (InvertControls)
        {
            if (mouseX >= 1f - m_EdgeThreshold) return 1f;
            if (mouseX <= m_EdgeThreshold) return -1f;
        }
        else
        {
            if (mouseX >= 1f - m_EdgeThreshold) return -1f;
            if (mouseX <= m_EdgeThreshold) return 1f;
        }

        return 0f;
    }

    void ApplyCameraTransform()
    {
        float t = Mathf.Abs(m_CurrentAngle) / m_MaxAngle;

        Vector3 localPos = Vector3.Lerp(m_SquareOnPosition, m_CornerPosition, t);

        m_Pivot.localRotation = Quaternion.Euler(0, m_CurrentAngle, 0);
        transform.localPosition = localPos;
        transform.localRotation = Quaternion.Euler(m_CameraRotation);
    }
}