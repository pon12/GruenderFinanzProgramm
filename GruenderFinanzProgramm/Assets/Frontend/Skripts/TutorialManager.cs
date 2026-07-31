using UnityEngine;
using UnityEngine.UI;
public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial-Bilder")]
    [SerializeField] private Sprite[] tutorialBilder;
    [Header("UI-Referenzen")]
    [SerializeField] private GameObject startDialog;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Image tutorialBildAnzeige;
    [SerializeField] private Button jaButton;
    [SerializeField] private Button neinButton;
    [SerializeField] private Button weiterButton;
    [SerializeField] private Button zurueckButton;
    [Header("Unabhängiger Start-Button")]
    [SerializeField] private Button tutorialDirektButton;
    private int aktuellerIndex = 0;
    void Start()
    {
        // Start-Dialog anzeigen, Tutorial verstecken
        startDialog.SetActive(true);
        tutorialPanel.SetActive(false);
        // Button-Events registrieren
        jaButton.onClick.AddListener(TutorialStarten);
        neinButton.onClick.AddListener(TutorialSchliessen);
        weiterButton.onClick.AddListener(NaechstesBild);
        zurueckButton.onClick.AddListener(VorherigesBild);
        // Unabhängiger Button, um das Tutorial jederzeit direkt zu starten
        // ( in den Einstellungen platziert)
        if (tutorialDirektButton != null)
        {
            tutorialDirektButton.onClick.AddListener(TutorialDirektStarten);
        }
    }
    private void TutorialStarten()
    {
        startDialog.SetActive(false);
        tutorialPanel.SetActive(true);
        aktuellerIndex = 0;
        BildAktualisieren();
    }
    // Öffentliche Methode: startet das Tutorial unabhängig vom Start-Dialog.
    public void TutorialDirektStarten()
    {
        if (startDialog != null)
        {
            startDialog.SetActive(false);
        }
        tutorialPanel.SetActive(true);
        aktuellerIndex = 0;
        BildAktualisieren();
    }
    private void TutorialSchliessen()
    {
        startDialog.SetActive(false);
        tutorialPanel.SetActive(false);
    }
    private void NaechstesBild()
    {
        if (aktuellerIndex < tutorialBilder.Length - 1)
        {
            aktuellerIndex++;
            BildAktualisieren();
        }
        else
        {
            // Letztes Bild erreicht – Tutorial beenden
            TutorialSchliessen();
        }
    }
    private void VorherigesBild()
    {
        if (aktuellerIndex > 0)
        {
            aktuellerIndex--;
            BildAktualisieren();
        }
    }
    private void BildAktualisieren()
    {
        tutorialBildAnzeige.sprite = tutorialBilder[aktuellerIndex];
        // Zurück-Button nur anzeigen, wenn nicht beim ersten Bild
        zurueckButton.interactable = aktuellerIndex > 0;
        // Optional: Weiter-Button-Text ändern beim letzten Bild
        var weiterText = weiterButton.GetComponentInChildren<Text>();
        if (weiterText != null)
        {
            weiterText.text = aktuellerIndex >= tutorialBilder.Length - 1 ? "Fertig" : "Weiter";
        }
    }
}
