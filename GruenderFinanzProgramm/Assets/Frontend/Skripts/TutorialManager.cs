using UnityEngine;
using UnityEngine.UIElements;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // Key wird pro Nutzer geprefixed, damit jeder neue Nutzer
    // den StartDialog einmal sieht.
    private const string SchluesselSuffix = "_TutorialAngezeigt";

    [Header("Tutorial-Bilder")]
    [SerializeField] private Sprite[] tutorialBilderLang;
    [SerializeField] private Sprite[] tutorialBilderKurz;

    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement overlay;
    private VisualElement startDialog;
    private VisualElement tutorialPanel;
    private VisualElement tutorialBild;

    private Button langButton;
    private Button kurzButton;
    private Button keinTutorialButton;
    private Button weiterButton;
    private Button zurueckButton;
    private Button beendenButton;

    private Sprite[] aktiveTutorialBilder;
    private int aktuellerIndex;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private VisualElement GetRoot()
{
    if (uiDocument == null)
        uiDocument = GetComponent<UIDocument>();
    return uiDocument?.rootVisualElement;
}

private void HoleElemente()
{
    var root = GetRoot();
    if (root == null) return;

    overlay            = root.Q<VisualElement>("TutorialOverlay");
    startDialog        = root.Q<VisualElement>("StartDialog");
    tutorialPanel      = root.Q<VisualElement>("TutorialPanel");
    tutorialBild       = root.Q<VisualElement>("TutorialBild");
    langButton         = root.Q<Button>("LangButton");
    kurzButton         = root.Q<Button>("KurzButton");
    keinTutorialButton = root.Q<Button>("KeinTutorialButton");
    weiterButton       = root.Q<Button>("WeiterButton");
    zurueckButton      = root.Q<Button>("ZurueckButton");
    beendenButton      = root.Q<Button>("BeendenButton");
}

    private void Start()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[TutorialManager] Kein UIDocument zugewiesen.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        overlay            = root.Q<VisualElement>("TutorialOverlay");
        startDialog        = root.Q<VisualElement>("StartDialog");
        tutorialPanel      = root.Q<VisualElement>("TutorialPanel");
        tutorialBild       = root.Q<VisualElement>("TutorialBild");
        langButton         = root.Q<Button>("LangButton");
        kurzButton         = root.Q<Button>("KurzButton");
        keinTutorialButton = root.Q<Button>("KeinTutorialButton");
        weiterButton       = root.Q<Button>("WeiterButton");
        zurueckButton      = root.Q<Button>("ZurueckButton");
        beendenButton      = root.Q<Button>("BeendenButton");

        // Alles erstmal komplett ausblenden – kein Block, keine Abdunklung.
        SetzeVisible(overlay, false);
        SetzeVisible(startDialog, false);
        SetzeVisible(tutorialPanel, false);

        langButton.clicked         += LangesTutorialStarten;
        kurzButton.clicked         += KurzesTutorialStarten;
        keinTutorialButton.clicked += AuswahlSchliessen;
        weiterButton.clicked       += NaechstesBild;
        zurueckButton.clicked      += VorherigesBild;
        beendenButton.clicked      += TutorialSchliessen;
    }

    private void OnDestroy()
    {
        if (langButton         != null) langButton.clicked         -= LangesTutorialStarten;
        if (kurzButton         != null) kurzButton.clicked         -= KurzesTutorialStarten;
        if (keinTutorialButton != null) keinTutorialButton.clicked -= AuswahlSchliessen;
        if (weiterButton       != null) weiterButton.clicked       -= NaechstesBild;
        if (zurueckButton      != null) zurueckButton.clicked      -= VorherigesBild;
        if (beendenButton      != null) beendenButton.clicked      -= TutorialSchliessen;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// Beim Login aufrufen: TutorialManager.Instance.PruefeErstenStart(nutzername);
    /// Zeigt den StartDialog nur wenn dieser Nutzer ihn noch nie gesehen hat.
    public void PruefeErstenStart(string nutzername)
    {
        string schluessel = nutzername + SchluesselSuffix;
        bool bereitsAngezeigt = PlayerPrefs.GetInt(schluessel, 0) == 1;

        if (!bereitsAngezeigt)
        {
            PlayerPrefs.SetInt(schluessel, 1);
            PlayerPrefs.Save();
            HoleElemente();
            TutorialAuswahlOeffnen();
        }
    }

    /// Aus Einstellungen.cs aufrufen:
    /// TutorialManager.Instance.TutorialAuswahlOeffnen();
    public void TutorialAuswahlOeffnen()
    {
        HoleElemente();
        SetzeVisible(tutorialPanel, false);
        SetzeVisible(startDialog, true);
        SetzeVisible(overlay, true);
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void LangesTutorialStarten()  => TutorialOeffnen(tutorialBilderLang);
    private void KurzesTutorialStarten()  => TutorialOeffnen(tutorialBilderKurz);

    private void TutorialOeffnen(Sprite[] bilder)
    {
        if (bilder == null || bilder.Length == 0)
        {
            Debug.LogWarning("[TutorialManager] Keine Bilder eingetragen.");
            return;
        }

        aktiveTutorialBilder = bilder;
        aktuellerIndex = 0;

        SetzeVisible(startDialog, false);
        SetzeVisible(tutorialPanel, true);
        SetzeVisible(overlay, true);

        BildAktualisieren();
    }

    private void AuswahlSchliessen()
    {
        SetzeVisible(startDialog, false);
        SetzeVisible(tutorialPanel, false);
        SetzeVisible(overlay, false);
    }

    private void TutorialSchliessen()
    {
        SetzeVisible(tutorialPanel, false);
        SetzeVisible(overlay, false);
        aktiveTutorialBilder = null;
        aktuellerIndex = 0;
    }

    private void NaechstesBild()
    {
        if (aktiveTutorialBilder == null || aktiveTutorialBilder.Length == 0)
            return;

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
        if (aktiveTutorialBilder == null || aktiveTutorialBilder.Length == 0)
            return;

        if (aktuellerIndex > 0)
        {
            aktuellerIndex--;
            BildAktualisieren();
        }
    }

    private void BildAktualisieren()
    {
        tutorialBild.style.backgroundImage =
            new StyleBackground(aktiveTutorialBilder[aktuellerIndex]);

        zurueckButton.SetEnabled(aktuellerIndex > 0);

        bool istLetztes = aktuellerIndex == aktiveTutorialBilder.Length - 1;
        weiterButton.text = istLetztes ? "Fertig" : "Weiter ›";
    }

    private static void SetzeVisible(VisualElement element, bool sichtbar)
    {
        element.style.display = sichtbar ? DisplayStyle.Flex : DisplayStyle.None;
    }

    [ContextMenu("Ersten Start zurücksetzen")]
    private void ErstenStartZuruecksetzen()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[TutorialManager] Alle Tutorial-Keys zurückgesetzt.");
    }
}