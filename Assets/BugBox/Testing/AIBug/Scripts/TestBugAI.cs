using UnityEngine;
using UnityEngine.AI;

public class TestBugAI : MonoBehaviour
{
    public float m_Range = 25.0f;
    NavMeshAgent m_Agent;

    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (m_Agent.pathPending || !m_Agent.isOnNavMesh || m_Agent.remainingDistance > 0.1f)
            return;

        m_Agent.destination = m_Range * Random.insideUnitSphere;
    }
}
