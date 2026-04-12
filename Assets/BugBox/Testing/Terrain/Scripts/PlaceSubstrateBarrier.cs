using System;
using System.Collections;
using System.Net;
using UnityEngine;

public class PlaceSubstrateBarrier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JarManager Manager;
    [SerializeField] private GameObject BarrierPrefab;
    [SerializeField] private Transform BarrierParent;

    [Header("Spawning")]
    [SerializeField] private Vector3 SpawnPosition;
    [SerializeField] private Quaternion SpawnRotation = Quaternion.identity;
    [SerializeField] private float SettleTime = 5f;

    private GameObject substrateBarrier;

    public void PlaceBarrier()
    {
        if (substrateBarrier == null)
        substrateBarrier = Instantiate(BarrierPrefab, SpawnPosition, SpawnRotation, BarrierParent);
        StartCoroutine(SettleBarrier());
    }

    IEnumerator SettleBarrier()
    {
        yield return new WaitForSeconds(SettleTime);
        Rigidbody rb = substrateBarrier.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;

            Manager.BarrierTransform = substrateBarrier.transform;
            Manager.SubstrateBarrierSettled();
        }
        yield return null;
    }
}