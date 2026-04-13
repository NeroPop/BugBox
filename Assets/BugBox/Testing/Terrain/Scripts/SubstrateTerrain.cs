using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using Unity.AI.Navigation;
using CustomAttributes;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class SubstrateTerrain : MonoBehaviour
{
    [Header("Brush Settings")]
    [SerializeField] float m_Radius = 1.5f;
    [SerializeField] float m_Power = 2.0f;

    [Header("Smooth Brush")]
    [SerializeField] float m_SmoothRadius = 3f;
    [SerializeField] float m_SmoothStrength = 0.3f;

    [Header("Terrain Limits")]
    [ReadOnly][SerializeField] float m_MaxHeight = 5f;
    [SerializeField] float m_MaxHeightOffset = 0f;
    [ReadOnly][SerializeField] float m_MinHeight = 0f;
    [SerializeField] float m_MinHeightOffset = 0f;
    [ReadOnly] public Transform LidTransform;
    [ReadOnly] public Transform BarrierTransform;

    [Header("NavMesh")]
    [SerializeField] NavMeshSurface m_NavMeshSurface;

    MeshFilter m_MeshFilter;
    MeshCollider m_MeshCollider;
    SubstrateMeshGenerator m_MeshGenerator;
    Camera m_Camera;

    float m_TopFaceY;
    int m_TopVertCount;

    InputAction m_RaiseAction;
    InputAction m_LowerAction;
    InputAction m_ClickAction;
    InputAction m_MousePositionAction;

    bool m_IsRaising;
    bool m_IsLowering;
    bool m_IsMouseHeld;
    bool m_IsSmoothing;

    AsyncOperation m_LastNavMeshUpdate;

    void Awake()
    {
        m_RaiseAction = new InputAction(binding: "<Mouse>/leftButton", type: InputActionType.Button);
        m_LowerAction = new InputAction(binding: "<Mouse>/rightButton", type: InputActionType.Button);
        m_ClickAction = new InputAction(binding: "<Mouse>/leftButton", type: InputActionType.Button);

        m_MousePositionAction = new InputAction(
            binding: "<Mouse>/position",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );

        m_RaiseAction.performed += _ => m_IsRaising = true;
        m_RaiseAction.canceled += _ => { m_IsRaising = false; FinaliseOnRelease(); };

        m_LowerAction.performed += _ => m_IsLowering = true;
        m_LowerAction.canceled += _ => { m_IsLowering = false; FinaliseOnRelease(); };

        m_ClickAction.performed += _ => m_IsMouseHeld = true;
        m_ClickAction.canceled += _ => { m_IsMouseHeld = false; FinaliseOnRelease(); };
    }

    void Start()
    {
        m_Camera = Camera.main;
        m_MeshFilter = GetComponent<MeshFilter>();
        m_MeshCollider = GetComponent<MeshCollider>();
        m_MeshGenerator = GetComponent<SubstrateMeshGenerator>();

        m_MinHeight = transform.InverseTransformPoint(
            new Vector3(0, BarrierTransform.position.y + m_MinHeightOffset, 0)).y;
        m_MaxHeight = transform.InverseTransformPoint(
            new Vector3(0, LidTransform.position.y + m_MaxHeightOffset, 0)).y;

        m_TopFaceY = float.MinValue;
        foreach (Vector3 v in m_MeshFilter.mesh.vertices)
            if (v.y > m_TopFaceY) m_TopFaceY = v.y;

        m_TopVertCount = m_MeshGenerator.m_Resolution * m_MeshGenerator.m_Resolution;
    }

    void OnEnable()
    {
        m_RaiseAction.Enable();
        m_LowerAction.Enable();
        m_ClickAction.Enable();
        m_MousePositionAction.Enable();
    }

    void OnDisable()
    {
        m_RaiseAction.Disable();
        m_LowerAction.Disable();
        m_ClickAction.Disable();
        m_MousePositionAction.Disable();
    }

    void OnDestroy()
    {
        m_RaiseAction.Dispose();
        m_LowerAction.Dispose();
        m_ClickAction.Dispose();
        m_MousePositionAction.Dispose();
    }

    public void EnableSmoothBrush() => m_IsSmoothing = true;
    public void DisableSmoothBrush() => m_IsSmoothing = false;

    void Update()
    {
        // Smoothing mode — left click to smooth
        if (m_IsSmoothing)
        {
            if (!m_IsMouseHeld) return;

            Vector2 mousePosSmooth = m_MousePositionAction.ReadValue<Vector2>();
            Ray raySmooth = m_Camera.ScreenPointToRay(mousePosSmooth);

            if (!Physics.Raycast(raySmooth, out RaycastHit smoothHit)) return;
            if (smoothHit.collider.gameObject != gameObject) return;

            SmoothMesh(smoothHit.point);
            UpdateNavMesh();
            return;
        }

        // Raise/lower mode
        if (!m_IsRaising && !m_IsLowering) return;

        Vector2 mousePos = m_MousePositionAction.ReadValue<Vector2>();
        Ray ray = m_Camera.ScreenPointToRay(mousePos);

        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        if (hit.collider.gameObject != gameObject) return;

        float direction = m_IsRaising ? 1f : -1f;
        ModifyMesh(Vector3.up * m_Power * direction, hit.point);
        UpdateNavMesh();
    }

    void ModifyMesh(Vector3 displacement, Vector3 centre)
    {
        Mesh mesh = m_MeshFilter.mesh;
        Vector3[] verts = mesh.vertices;

        for (int i = 0; i < m_TopVertCount; i++)
        {
            Vector3 worldVert = transform.TransformPoint(verts[i]);
            float gaussian = Gaussian(worldVert, centre, m_Radius);
            float newY = verts[i].y + displacement.y * gaussian;

            verts[i].y = Mathf.Clamp(newY, m_MinHeight, m_MaxHeight);
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        if (m_MeshGenerator != null)
            m_MeshGenerator.SyncSidesToTopFace();
    }

    void SmoothMesh(Vector3 centre)
    {
        Mesh mesh = m_MeshFilter.mesh;
        Vector3[] verts = mesh.vertices;

        float averageY = GetAverageHeightAround(verts, centre);

        for (int i = 0; i < m_TopVertCount; i++)
        {
            Vector3 worldVert = transform.TransformPoint(verts[i]);

            float dist = Vector2.Distance(
                new Vector2(worldVert.x, worldVert.z),
                new Vector2(centre.x, centre.z)
            );

            if (dist > m_SmoothRadius) continue;

            float influence = 1f - (dist / m_SmoothRadius);
            influence = influence * influence * (3f - 2f * influence);

            verts[i].y = Mathf.Lerp(verts[i].y, averageY, m_SmoothStrength * influence);
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        if (m_MeshGenerator != null)
            m_MeshGenerator.SyncSidesToTopFace();
    }

    float GetAverageHeightAround(Vector3[] verts, Vector3 centre)
    {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < m_TopVertCount; i++)
        {
            Vector3 worldVert = transform.TransformPoint(verts[i]);

            float dist = Vector2.Distance(
                new Vector2(worldVert.x, worldVert.z),
                new Vector2(centre.x, centre.z)
            );

            if (dist > m_SmoothRadius) continue;

            total += verts[i].y;
            count++;
        }

        return count > 0 ? total / count : centre.y;
    }

    void UpdateNavMesh()
    {
        if (m_NavMeshSurface == null) return;

        if (m_LastNavMeshUpdate == null || m_LastNavMeshUpdate.isDone)
            m_LastNavMeshUpdate = m_NavMeshSurface.UpdateNavMesh(m_NavMeshSurface.navMeshData);
    }

    void FinaliseOnRelease()
    {
        UpdateCollider();
        FinaliseNavMesh();
    }

    void UpdateCollider()
    {
        Mesh mesh = m_MeshFilter.mesh;
        Mesh colliderMesh = new Mesh();
        colliderMesh.vertices = mesh.vertices;
        colliderMesh.triangles = mesh.triangles;
        m_MeshCollider.sharedMesh = colliderMesh;
    }

    void FinaliseNavMesh()
    {
        if (m_NavMeshSurface == null) return;

        if (m_LastNavMeshUpdate != null && !m_LastNavMeshUpdate.isDone)
            NavMeshBuilder.Cancel(m_NavMeshSurface.navMeshData);

        m_LastNavMeshUpdate = m_NavMeshSurface.UpdateNavMesh(m_NavMeshSurface.navMeshData);
    }

    static float Gaussian(Vector3 pos, Vector3 mean, float radius)
    {
        float x = pos.x - mean.x;
        float z = pos.z - mean.z;
        float dist = Mathf.Sqrt(x * x + z * z);
        float t = 1f - Mathf.Clamp01(dist / radius);

        return t * t * (3f - 2f * t);
    }
}