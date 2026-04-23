using UnityEngine;

public class OutlineObject : MonoBehaviour
{
    [SerializeField] bool m_HasOutline = true;

    void Start()
    {
        ApplyOutline();
    }

    void ApplyOutline()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.sharedMaterials)
            {
                if (mat == null) continue;

                mat.SetInt("_StencilRef", m_HasOutline ? 1 : 0);
                mat.SetInt("_StencilComp", (int)UnityEngine.Rendering.CompareFunction.Always);
                mat.SetInt("_StencilOp", (int)UnityEngine.Rendering.StencilOp.Replace);
                mat.SetFloat("_EnableOutline", m_HasOutline ? 1f : 0f);
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate() => ApplyOutline();
#endif
}