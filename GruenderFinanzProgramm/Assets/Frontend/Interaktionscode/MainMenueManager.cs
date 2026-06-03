using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    public GameObject startScreenObj;
    public GameObject loginScreenObj;
    public GameObject registrationScreenObj;

    private Coroutine fadeCoroutine; 

    void OnEnable()
    {
        SetupStartScreen();
    }

    void Start()
    {
        // 1. Der alte Start-Screen-Fade-Code:
        if (startScreenObj != null && startScreenObj.activeSelf)
        {
            fadeCoroutine = StartCoroutine(FadeOutStartScreen());
        }

        // --- HIER IST NEU: DIE EVENT-VERBINDUNG ZUM REGISTRIERUNGSSCREEN ---
        if (registrationScreenObj != null)
        {
            // Wir holen uns die Logik-Komponente vom Registrierungs-Objekt
            RegestrierungLogik regSkript = registrationScreenObj.GetComponent<RegestrierungLogik>();

            if (regSkript != null)
            {
                // Wenn im Reg-Skript das Back-Event gefeuert wird, rufen wir hier "ShowLogin()" auf
                regSkript.OnBackToLoginRequested += ShowLogin;

                // Wenn die Registrierung erfolgreich war, fangen wir das hier ab
                regSkript.OnRegistrationSuccessful += (username) => 
                {
                    Debug.Log($"MainMenuManager meldet: {username} hat sich erfolgreich registriert!");
                    // Hier könntest du später z.B. automatisch zum Hauptmenü weiterleiten
                };
            }
        }
    }

    // --- START SCREEN LOGIK ---
    void SetupStartScreen()
    {
        var root = startScreenObj.GetComponent<UIDocument>().rootVisualElement;
        root.RegisterCallback<PointerDownEvent>(evt => ShowLogin());
    }

    private IEnumerator FadeOutStartScreen()
    {
        yield return new WaitForSeconds(3f);

        var startDoc = startScreenObj.GetComponent<UIDocument>();
        var loginDoc = loginScreenObj.GetComponent<UIDocument>();

        if (startDoc != null) startDoc.sortingOrder = 10;
        if (loginDoc != null) loginDoc.sortingOrder = 5;

        if (loginScreenObj != null)
        {
            loginScreenObj.SetActive(true);
            SetupLoginButtons();
        }
        if (registrationScreenObj != null)
        {
            registrationScreenObj.SetActive(false);
        }

        var root = startScreenObj.GetComponent<UIDocument>().rootVisualElement;
        
        float fadeDuration = 1.5f; 
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            root.style.opacity = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null; 
        }

        ShowLogin();
    }

    // --- LOGIN SCREEN LOGIK ---
    void SetupLoginButtons()
    {
        var root = loginScreenObj.GetComponent<UIDocument>().rootVisualElement;

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
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        var startRoot = startScreenObj.GetComponent<UIDocument>().rootVisualElement;
        if (startRoot != null) startRoot.style.opacity = 1f;

        startScreenObj.SetActive(false);
        loginScreenObj.SetActive(true);
        registrationScreenObj.SetActive(false);
        
        SetupLoginButtons();
    }

    public void ShowRegistration()
    {
        loginScreenObj.SetActive(false);
        registrationScreenObj.SetActive(true);
    }
}