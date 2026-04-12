using UnityEngine;
using CustomAttributes;

public class JarManager : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] DrainageLayerPlacer DrainageLayer;
    [SerializeField] PlaceSubstrateBarrier SubstrateBarrier;
    [SerializeField] PlaceSubstrate SubstrateLayer;

    [Header("References")]
    [ReadOnly] public Transform BarrierTransform;

    private void Start()
    {
        DrainageLayer.enabled = false;
    }

    public void PlaceDrainageLayer(bool enable)
    {
        DrainageLayer.enabled = enable;
    }

    public void PlaceSubstrateBarrier()
    {
        SubstrateBarrier.PlaceBarrier();
    }

    public void PlaceSubstrateLayer()
    {
        SubstrateLayer.CreateSubstrate();
    }

}