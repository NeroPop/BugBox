using UnityEngine;

public class DrainageStone : MonoBehaviour
{
    [SerializeField] float m_SettleTime = 1f;

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

    public void ApplySmoothForce(Vector3 force)
    {
        // Temporarily non-kinematic so physics can move it
        m_Rigidbody.isKinematic = false;
        m_Rigidbody.AddForce(force, ForceMode.VelocityChange);
    }

    public void Rekinematic()
    {
        if (m_Rigidbody.IsSleeping())
            m_Rigidbody.isKinematic = true;
        else
            m_SmoothingPending = true;
    }
}