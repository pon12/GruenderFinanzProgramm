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
    // PRÜFUNG: Wenn das Tutorial läuft, KEINEN automatischen Start-Screen-Fade ausführen!
    bool tutorialAktiv = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialAktiv;

    if (!tutorialAktiv && startScreenObj != null && startScreenObj.activeSelf)
    {
        fadeCoroutine = StartCoroutine(FadeOutStartScreen());
    }

    // --- EVENT-VERBINDUNG ZUM REGISTRIERUNGSSCREEN ---
    if (registrationScreenObj != null)
    {
        RegestrierungLogik regSkript = registrationScreenObj.GetComponent<RegestrierungLogik>();

        if (regSkript != null)
        {
            regSkript.OnBackToLoginRequested += ShowLogin;

            regSkript.OnRegistrationSuccessful += (username) => 
            {
                Debug.Log($"MainMenuManager meldet: {username} hat sich erfolgreich registriert!");
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