using System.Collections.Generic;
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
    [SerializeField] float m_SpawnRate = 0.05f;
    [SerializeField] float m_SpawnRadius = 0.5f;
    [SerializeField] float m_SpawnHeight = 1f;
    [SerializeField] Vector3 m_FloorPosition = Vector3.zero;
    [SerializeField] LayerMask m_SpawnLayerMask = Physics.DefaultRaycastLayers;

    [Header("Smoothing")]
    [SerializeField] float m_SmoothRadius = 3f;
    [SerializeField] float m_SmoothStrength = 0.1f;

    InputAction m_ClickAction;
    InputAction m_MousePositionAction;
    float m_SpawnTimer;
    bool m_IsPouring;
    bool m_IsSmoothing;

    List<DrainageStone> m_Stones = new List<DrainageStone>();

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

    public void EnableSmoothing()
    {
        m_IsSmoothing = true;
        foreach (DrainageStone stone in m_Stones)
            stone.SetSmoothing(true);
    }

    public void EnablePouring()
    {
        m_IsSmoothing = false;
        foreach (DrainageStone stone in m_Stones)
            stone.SetSmoothing(false);
    }

    void Update()
    {
        if (m_IsSmoothing)
        {
            if (m_IsPouring)
                TrySmooth();
            else
            {
                // Re-kinematic any stones that have settled after being pushed
                foreach (DrainageStone stone in m_Stones)
                    stone.Rekinematic();
            }

            return;
        }

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
        Vector2 randomCircle = Random.insideUnitCircle * m_SpawnRadius;
        Vector3 spreadPoint = floorPoint + new Vector3(randomCircle.x, 0f, randomCircle.y);

        Vector3 clampedPoint = new Vector3(
            Mathf.Clamp(spreadPoint.x, m_TankBoundsMin.x, m_TankBoundsMax.x),
            spreadPoint.y,
            Mathf.Clamp(spreadPoint.z, m_TankBoundsMin.z, m_TankBoundsMax.z)
        );

        Vector3 rayOrigin = clampedPoint + Vector3.up * m_SpawnHeight;
        Ray downwardRay = new Ray(rayOrigin, Vector3.down);

        if (!Physics.Raycast(downwardRay, out RaycastHit hit, m_SpawnHeight * 2f, m_SpawnLayerMask))
            return;

        Vector3 spawnPosition = hit.point + Vector3.up * m_SpawnHeight;

        GameObject spawned = Instantiate(m_StonePrefab, spawnPosition, Random.rotation, m_StoneHolder);
        m_Stones.Add(spawned.GetComponent<DrainageStone>());
    }

    void TrySmooth()
    {
        Vector2 mousePos = m_MousePositionAction.ReadValue<Vector2>();
        Ray cameraRay = m_Camera.ScreenPointToRay(mousePos);

        Plane floorPlane = new Plane(Vector3.up, m_FloorPosition);

        if (!floorPlane.Raycast(cameraRay, out float enter))
            return;

        Vector3 cursorPoint = cameraRay.GetPoint(enter);

        foreach (DrainageStone stone in m_Stones)
        {
            Vector3 stonePos = stone.transform.position;

            float dist = Vector2.Distance(
                new Vector2(stonePos.x, stonePos.z),
                new Vector2(cursorPoint.x, cursorPoint.z)
            );

            if (dist > m_SmoothRadius) continue;

            // Direction away from cursor, horizontal only
            Vector3 awayFromCursor = new Vector3(
                stonePos.x - cursorPoint.x,
                0f,
                stonePos.z - cursorPoint.z
            ).normalized;

            // Stones closer to cursor get pushed more
            float influence = 1f - (dist / m_SmoothRadius);
            Vector3 force = awayFromCursor * m_SmoothStrength * influence;

            stone.ApplySmoothForce(force);
        }
    }
}