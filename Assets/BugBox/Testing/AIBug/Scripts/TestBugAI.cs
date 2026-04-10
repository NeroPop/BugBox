using UnityEngine;
using UnityEngine.AI;

public class TestBugAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Animator BugAnimator;

    [Header("Preferences")]
    [SerializeField] float m_Range = 25.0f;
    [SerializeField] float m_MinIdleTime = 1f;
    [SerializeField] float m_MaxIdleTime = 5f;
    [SerializeField] float m_IdleChance = 0.3f;   // 0-1 probability of stopping at each destination

    NavMeshAgent m_Agent;
    float m_IdleTimer;
    bool m_Idling;

    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        StartWalking();
    }

    void Update()
    {
        if (m_Agent.pathPending || !m_Agent.isOnNavMesh)
            return;

        if (m_Idling)
        {
            m_IdleTimer -= Time.deltaTime;
            if (m_IdleTimer <= 0f)
            {
                m_Idling = false;
                SetDestination();
                StartWalking();
            }
            return;
        }

        if (m_Agent.remainingDistance > 0.1f)
            return;

        if (Random.value < m_IdleChance)
        {
            m_Idling = true;
            m_IdleTimer = Random.Range(m_MinIdleTime, m_MaxIdleTime);
            StartIdle();
        }
        else
        {
            SetDestination();
        }
    }

    void SetDestination()
    {
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * m_Range;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, m_Range, NavMesh.AllAreas))
            m_Agent.destination = hit.position;
    }

    private void StartIdle()
    {
        BugAnimator.SetBool("isWalking", false);
    }

    private void StartWalking()
    {
        BugAnimator.SetBool("isWalking", true);
    }
}