using UnityEngine;
using UnityEngine.InputSystem;

public class DrainageLayerPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject m_StonePrefab;
    [SerializeField] Transform m_StoneHolder;
    [SerializeField] Camera m_Camera;

    [Header("Tank Bounds")]
    [SerializeField] Vector3 m_TankBoundsMin = new Vector3(-19f, 0f, -19f);
    [SerializeField] Vector3 m_TankBoundsMax = new Vector3(19f, 0f, 19f);

    [Header("Spawning")]
    [SerializeField] float m_SpawnRate = 0.05f;        // seconds between each spawn
    [SerializeField] float m_SpawnRadius = 0.5f;       // random spread around cursor
    [SerializeField] float m_SpawnHeight = 1f;         // how high above the hit point to spawn
    [SerializeField] Vector3 m_FloorPosition = Vector3.zero;
    [SerializeField] LayerMask m_SpawnLayerMask = Physics.DefaultRaycastLayers;

    InputAction m_ClickAction;
    InputAction m_MousePositionAction;
    float m_SpawnTimer;
    bool m_IsPouring;

    void Awake()
    {
        m_ClickAction = new InputAction(
            binding: "<Mouse>/leftButton",
            type: InputActionType.Button
        );

        m_MousePositionAction = new InputAction(
            binding: "<Mouse>/position",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );

        m_ClickAction.performed += _ => m_IsPouring = true;
        m_ClickAction.canceled += _ => m_IsPouring = false;
    }

    void Start()
    {
        if (m_Camera == null)
            m_Camera = Camera.main;
    }

    void OnEnable()
    {
        m_ClickAction.Enable();
        m_MousePositionAction.Enable();
    }

    void OnDisable()
    {
        m_ClickAction.Disable();
        m_MousePositionAction.Disable();
    }

    void OnDestroy()
    {
        m_ClickAction.Dispose();
        m_MousePositionAction.Dispose();
    }

    void Update()
    {
        if (!m_IsPouring) return;

        m_SpawnTimer -= Time.deltaTime;
        if (m_SpawnTimer <= 0f)
        {
            TrySpawnStone();
            m_SpawnTimer = m_SpawnRate;
        }
    }

    void TrySpawnStone()
    {
        Vector2 mousePos = m_MousePositionAction.ReadValue<Vector2>();
        Ray cameraRay = m_Camera.ScreenPointToRay(mousePos);

        Plane floorPlane = new Plane(Vector3.up, m_FloorPosition);

        if (!floorPlane.Raycast(cameraRay, out float enter))
            return;

        Vector3 floorPoint = cameraRay.GetPoint(enter);

        // Clamp the floor point to within the tank bounds so pressing 
        // against the back glass doesn't spawn outside
        Vector3 clampedPoint = new Vector3(
            Mathf.Clamp(floorPoint.x, m_TankBoundsMin.x, m_TankBoundsMax.x),
            floorPoint.y,
            Mathf.Clamp(floorPoint.z, m_TankBoundsMin.z, m_TankBoundsMax.z)
        );

        Vector3 rayOrigin = clampedPoint + Vector3.up * m_SpawnHeight;
        Ray downwardRay = new Ray(rayOrigin, Vector3.down);

        if (!Physics.Raycast(downwardRay, out RaycastHit hit, m_SpawnHeight * 2f, m_SpawnLayerMask))
            return;

        Vector2 randomCircle = Random.insideUnitCircle * m_SpawnRadius;
        Vector3 spawnPosition = hit.point
                              + new Vector3(randomCircle.x, m_SpawnHeight, randomCircle.y);

        Instantiate(m_StonePrefab, spawnPosition, Random.rotation, m_StoneHolder);
    }
}
