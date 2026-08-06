using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    // Singleton, damit UIToolkit-Screens (z.B. Einstellungen.cs) das
    // Tutorial per Code auslösen können, ohne eine Inspector-Referenz zu
    // brauchen (Einstellungen nutzt UIToolkit-Buttons, keine uGUI-Buttons,
    // die man hier im Inspector verdrahten könnte).
    public static TutorialManager Instance { get; private set; }

    private const string ErsterStartSchluessel =
        "TutorialStartDialogAngezeigt";

    [Header("Tutorial-Bilder")]
    [SerializeField] private Sprite[] tutorialBilderLang;
    [SerializeField] private Sprite[] tutorialBilderKurz;

    [Header("UI-Referenzen")]
    [SerializeField] private GameObject startDialog;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Image tutorialBildAnzeige;

    [Header("Tutorial-Auswahl")]
    [SerializeField] private Button langButton;
    [SerializeField] private Button kurzButton;
    [SerializeField] private Button keinTutorialButton;

    [Header("Navigation")]
    [SerializeField] private Button weiterButton;
    [SerializeField] private Button zurueckButton;

    [Header("Tutorial-Button in den Einstellungen")]
    [SerializeField] private Button tutorialDirektButton;

    private Sprite[] aktiveTutorialBilder;
    private int aktuellerIndex;

    private void Start()
    {
        Instance = this;
        tutorialPanel.SetActive(false);

        // Nur beim ersten Start automatisch anzeigen.
        bool auswahlBereitsAngezeigt =
            PlayerPrefs.GetInt(ErsterStartSchluessel, 0) == 1;

        startDialog.SetActive(!auswahlBereitsAngezeigt);

        if (!auswahlBereitsAngezeigt)
        {
            PlayerPrefs.SetInt(ErsterStartSchluessel, 1);
            PlayerPrefs.Save();
        }

        langButton.onClick.AddListener(LangesTutorialStarten);
        kurzButton.onClick.AddListener(KurzesTutorialStarten);
        keinTutorialButton.onClick.AddListener(AuswahlSchliessen);

        weiterButton.onClick.AddListener(NaechstesBild);
        zurueckButton.onClick.AddListener(VorherigesBild);

        if (tutorialDirektButton != null)
        {
            // Zeigt zuerst die Auswahl, statt direkt das Tutorial zu starten.
            tutorialDirektButton.onClick.AddListener(
                TutorialAuswahlOeffnen
            );
        }
    }

    /// Wird vom Tutorial-Button in den Einstellungen aufgerufen.
    public void TutorialAuswahlOeffnen()
    {
        tutorialPanel.SetActive(false);
        startDialog.SetActive(true);
    }

    private void LangesTutorialStarten()
    {
        TutorialOeffnen(tutorialBilderLang);
    }

    private void KurzesTutorialStarten()
    {
        TutorialOeffnen(tutorialBilderKurz);
    }

    private void TutorialOeffnen(Sprite[] bilder)
    {
        if (bilder == null || bilder.Length == 0)
        {
            Debug.LogWarning(
                "Für diese Tutorial-Variante wurden keine Bilder eingetragen."
            );
            return;
        }

        aktiveTutorialBilder = bilder;
        aktuellerIndex = 0;

        startDialog.SetActive(false);
        tutorialPanel.SetActive(true);

        BildAktualisieren();
    }

    private void AuswahlSchliessen()
    {
        startDialog.SetActive(false);
        tutorialPanel.SetActive(false);
    }

    private void NaechstesBild()
    {
        if (aktiveTutorialBilder == null ||
            aktiveTutorialBilder.Length == 0)
        {
            return;
        }

        if (aktuellerIndex < aktiveTutorialBilder.Length - 1)
        {
            aktuellerIndex++;
            BildAktualisieren();
        }
        else
        {
            TutorialSchliessen();
        }
    }

    private void VorherigesBild()
    {
        if (aktiveTutorialBilder == null ||
            aktiveTutorialBilder.Length == 0)
        {
            return;
        }

        if (aktuellerIndex > 0)
        {
            aktuellerIndex--;
            BildAktualisieren();
        }
    }

    private void BildAktualisieren()
    {
        if (aktiveTutorialBilder == null ||
            aktiveTutorialBilder.Length == 0)
        {
            return;
        }

        tutorialBildAnzeige.sprite =
            aktiveTutorialBilder[aktuellerIndex];

        zurueckButton.interactable = aktuellerIndex > 0;

        Text weiterText =
            weiterButton.GetComponentInChildren<Text>();

        if (weiterText != null)
        {
            bool istLetztesBild =
                aktuellerIndex == aktiveTutorialBilder.Length - 1;

            weiterText.text = istLetztesBild ? "Fertig" : "Weiter";
        }
    }

    private void TutorialSchliessen()
    {
        tutorialPanel.SetActive(false);
        aktiveTutorialBilder = null;
        aktuellerIndex = 0;
    }

    private void OnDestroy()
    {
        langButton.onClick.RemoveListener(LangesTutorialStarten);
        kurzButton.onClick.RemoveListener(KurzesTutorialStarten);
        keinTutorialButton.onClick.RemoveListener(AuswahlSchliessen);

        weiterButton.onClick.RemoveListener(NaechstesBild);
        zurueckButton.onClick.RemoveListener(VorherigesBild);

        if (tutorialDirektButton != null)
        {
            tutorialDirektButton.onClick.RemoveListener(
                TutorialAuswahlOeffnen
            );
        }
    }

    [ContextMenu("Ersten Start zurücksetzen")]
    private void ErstenStartZuruecksetzen()
    {
        PlayerPrefs.DeleteKey(ErsterStartSchluessel);
        PlayerPrefs.Save();

        Debug.Log("Der Tutorial-Erststart wurde zurückgesetzt.");
    }
}
