using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    private GameObject mainMenuPanel;
    private GameObject[] userMenuPanels;
    private GameObject zukünftigeErweiterungenPanel;

    private void Start()
    {
        showMainMenu();
    }

    public void hideAllMenus()
    {
        mainMenuPanel.SetActive(false);

        // userMenuPanel.setActive(false);
        foreach (GameObject panel in userMenuPanels)
        {
            panel.SetActive(false);
        }

        zukünftigeErweiterungenPanel.SetActive(false);

    }

    public void showMainMenu()
    {
        hideAllMenus();
        mainMenuPanel.SetActive(true);
    }

    public void showUserMenu()
    {
        hideAllMenus();

    }

    public void ShowZukünftigeErweiterungen()
    {

    }

}
