using UnityEngine;
using UnityEngine.InputSystem;

public class DropItem : MonoBehaviour
{
    [SerializeField]
    GameObject prefab;

    [SerializeField]
    Transform spawnedPrefabsHolder;

    [SerializeField]
    Camera spawnCamera;

    [SerializeField]
    LayerMask spawnLayerMask = Physics.DefaultRaycastLayers;

    [Header("Spawn Settings")]

    [SerializeField]
    Vector3 PositionOffset = new Vector3(0, 5, 0);

    [SerializeField]
    Quaternion RotationOffsetPositive = Quaternion.identity;
    [SerializeField]
    Quaternion RotationOffsetNegative = Quaternion.identity;

    Transform m_Transform;
    InputAction m_ClickAction;
    private Quaternion _RotationOffset;

    void Awake()
    {
        m_ClickAction = new InputAction(binding: "<Mouse>/leftButton", type: InputActionType.Button);
        m_ClickAction.performed += OnClick;
    }

    void Start()
    {
        m_Transform = transform;

        if (spawnedPrefabsHolder == null)
            spawnedPrefabsHolder = m_Transform;

        if (spawnCamera == null)
            spawnCamera = Camera.main;
    }

    void OnEnable() => m_ClickAction.Enable();
    void OnDisable() => m_ClickAction.Disable();

    void OnDestroy() => m_ClickAction.Dispose();

    void OnClick(InputAction.CallbackContext ctx)
    {
        if (prefab == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = spawnCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, spawnLayerMask))
        {
            if (hit.point.x < gameObject.transform.position.x)
            {
                _RotationOffset = RotationOffsetNegative;
            }
            else
            {
                _RotationOffset = RotationOffsetPositive;
            }

            Instantiate(prefab, (hit.point + PositionOffset), (_RotationOffset), spawnedPrefabsHolder);
        }
    }
}
