using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshRenderer))]
public class SubstrateMeshGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] public int m_Resolution = 30;
    [SerializeField] public float m_Width = 10f;
    [SerializeField] public float m_Depth = 10f;
    [SerializeField] public float m_Thickness = 2f;

    MeshFilter m_MeshFilter;
    MeshCollider m_MeshCollider;

    // Track top face vertex count so we can find edge verts later
    int m_TopVertCount;

    public void Generate()
    {
        m_MeshFilter = GetComponent<MeshFilter>();
        m_MeshCollider = GetComponent<MeshCollider>();

        Mesh mesh = BuildMesh();
        m_MeshFilter.mesh = mesh;
        m_MeshCollider.sharedMesh = mesh;
    }

    // Called by SubstrateTerrain after each sculpt to sync side tops to top face edges
    public void SyncSidesToTopFace()
    {
        Mesh mesh = m_MeshFilter.mesh;
        Vector3[] verts = mesh.vertices;
        int res = m_Resolution;

        // Bottom row of top face (front edge, z = min)
        // Top row of top face (back edge, z = max)
        // Left column (x = min)
        // Right column (x = max)

        // Side verts start after m_TopVertCount
        // Each side has (m_Resolution) * 2 verts (top + bottom pairs)
        int sideStart = m_TopVertCount;

        // Front side — top verts match bottom row of top face (z=0 row)
        for (int i = 0; i < res; i++)
        {
            int topFaceVert = i;                        // bottom row of top face
            int sideVert = sideStart + i * 2;        // top vert of front side pair

            verts[sideVert].y = verts[topFaceVert].y;  // match height
        }
        sideStart += res * 2;

        // Back side — top verts match top row of top face
        for (int i = 0; i < res; i++)
        {
            int topFaceVert = (res - 1) * res + (res - 1 - i);
            int sideVert = sideStart + i * 2;

            verts[sideVert].y = verts[topFaceVert].y;
        }
        sideStart += res * 2;

        // Left side — top verts match left column of top face
        for (int i = 0; i < res; i++)
        {
            int topFaceVert = (res - 1 - i) * res;
            int sideVert = sideStart + i * 2;

            verts[sideVert].y = verts[topFaceVert].y;
        }
        sideStart += res * 2;

        // Right side — top verts match right column of top face
        for (int i = 0; i < res; i++)
        {
            int topFaceVert = i * res + (res - 1);
            int sideVert = sideStart + i * 2;

            verts[sideVert].y = verts[topFaceVert].y;
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        m_MeshCollider.sharedMesh = mesh;
    }

    Mesh BuildMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uvs = new List<Vector2>();

        BuildTopFace(vertices, triangles, uvs);

        m_TopVertCount = vertices.Count;

        BuildSideFace(vertices, triangles, uvs, Side.Front);
        BuildSideFace(vertices, triangles, uvs, Side.Back);
        BuildSideFace(vertices, triangles, uvs, Side.Left);
        BuildSideFace(vertices, triangles, uvs, Side.Right);

        Mesh mesh = new Mesh();
        mesh.name = "SubstrateMesh";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    enum Side { Front, Back, Left, Right }

    void BuildTopFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        int startIndex = vertices.Count;

        for (int z = 0; z < m_Resolution; z++)
        {
            for (int x = 0; x < m_Resolution; x++)
            {
                float xPos = (x / (float)(m_Resolution - 1) - 0.5f) * m_Width;
                float zPos = (z / (float)(m_Resolution - 1) - 0.5f) * m_Depth;

                vertices.Add(new Vector3(xPos, 0f, zPos));
                uvs.Add(new Vector2(x / (float)(m_Resolution - 1),
                                    z / (float)(m_Resolution - 1)));
            }
        }

        for (int z = 0; z < m_Resolution - 1; z++)
        {
            for (int x = 0; x < m_Resolution - 1; x++)
            {
                int i = startIndex + z * m_Resolution + x;

                triangles.Add(i);
                triangles.Add(i + m_Resolution);
                triangles.Add(i + 1);

                triangles.Add(i + 1);
                triangles.Add(i + m_Resolution);
                triangles.Add(i + m_Resolution + 1);
            }
        }
    }

    void BuildSideFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Side side)
    {
        int startIndex = vertices.Count;
        int steps = m_Resolution - 1;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;

            Vector3 top, bottom;

            switch (side)
            {
                case Side.Front:
                    top = new Vector3((t - 0.5f) * m_Width, 0f, -m_Depth * 0.5f);
                    bottom = new Vector3((t - 0.5f) * m_Width, -m_Thickness, -m_Depth * 0.5f);
                    break;
                case Side.Back:
                    top = new Vector3((0.5f - t) * m_Width, 0f, m_Depth * 0.5f);
                    bottom = new Vector3((0.5f - t) * m_Width, -m_Thickness, m_Depth * 0.5f);
                    break;
                case Side.Left:
                    top = new Vector3(-m_Width * 0.5f, 0f, (0.5f - t) * m_Depth);
                    bottom = new Vector3(-m_Width * 0.5f, -m_Thickness, (0.5f - t) * m_Depth);
                    break;
                default: // Right
                    top = new Vector3(m_Width * 0.5f, 0f, (t - 0.5f) * m_Depth);
                    bottom = new Vector3(m_Width * 0.5f, -m_Thickness, (t - 0.5f) * m_Depth);
                    break;
            }

            vertices.Add(top);
            vertices.Add(bottom);
            uvs.Add(new Vector2(t, 1f));
            uvs.Add(new Vector2(t, 0f));
        }

        for (int i = 0; i < steps; i++)
        {
            int a = startIndex + i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;

            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);

            triangles.Add(c);
            triangles.Add(b);
            triangles.Add(d);
        }
    }
}