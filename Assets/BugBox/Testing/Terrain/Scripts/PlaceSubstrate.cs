using UnityEngine;
using CustomAttributes;

public class PlaceSubstrate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JarManager Manager;
    [SerializeField] private GameObject SubstratePrefab;
    [SerializeField] private GameObject SubstrateParent;

    [Header("Spawning")]
    [SerializeField] private Vector3 SpawnOffset;
    [ReadOnly] public Transform BarrierTransform;

    public void CreateSubstrate()
        {
        if (BarrierTransform != null)
        {
            GameObject substrate = Instantiate(SubstratePrefab, (BarrierTransform.position + SpawnOffset), Quaternion.identity, SubstrateParent.transform);

            substrate.GetComponent<SubstrateTerrain>().BarrierTransform = BarrierTransform;
        }
        else { Debug.LogError("BarrierTransform is not set. Please place the substrate barrier first."); }
    }
}