using UnityEngine;
using CustomAttributes;

public class JarManager : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] DrainageLayerPlacer DrainageLayer;
    [SerializeField] PlaceSubstrateBarrier SubstrateBarrier;
    [SerializeField] PlaceSubstrate SubstrateLayer;

    [Header("References")]
    [SerializeField] private UIController UIController;
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

    public void SubstrateBarrierSettled()
    {
        //SubstrateBarrier has finished settling, so we can now enable the substrate layer placement panel
        UIController.EnableSubstraitPanel();
    }

    public void PlaceSubstrateLayer()
    {
        SubstrateLayer.BarrierTransform = BarrierTransform;
        SubstrateLayer.CreateSubstrate();
    }

}