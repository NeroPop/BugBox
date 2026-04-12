using UnityEngine;

public class JarManager : MonoBehaviour
{
    [SerializeField] DrainageLayerPlacer DrainageLayer;
    [SerializeField] PlaceSubstrateBarrier SubstrateBarrier;

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
}