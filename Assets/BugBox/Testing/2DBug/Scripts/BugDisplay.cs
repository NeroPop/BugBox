using UnityEngine;
using UnityEngine.AI;

public class BugDisplay : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer m_SpriteRenderer;

    Camera m_Camera;
    NavMeshAgent m_Agent;

    void Start()
    {
        m_Camera = Camera.main;
        m_Agent = GetComponentInParent<NavMeshAgent>();
    }

    void LateUpdate()
    {
        FaceCamera();
        FlipSprite();
    }

    void FaceCamera()
    {
        transform.rotation = m_Camera.transform.rotation;
    }

    void FlipSprite()
    {
        Vector3 worldVelocity = m_Agent.velocity;

        if (worldVelocity.sqrMagnitude < 0.01f) return;

        Vector3 screenCurrent = m_Camera.WorldToScreenPoint(transform.position);
        Vector3 screenAhead = m_Camera.WorldToScreenPoint(transform.position + worldVelocity.normalized);

        float screenDeltaX = screenAhead.x - screenCurrent.x;

        if (Mathf.Abs(screenDeltaX) < 0.01f) return;

        m_SpriteRenderer.flipX = screenDeltaX > 0;
    }
}