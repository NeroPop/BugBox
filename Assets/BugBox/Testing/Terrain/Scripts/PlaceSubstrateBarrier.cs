using System;
using UnityEngine;

public class PlaceSubstrateBarrier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject BarrierPrefab;
    [SerializeField] private Transform BarrierParent;

    [Header("Spawning")]
    [SerializeField] private Vector3 SpawnPosition;
    [SerializeField] private Quaternion SpawnRotation = Quaternion.identity;

    private GameObject substrateBarrier;

    public void PlaceBarrier()
    {
        if (substrateBarrier == null)
        substrateBarrier = Instantiate(BarrierPrefab, SpawnPosition, SpawnRotation, BarrierParent);
    }
}
