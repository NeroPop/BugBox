using UnityEngine;

public class DrainageStone : MonoBehaviour
{
    [SerializeField] float m_SettleTime = 1f;  // seconds to wait after spawning before allowing kinematic

    Rigidbody m_Rigidbody;
    bool m_SmoothingPending;
    float m_TimeAlive;

    void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        m_TimeAlive += Time.deltaTime;

        if (m_SmoothingPending && m_TimeAlive >= m_SettleTime)
        {
            m_Rigidbody.isKinematic = true;
            m_SmoothingPending = false;
        }
    }

    public void SetSmoothing(bool smoothing)
    {
        if (smoothing)
        {
            if (m_TimeAlive >= m_SettleTime)
                m_Rigidbody.isKinematic = true;
            else
                m_SmoothingPending = true;
        }
        else
        {
            m_SmoothingPending = false;
            m_Rigidbody.isKinematic = false;
        }
    }
}