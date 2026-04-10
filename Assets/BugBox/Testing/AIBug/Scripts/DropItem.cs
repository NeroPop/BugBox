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

    [SerializeField]
    Vector3 SpawnOffset = new Vector3(0, 5, 0);

    Transform m_Transform;
    InputAction m_ClickAction;

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
            Instantiate(prefab, (hit.point +SpawnOffset), Quaternion.identity, spawnedPrefabsHolder);
    }
}
