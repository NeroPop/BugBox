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

    [Header("Terrain Limits")]
    [SerializeField] float m_MaxHeight = 5f;
    [ReadOnly][SerializeField] float m_MinHeight = 0f;
    [ReadOnly] public Transform BarrierTransform;

    [Header("NavMesh")]
    [SerializeField] NavMeshSurface m_NavMeshSurface;

    MeshFilter m_MeshFilter;
    MeshCollider m_MeshCollider;
    Camera m_Camera;

    InputAction m_RaiseAction;
    InputAction m_LowerAction;
    InputAction m_MousePositionAction;

    bool m_IsRaising;
    bool m_IsLowering;

    AsyncOperation m_LastNavMeshUpdate;

    void Awake()
    {
        m_RaiseAction = new InputAction(binding: "<Mouse>/leftButton", type: InputActionType.Button);
        m_LowerAction = new InputAction(binding: "<Mouse>/rightButton", type: InputActionType.Button);

        m_MousePositionAction = new InputAction(
            binding: "<Mouse>/position",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );

        m_RaiseAction.performed += _ => m_IsRaising = true;
        m_RaiseAction.canceled += _ => { m_IsRaising = false; FinaliseOnRelease(); };

        m_LowerAction.performed += _ => m_IsLowering = true;
        m_LowerAction.canceled += _ => { m_IsLowering = false; FinaliseOnRelease(); };
    }

    void Start()
    {
        m_Camera = Camera.main;
        m_MeshFilter = GetComponent<MeshFilter>();
        m_MeshCollider = GetComponent<MeshCollider>();

        m_MinHeight = BarrierTransform.position.y;
    }

    void OnEnable()
    {
        m_RaiseAction.Enable();
        m_LowerAction.Enable();
        m_MousePositionAction.Enable();
    }

    void OnDisable()
    {
        m_RaiseAction.Disable();
        m_LowerAction.Disable();
        m_MousePositionAction.Disable();
    }

    void OnDestroy()
    {
        m_RaiseAction.Dispose();
        m_LowerAction.Dispose();
        m_MousePositionAction.Dispose();
    }

    void Update()
    {
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

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 worldVert = transform.TransformPoint(verts[i]);
            float gaussian = Gaussian(worldVert, centre, m_Radius);
            float newY = verts[i].y + displacement.y * gaussian;

            verts[i].y = Mathf.Clamp(newY, m_MinHeight, m_MaxHeight);
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
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
        float y = pos.y - mean.y;
        float z = pos.z - mean.z;
        float n = 1.0f / (2.0f * Mathf.PI * radius * radius);
        return n * Mathf.Pow(2.718281828f, -(x * x + y * y + z * z) / (2.0f * radius * radius));
    }
}