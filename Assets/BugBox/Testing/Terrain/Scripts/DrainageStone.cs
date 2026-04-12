using UnityEngine;

public class DrainageStone : MonoBehaviour
{
    Rigidbody m_Rigidbody;

    void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    public void SetSmoothing(bool smoothing)
    {
        m_Rigidbody.isKinematic = smoothing;
    }
}
