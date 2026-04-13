using System;
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
    [SerializeField] private GameObject SubstrateLayerPanel;

    [SerializeField] private bool DebugMode = false;

    private void Start()
    {
        if (!DebugMode)
        {
            //Ensures that only the start panel is active at the beginning of the game
            StartPanel.SetActive(true);
            DrainagePanel.SetActive(false);
            SubstrateBarrierPanel.SetActive(false);
            SubstrateLayerPanel.SetActive(false);
        }
    }

    public void GameStart()
    {
        //Enables the drainage layer placement panel and hides the start panel
        StartPanel.SetActive(false);
        DrainagePanel.SetActive(true);
        SubstrateBarrierPanel.SetActive(false);
        SubstrateLayerPanel.SetActive(false);

        //Calls the method in JarManager to enable the drainage layer placement script
        jarManager.PlaceDrainageLayer(true);
    }

    public void DrainageDone()
    {
        //Enables the substrate barrier placement panel and hides the drainage panel
        StartPanel.SetActive(false);
        DrainagePanel.SetActive(false);
        SubstrateBarrierPanel.SetActive(true);
        SubstrateLayerPanel.SetActive(false);

        //Calls the method in JarManager to disable the drainage layer placement script
        jarManager.PlaceDrainageLayer(false);
    }

    public void AddSubstraitBarrier()
    {
        //Removes all panels from the screen
        StartPanel.SetActive(false);
        DrainagePanel.SetActive(false);
        SubstrateBarrierPanel.SetActive(false);
        SubstrateLayerPanel.SetActive(false);

        //Calls the method in JarManager to place the substrate barrier
        jarManager.PlaceSubstrateBarrier();
    }

    public void EnableSubstraitPanel()
    {
        SubstrateLayerPanel.SetActive(true);
    }

    public void AddSubstrate()
    {
        //Removes all panels from the screen
        StartPanel.SetActive(false);
        DrainagePanel.SetActive(false);
        SubstrateBarrierPanel.SetActive(false);
        SubstrateLayerPanel.SetActive(false);

        //Calls the method in JarManager to place the substrate layer
        jarManager.PlaceSubstrateLayer();
    }
}
