using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JarManager jarManager;

    [Header("Panels")]
    [SerializeField] private GameObject StartPanel;
    [SerializeField] private GameObject DrainagePanel;
    [SerializeField] private GameObject SubstrateBarrierPanel;

    private void Start()
    {
        //Ensures that only the start panel is active at the beginning of the game
        StartPanel.SetActive(true);
        DrainagePanel.SetActive(false);
        SubstrateBarrierPanel.SetActive(false);
    }

    public void GameStart()
    {
        //Enables the drainage layer placement panel and hides the start panel
        StartPanel.SetActive(false);
        DrainagePanel.SetActive(true);
        SubstrateBarrierPanel.SetActive(false);

        //Calls the method in JarManager to enable the drainage layer placement script
        jarManager.PlaceDrainageLayer(true);
    }

    public void DrainageDone()
    {
        //Enables the substrate barrier placement panel and hides the drainage panel
        StartPanel.SetActive(false);
        DrainagePanel.SetActive(false);
        SubstrateBarrierPanel.SetActive(true);

        //Calls the method in JarManager to disable the drainage layer placement script
        jarManager.PlaceDrainageLayer(false);
    }

    public void AddSubstraitBarrier()
    {
        //Removes all panels from the screen
        StartPanel.SetActive(false);
        DrainagePanel.SetActive(false);
        SubstrateBarrierPanel.SetActive(false);

        //Calls the method in JarManager to place the substrate barrier
        jarManager.PlaceSubstrateBarrier();
    }
}
