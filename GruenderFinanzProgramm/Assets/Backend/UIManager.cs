
using UnityEngine;

public class UIManager : MonoBehaviour
{

    [SerializeField] private GameObject mainMenuPanel;

    //Variable für showUserPanel() -> Panel erstellen
    [SerializeField] private GameObject userMenuPanel;
    // [SerializeField] private GameObject zukuenftigeErweiterungenPanel;

    [SerializeField] private GameObject CompanyTestPanel;

    private void Start()
    {
        showMainMenu();
    }

    public void hideAllMenus()
    {
        mainMenuPanel.SetActive(false);
        userMenuPanel.SetActive(false);
        // zukuenftigeErweiterungenPanel.SetActive(false);
        CompanyTestPanel.SetActive(false);

    }

    public void showMainMenu()
    {
        hideAllMenus();
        mainMenuPanel.SetActive(true);
    }

    public void showUserMenu()
    {
        hideAllMenus();
        userMenuPanel.SetActive(true);
    }

    public void showCompanyTestPanel()
    {
        hideAllMenus();
        CompanyTestPanel.SetActive(true);
    }

    /*
    public void showZukuenftigeErweiterungen()
    {
        hideAllMenus();
        zukuenftigeErweiterungenPanel.SetActive(true);
    }
    */

}
