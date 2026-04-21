
using UnityEngine;

public class UIManager : MonoBehaviour
{

    [SerializeField] private GameObject mainMenuPanel;

    //Variable für showUserPanel() -> Panel erstellen
    [SerializeField] private GameObject userMenuPanel;
    [SerializeField] private GameObject zukuenftigeErweiterungenPanel;

    private void Start()
    {
        showMainMenu();
    }

    public void hideAllMenus()
    {
        mainMenuPanel.SetActive(false);
        userMenuPanel.SetActive(false);
        zukuenftigeErweiterungenPanel.SetActive(false);

    }

    public void showMainMenu()
    {
        hideAllMenus();
        mainMenuPanel.SetActive(true);
    }

    public void showUserMenu()
    {
        //Alle User Panel anzeigen
        hideAllMenus();
        userMenuPanel.SetActive(true);

    }

    public void showZukuenftigeErweiterungen()
    {
        hideAllMenus();
        zukuenftigeErweiterungenPanel.SetActive(true);
    }

}
