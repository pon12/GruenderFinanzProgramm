using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuManager : MonoBehaviour
{
    public GameObject startScreenObj;
    public GameObject loginScreenObj;
    public GameObject registrationScreenObj;

    void OnEnable()
    {
        SetupStartScreen();
    }

    // --- START SCREEN LOGIK ---
    void SetupStartScreen()
    {
        var root = startScreenObj.GetComponent<UIDocument>().rootVisualElement;
        root.RegisterCallback<PointerDownEvent>(evt => ShowLogin());
    }

    // --- LOGIN SCREEN LOGIK ---
    void SetupLoginButtons()
    {
        var root = loginScreenObj.GetComponent<UIDocument>().rootVisualElement;

        // Wir suchen die Buttons anhand der IDs aus image_e0a23b.png
        Button anmeldenBtn = root.Q<Button>("Anmelden");
        Button registrierenBtn = root.Q<Button>("KontoErstellen");

        if (anmeldenBtn != null)
            anmeldenBtn.clicked += () => Debug.Log("Login-Logik hier ausführen!");

        if (registrierenBtn != null)
            registrierenBtn.clicked += ShowRegistration;
    }

    // --- WECHSEL-FUNKTIONEN ---
    public void ShowLogin()
    {
        startScreenObj.SetActive(false);
        loginScreenObj.SetActive(true);
        registrationScreenObj.SetActive(false);
        
        // WICHTIG: Buttons registrieren, wenn der Screen aktiv wird
        SetupLoginButtons();
    }

    public void ShowRegistration()
    {
        loginScreenObj.SetActive(false);
        registrationScreenObj.SetActive(true);
    }
}