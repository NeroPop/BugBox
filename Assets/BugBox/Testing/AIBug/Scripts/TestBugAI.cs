using UnityEngine;
using UnityEngine.AI;

public class TestBugAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Animator BugAnimator;

    [Header("Wandering")]
    [SerializeField] float m_Range = 25.0f;
    [SerializeField] float m_MinIdleTime = 1f;
    [SerializeField] float m_MaxIdleTime = 5f;
    [SerializeField] float m_IdleChance = 0.3f;

    [Header("Avoidance")]
    [SerializeField] float m_StuckTimeout = 2f;       // seconds before assuming stuck
    [SerializeField] float m_StuckThreshold = 0.05f;  // minimum movement to not be considered stuck

    NavMeshAgent m_Agent;
    float m_IdleTimer;
    bool m_Idling;

    Vector3 m_LastPosition;
    float m_StuckTimer;

    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();

        // Built-in avoidance quality - Higher is better but more expensive
        m_Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        m_LastPosition = transform.position;
        SetDestination();
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
        else
        {
            CheckIfStuck();
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

    void CheckIfStuck()
    {
        float movedDistance = Vector3.Distance(transform.position, m_LastPosition);

        if (movedDistance < m_StuckThreshold)
        {
            m_StuckTimer += Time.deltaTime;

            if (m_StuckTimer >= m_StuckTimeout)
            {
                m_StuckTimer = 0f;
                SetDestination();
            }
        }
        else
        {
            m_StuckTimer = 0f;
        }

        m_LastPosition = transform.position;
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