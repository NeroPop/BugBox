using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JarManager jarManager;

    [Header("Panels")]
    [SerializeField] private GameObject StartPanel;
    [SerializeField] private GameObject DrainagePanel;


    private void Start()
    {
        StartPanel.SetActive(true);
        DrainagePanel.SetActive(false);
    }

    public void GameStart()
    {
        StartPanel.SetActive(false);
        DrainagePanel.SetActive(true);

        jarManager.PlaceDrainageLayer();
    }
}
