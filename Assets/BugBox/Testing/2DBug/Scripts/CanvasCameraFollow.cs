using UnityEngine;
using UnityEngine.AI;

public class CanvasCameraFollow : MonoBehaviour
{
    [SerializeField]
    RectTransform m_ImageTransform;

    Camera m_Camera;
    NavMeshAgent m_Agent;

    void Start()
    {
        m_Camera = Camera.main;
        m_Agent = GetComponentInParent<NavMeshAgent>();
    }

    void LateUpdate()
    {
        transform.rotation = m_Camera.transform.rotation;
        FlipSprite();
    }

    void FlipSprite()
    {
        Vector3 worldVelocity = m_Agent.velocity;

        if (worldVelocity.sqrMagnitude < 0.01f) return;

        Vector3 screenCurrent = m_Camera.WorldToScreenPoint(transform.position);
        Vector3 screenAhead = m_Camera.WorldToScreenPoint(transform.position + worldVelocity.normalized);

        float screenDeltaX = screenAhead.x - screenCurrent.x;

        if (Mathf.Abs(screenDeltaX) < 0.01f) return;

        Vector3 scale = m_ImageTransform.localScale;
        scale.x = screenDeltaX > 0 ? -1f : 1f;
        m_ImageTransform.localScale = scale;
    }
}