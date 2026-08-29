using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

// ── Datenstruktur für einen Tutorial-Schritt ──────────────────────────────────
[System.Serializable]
public class TutorialSchritt
{
    [TextArea(2, 5)]
    [Tooltip("Erklärungstext der dem Nutzer angezeigt wird.")]
    public string Erklaerung;

    [Tooltip("name=\"...\" des Elements im UXML das hervorgehoben werden soll. " +
             "Leer lassen für allgemeine Beschreibung ohne Highlight.")]
    public string ElementName;

    [Tooltip("Wird dieser Schritt auch in der Kurzversion angezeigt?")]
    public bool inKurzversion;

    [Tooltip("Scene die geladen wird. Leer = aktuelle Scene behalten.")]
    public string SceneName;

    [Tooltip("Welches GameObject soll in 3-Start-Screens aktiviert werden? Leer = nichts umschalten.")]
    public string GameObjectName;

    [Tooltip("Falls ein VisualElement-Popup im UI Document eingeblendet werden muss.")]
    public string PopupElementName;

    [Tooltip("Der Name der ScrollView im UXML, falls gescrollt werden muss.")]
    public string ScrollViewName;
}

// ── TutorialManager ──────────────────────────────────────────────────────────
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string SchluesselSuffix = "_TutorialAngezeigt";

    public bool IsTutorialAktiv => tutorialModus != null && tutorialModus.style.display == DisplayStyle.Flex;

    private string _zielSceneNachTutorial = null;

    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Settings")]
    [SerializeField] private float buttonCooldownSekunden = 0.5f;

    [Header("Tutorial-Schritte (Reihenfolge = Anzeigereihenfolge)")]
    [SerializeField] private List<TutorialSchritt> alleSchritte = new();

    [Header("Start Screens (Falls in derselben Szene)")]
    [SerializeField] private GameObject startScreenObj;
    [SerializeField] private GameObject loginScreenObj;
    [SerializeField] private GameObject registrationScreenObj;
    [SerializeField] private GameObject settingsScreenObj;

    // ── UI-Elemente ──────────────────────────────────────────────────────────
    private VisualElement overlay;
    private VisualElement startDialogWrapper;
    private VisualElement tutorialModus;
    private VisualElement highlightRahmen;
    private VisualElement erklaerungsBox;
    private Label         erklaerungsText;
    private Label         schrittAnzeige;
    private Button        zurueckButton;
    private Button        weiterButton;
    private Button        beendenButton;
    private Button        langButton;
    private Button        kurzButton;
    private Button        keinTutorialButton;

    private System.Action _onLang;
    private System.Action _onKurz;

    private List<TutorialSchritt> aktiveSchritte = new();
    private int aktuellerIndex = 0;
    private Coroutine _scrollCoroutine;
    private Coroutine _sceneCoroutine;

    private bool _isTransitioning = false;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        HoleElemente();

        SetzeVisible(overlay,            false);
        SetzeVisible(startDialogWrapper, false);
        SetzeVisible(tutorialModus,      false);
    }

    private void HoleElemente()
    {
        var root = uiDocument?.rootVisualElement;
        if (root == null) return;

        overlay             = root.Q<VisualElement>("TutorialOverlay");
        startDialogWrapper  = root.Q<VisualElement>("StartDialogWrapper");
        tutorialModus       = root.Q<VisualElement>("TutorialModus");
        highlightRahmen     = root.Q<VisualElement>("HighlightRahmen");
        erklaerungsBox      = root.Q<VisualElement>("ErklaerungsBox");
        erklaerungsText     = root.Q<Label>("ErklaerungsText");
        schrittAnzeige      = root.Q<Label>("SchrittAnzeige");
        zurueckButton       = root.Q<Button>("ZurueckButton");
        weiterButton        = root.Q<Button>("WeiterButton");
        beendenButton       = root.Q<Button>("BeendenButton");
        langButton          = root.Q<Button>("LangButton");
        kurzButton          = root.Q<Button>("KurzButton");
        keinTutorialButton  = root.Q<Button>("KeinTutorialButton");

        _onLang = () => TutorialStarten(lang: true);
        _onKurz = () => TutorialStarten(lang: false);

        langButton.clicked         += _onLang;
        kurzButton.clicked         += _onKurz;
        keinTutorialButton.clicked += AuswahlSchliessen;
        weiterButton.clicked       += NaechsterSchritt;
        zurueckButton.clicked      += VorherigerSchritt;
        beendenButton.clicked      += TutorialSchliessen;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public bool PruefeErstenStart(string nutzername, string zielScene = "Dashboard")
    {
        string schluessel = nutzername + SchluesselSuffix;
        if (PlayerPrefs.GetInt(schluessel, 0) == 1) return false;

        PlayerPrefs.SetInt(schluessel, 1);
        PlayerPrefs.Save();
        _zielSceneNachTutorial = zielScene;
        TutorialAuswahlOeffnen();
        return true;
    }

    public void TutorialAuswahlOeffnen()
    {
        SetzeVisible(tutorialModus,      false);
        SetzeVisible(startDialogWrapper, true);
        SetzeVisible(overlay,            true);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void TutorialStarten(bool lang)
    {
        aktiveSchritte = lang
            ? new List<TutorialSchritt>(alleSchritte)
            : alleSchritte.FindAll(s => s.inKurzversion);

        if (aktiveSchritte.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] Keine Schritte für diese Variante.");
            AuswahlSchliessen();
            return;
        }

        StopAllCoroutines();
        _scrollCoroutine = null;
        _sceneCoroutine  = null;
        aktuellerIndex = 0;
        _isTransitioning = false;

        SetzeVisible(startDialogWrapper, false);
        SetzeVisible(tutorialModus,      true);
        StarteSchrittCoroutine();
    }

    private void AuswahlSchliessen()
    {
        SetzeVisible(startDialogWrapper, false);
        SetzeVisible(overlay,            false);
    }

    private void StarteSchrittCoroutine()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        SetzeButtonsAktiv(false);

        if (_scrollCoroutine != null) { StopCoroutine(_scrollCoroutine); _scrollCoroutine = null; }
        if (_sceneCoroutine  != null) { StopCoroutine(_sceneCoroutine);  _sceneCoroutine  = null; }
        _sceneCoroutine = StartCoroutine(LadeSceneUndZeigeSchritt());
    }

    private void NaechsterSchritt()
    {
        if (_isTransitioning) return;

        if (aktuellerIndex < aktiveSchritte.Count - 1)
        {
            aktuellerIndex++;
            StarteSchrittCoroutine();
        }
        else
        {
            TutorialSchliessen();
        }
    }

    private void VorherigerSchritt()
    {
        if (_isTransitioning) return;

        if (aktuellerIndex > 0)
        {
            aktuellerIndex--;
            StarteSchrittCoroutine();
        }
    }

    private IEnumerator LadeSceneUndZeigeSchritt()
    {
        var schritt      = aktiveSchritte[aktuellerIndex];
        string zielScene = schritt.SceneName;
        string aktScene  = SceneManager.GetActiveScene().name;

        SetzeButtonsAktiv(false);
        SchliesseAllePopups();

        if (!string.IsNullOrEmpty(zielScene) && zielScene != aktScene)
        {
            yield return SceneManager.LoadSceneAsync(zielScene);
            yield return null;
        }

        if (!string.IsNullOrEmpty(schritt.GameObjectName))
        {
            var mainMenu = Object.FindFirstObjectByType<MainMenuManager>();
            if (mainMenu != null) mainMenu.StopAllCoroutines();

            GameObject zielObj = null;
            if (startScreenObj != null && startScreenObj.name == schritt.GameObjectName) zielObj = startScreenObj;
            else if (loginScreenObj != null && loginScreenObj.name == schritt.GameObjectName) zielObj = loginScreenObj;
            else if (registrationScreenObj != null && registrationScreenObj.name == schritt.GameObjectName) zielObj = registrationScreenObj;

            if (zielObj == null) zielObj = FindInaktivesGameObject(schritt.GameObjectName);

            if (zielObj != null)
            {
                if (startScreenObj != null) startScreenObj.SetActive(false);
                if (loginScreenObj != null) loginScreenObj.SetActive(false);
                if (registrationScreenObj != null) registrationScreenObj.SetActive(false);

                zielObj.SetActive(true);
                yield return null;
            }
        }

        if (!string.IsNullOrEmpty(schritt.PopupElementName))
        {
            ZeigePopupVisualElement(schritt.PopupElementName);
            yield return null;
        }

        SchrittAnzeigen();

        yield return null;
        yield return null;

        ZeigeElementMitScroll(schritt.ScrollViewName, schritt.ElementName);
    }

    private void ZeigeElementMitScroll(string scrollViewName, string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
        {
            SetzeVisible(highlightRahmen, false);
            Freigeben();
            return;
        }

        VisualElement targetElement = null;
        ScrollView scrollView = null;

        foreach (var doc in FindObjectsByType<UIDocument>())
        {
            if (doc == uiDocument) continue;
            var root = doc.rootVisualElement;
            if (root == null) continue;

            if (targetElement == null)
                targetElement = root.Q<VisualElement>(elementName);

            if (scrollView == null)
            {
                if (!string.IsNullOrEmpty(scrollViewName))
                    scrollView = root.Q<ScrollView>(scrollViewName);
                else if (targetElement != null)
                    scrollView = targetElement.GetFirstAncestorOfType<ScrollView>();
            }
        }

        if (targetElement == null)
        {
            SetzeVisible(highlightRahmen, false);
            Debug.LogWarning($"[TutorialManager] Element '{elementName}' nicht gefunden.");
            Freigeben();
            return;
        }

        if (_scrollCoroutine != null) StopCoroutine(_scrollCoroutine);
        _scrollCoroutine = StartCoroutine(ScrollUndHighlight(targetElement, scrollView));
    }

    private IEnumerator ScrollUndHighlight(VisualElement element, ScrollView scrollView)
    {
        for (int i = 0; i < 10; i++)
        {
            if (element.worldBound.height > 0) break;
            yield return null;
        }
        yield return null;

        Rect rect = element.worldBound;

        if (scrollView != null)
        {
            float altesOffset  = scrollView.scrollOffset.y;
            float neuesOffset  = rect.y - scrollView.contentContainer.worldBound.y;
            float maxOffset    = Mathf.Max(0, scrollView.contentContainer.layout.height - scrollView.layout.height);
            neuesOffset        = Mathf.Clamp(neuesOffset, 0, maxOffset);

            rect = new Rect(rect.x, rect.y + (altesOffset - neuesOffset), rect.width, rect.height);

            scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, neuesOffset);
        }

        SetzeVisible(highlightRahmen, true);
        HighlightPositionieren(rect, scrollView);
        
        Freigeben();
    }

    private void Freigeben()
    {
        StartCoroutine(FreigebenVerzoegert());
    }

    private IEnumerator FreigebenVerzoegert()
    {
        yield return new WaitForSeconds(buttonCooldownSekunden);

        _isTransitioning = false;
        SetzeButtonsAktiv(true);
    }

    private void SetzeButtonsAktiv(bool aktiv)
    {
        weiterButton?.SetEnabled(aktiv);
        zurueckButton?.SetEnabled(aktiv && aktuellerIndex > 0);
        beendenButton?.SetEnabled(aktiv);
    }

    private void SchrittAnzeigen()
    {
        var schritt = aktiveSchritte[aktuellerIndex];

        erklaerungsText.text = schritt.Erklaerung;
        schrittAnzeige.text  = $"{aktuellerIndex + 1} / {aktiveSchritte.Count}";
        zurueckButton.SetEnabled(aktuellerIndex > 0);
        weiterButton.text = aktuellerIndex == aktiveSchritte.Count - 1
            ? "Fertig"
            : "Weiter ›";

        SetzeVisible(highlightRahmen, false);
    }

    private void HighlightPositionieren(Rect r, ScrollView scrollView = null)
    {
        const float Padding = 8f;

        if (scrollView != null)
        {
            var sv     = scrollView.worldBound;
            float x      = Mathf.Max(r.x,    sv.x);
            float y      = Mathf.Max(r.y,    sv.y);
            float right  = Mathf.Min(r.xMax, sv.xMax);
            float bottom = Mathf.Min(r.yMax, sv.yMax);
            r = new Rect(x, y, right - x, bottom - y);
        }

        highlightRahmen.style.left   = r.x      - Padding;
        highlightRahmen.style.top    = r.y      - Padding;
        highlightRahmen.style.width  = r.width  + Padding * 2;
        highlightRahmen.style.height = r.height + Padding * 2;
    }

    private void TutorialSchliessen()
    {
        _isTransitioning = false;

        SchliesseAllePopups();
        SetzeVisible(tutorialModus, false);
        SetzeVisible(overlay,       false);

        aktuellerIndex = 0;
        aktiveSchritte.Clear();

        if (_zielSceneNachTutorial != null)
        {
            string ziel = _zielSceneNachTutorial;
            _zielSceneNachTutorial = null;
            SceneManager.LoadScene(ziel);
        }
        else
        {
            string einstellungenSzeneName = "Einstellungen";
            if (SceneManager.GetActiveScene().name != einstellungenSzeneName)
                SceneManager.LoadScene(einstellungenSzeneName);
        }
    }

    private static void SetzeVisible(VisualElement el, bool sichtbar)
    {
        if (el == null) return;
        el.style.display = sichtbar ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (langButton         != null && _onLang != null) langButton.clicked         -= _onLang;
        if (kurzButton         != null && _onKurz != null) kurzButton.clicked         -= _onKurz;
        if (keinTutorialButton != null) keinTutorialButton.clicked -= AuswahlSchliessen;
        if (weiterButton       != null) weiterButton.clicked       -= NaechsterSchritt;
        if (zurueckButton      != null) zurueckButton.clicked      -= VorherigerSchritt;
        if (beendenButton      != null) beendenButton.clicked      -= TutorialSchliessen;
    }

    // ── Inspector-Helfer ─────────────────────────────────────────────────────

    [ContextMenu("Standardschritte befüllen")]
    private void StandardschritteBefllen()
    {
        alleSchritte = new List<TutorialSchritt>
        {
            new()
            {
                Erklaerung    = "Willkommen auf ihrer zentralen Steuerzentrale!",
                ElementName   = "",
                SceneName     = "Dashboard",
                inKurzversion = true
            },
            new()
            {
                Erklaerung    = "Hier sehen sie all ihre wichtigen Geschäftszahlen auf einen Blick – von der Anzahl ihrer Kunden bis hin zum aktuellen Kontostand. Über die direkten Buttons können sie ohne Umwege sofort neue Kunden anlegen, Angebote schreiben oder Rechnungen erstellen",
                ElementName   = "stats-grid",
                SceneName     = "Dashboard",
                inKurzversion = false
            },
            new()
            {
                Erklaerung    = "In diesem Bereich wird ihre finanzielle Entwicklung des gesamten Jahres übersichtlich als Diagramm aufbereitet. Auf der linken Seite sehen sie die Beträge, während die Achse unten ihre Umsätze von Januar bis Dezember abbildet. So erkennen sie auf einen Blick, in welchen Monaten ihr Geschäft besonders gut läuft.",
                ElementName   = "kassenbuch-statistik-panel",
                SceneName     = "Dashboard",
                inKurzversion = false
            },
            new()
            {
                Erklaerung    = "Mit dem Fristenkalender verpassen die garantiert keine wichtigen Termine oder steuerlichen Abgabefristen mehr. Navigieren sie einfach durch die Monate und Jahre, um ihre Fälligkeiten bequem im Voraus zu planen. Ein Klick auf den jeweiligen Tag zeigt ihnen sofort alle anstehenden Aufgaben an.",
                ElementName   = "kalender-panel",
                SceneName     = "Dashboard",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Auf der linken Seite können Sie wichtigen Daten für Sie und Ihr Unternehmen bearbeiten und unten Ihre aktuelle Version der App sehen.",
                ElementName   = "col-left",
                ScrollViewName  = "settings-scroll",
                SceneName     = "Einstellungen",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Auf der rechten Seite finden sie die Steuersätze ...",
                ElementName   = "card-steuersaetze",
                ScrollViewName  = "settings-scroll",
                SceneName     = "Einstellungen",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "... den Anzeigemodus und Ihren Persönlichen Begleiter, welchen sie ein und ausschalten können ...",
                ElementName   = "card-layout",
                ScrollViewName  = "settings-scroll",
                SceneName     = "Einstellungen",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "... und die Sicherheitseinstellungen, wo sie Ihren aktuellen Passkey zurücksetzen können.",
                ElementName   = "card-sicherheit",
                ScrollViewName  = "settings-scroll",
                SceneName     = "Einstellungen",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Ganz unten finden Sie den Tutorial-Button – Sie können diese Einführung jederzeit erneut starten.",
                ElementName   = "card-hilfe",
                ScrollViewName  = "settings-scroll",
                SceneName     = "Einstellungen",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier sehen sie all ihre Dokumente auf einen Blick.",
                ElementName   = "Main-Scroll-View",
                SceneName     = "Dokument-Screen",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Mit diesen zwei Buttons können Dukemente hinzugefügt oder gelöscht werden.",
                ElementName   = "row-alle-loeschen",
                SceneName     = "Dokument-Screen",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Auf diesem Screen verwaltest du alle Dienstleistungen, die du deinen Kunden anbietest. Du kannst neue Leistungen anlegen, bestehende bearbeiten oder löschen. Die hinterlegten Dienstleistungen stehen dir später beim Erstellen von Angeboten und Rechnungen direkt zur Auswahl, damit du sie nicht jedes Mal neu eingeben musst.",
                ElementName   = "",
                SceneName     = "Dienstleistungen",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier siehst du alle angelegten Dienstleistungen in einer übersichtlichen Liste. Jede Zeile zeigt dir den Namen, die Beschreibung, die Kategorie und den Preis der Leistung.",
                ElementName   = "tabelle-wrapper",
                SceneName     = "Dienstleistungen",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Mit diesem Button legst du eine neue Dienstleistung an. Es öffnet sich ein Formular, in dem du alle relevanten Felder ausfüllst.",
                ElementName   = "btn-neu",
                SceneName     = "Dienstleistungen",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Dieses Formular erscheint beim Anlegen und beim Bearbeiten einer Dienstleistung. Du gibst den Namen und eine optionale Beschreibung ein, wählst das Preismodell und trägst den Betrag ein. Pflichtfelder werden rot markiert, wenn sie fehlen oder ungültig sind. Mit Fertig speicherst du, mit Abbrechen verwirfst du die Änderungen.",
                ElementName   = "container-popup",
                PopupElementName = "popup-overlay",
                SceneName     = "Dienstleistungen",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Hier wählst du, wie du die Leistung abrechnest: als Festpreis, Stundensatz oder Pauschalpreis. Diese Angabe erscheint später auch in Angeboten und Rechnungen.",
                ElementName   = "feld-preismodell",
                PopupElementName = "popup-overlay",
                SceneName     = "Dienstleistungen",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Auf diesem Screen verwaltest du all deine Kunden und Kontakte sowie deine eigenen Firmendaten an einem zentralen Ort. Ganz oben findest du deine eigenen Profil- und Unternehmensdaten. Darunter befindet sich die Aktionsleiste, mit der du neue Kunden anlegen kannst, gefolgt von der Übersicht all deiner gespeicherten Kundenkontakte.",
                ElementName   = "",
                SceneName     = "KundenDB",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Im oberen Bereich siehst du die Karte „Lokaler Nutzer“. Hier werden deine eigenen Firmen- und Kontaktdaten (Name, Firma, Adresse, E-Mail und Telefonnummer) angezeigt, die aus deinen Einstellungen geladen werden. Über den Button „Ändern“ gelangst du direkt in die Einstellungen, um deine Unternehmensdaten anzupassen.",
                ElementName   = "card-Lokaler-Nutzer",
                SceneName     = "KundenDB",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "In der Zwischenleiste findest du den Button „Kunde Hinzufügen“. Damit öffnest du ein Formular, um einen neuen Kontakt anzulegen. Rechts daneben siehst du einen Zähler, der dir jederzeit die genaue Anzahl deiner aktuell gespeicherten Kunden anzeigt.",
                ElementName   = "card-Zwischenbar",
                SceneName     = "KundenDB",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Wenn du auf „Kunde Hinzufügen“ klickst, öffnet sich dieses Fenster. Trage hier Vor- und Nachnamen, Firma, Adresse, E-Mail sowie Telefonnummer inklusive Ländervorwahl ein. Klicke anschließend auf „Kunden speichern“, um den neuen Kontakt in der Datenbank abgelegen.",
                ElementName   = "Pop-up",
                PopupElementName = "PopUpKundeerstellen",
                SceneName     = "KundenDB",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier werden all deine angelegten Kunden in einer Übersicht aufgelistet. Jede Kundenkarte zeigt die wichtigsten Kontaktdaten wie Name, Firma, Adresse, E-Mail und Telefonnummer.",
                ElementName   = "kunden-liste-holder",
                SceneName     = "KundenDB",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Der Angebot-Screen zeigt dir alle Werkzeuge um professionelle Angebote zu erstellen.",
                ElementName   = "",
                SceneName     = "Angebot",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier wählst du den Kunden aus dem Ihr das Angebot erstellt oder trägst einen neuen ein.",
                ElementName   = "card-partner",
                SceneName     = "Angebot",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier pflegst du Angebotsnummer, Datum, Status und Währung.",
                ElementName   = "card-grunddaten",
                SceneName     = "Angebot",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Hier kannst du Dateien und Dokumente direkt an das Angebot anhängen.",
                ElementName   = "card-anhaenge",
                SceneName     = "Angebot",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Trage hier alle Artikel und Leistungen mit Menge und Preis ein.",
                ElementName   = "card-positionen",
                SceneName     = "Angebot",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier kannst du Rabatt, Skonto und interne Notizen zum Angebot hinterlegen.",
                ElementName   = "card-details",
                SceneName     = "Angebot",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Hier behältst du die Finanzen deines Unternehmens im Blick, erfasst Einnahmen und Ausgaben und bereitest deine Daten für die Buchhaltung vor.",
                ElementName   = "",
                SceneName     = "Kassenbuch",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Wähle oben rechts das gewünschte Jahr aus, um deine Einträge zu filtern. Die Anzeige zeigt dir deinen aktuellen Saldo, der aus Einnahmen abzüglich aller Ausgaben automatisch berechnet und live aktualisiert wird.",
                ElementName   = "dropJahr",
                SceneName     = "Kassenbuch",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Über die Buttons fügst du neue Ausgaben oder Einnahmen hinzu. Es öffnet sich ein Fenster, in dem du Betrag, Verwendungszweck, Art und Datum einträgst.",
                ElementName   = "Buttons",
                SceneName     = "Kassenbuch",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "In der zentralen Liste siehst du alle Transaktionen auf einen Blick (Einnahmen in Grün, Ausgaben in Rot). Klicke auf die Spaltenköpfe, um die Liste z. B. nach Datum, Betrag oder Typ zu sortieren. Bezahlte Rechnungen werden automatisch eingefügt.",
                ElementName   = "Tabelle",
                SceneName     = "Kassenbuch",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Nutze das Export-Menü, um Berichte wie die Einnahmen-Überschuss-Rechnung (EÜR), die GuV oder das Buchungsjournal direkt als PDF zu generieren.",
                ElementName   = "FinanzExport",
                SceneName     = "Kassenbuch",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Der Fortschritt-Screen gibt dir einen kombinierten Überblick über deine gesamte Gründungsreise. Er fasst zusammen, wie weit du im Gründerpfad bist und wie viele Pflichtdokumente du bereits ausgefüllt hast. So siehst du auf einen Blick, was insgesamt noch zu tun ist.",
                ElementName   = "",
                SceneName     = "Fortschritt",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier siehst du jede Gründungsphase einzeln mit einem Mini-Fortschrittsbalken und der Anzahl erledigter Schritte. Eine Phase zeigt ein grünes Häkchen, sobald alle Schritte abgeschlossen sind.",
                ElementName   = "panel-gruenderpfad",
                SceneName     = "Fortschritt",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Dieses Panel zeigt dir die nächsten offenen Aufgaben aus deinem Gründerpfad – immer die ersten fünf, die noch nicht erledigt sind. Du erledigst sie direkt im Gründerpfad-Screen.",
                ElementName   = "panel-naechste-schritte",
                SceneName     = "Fortschritt",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier erscheinen deine zuletzt freigeschalteten Erfolge aus dem Gründerpfad und aus ausgefüllten Dokumenten. Jeder abgeschlossene Schritt und jedes Pflichtdokument kann hier auftauchen.",
                ElementName   = "panel-letzte-erfolge",
                SceneName     = "Fortschritt",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Der Gründerpfad ist deine persönliche Roadmap von der Idee bis zur fertigen Gründung. Alle wichtigen Schritte sind in Phasen unterteilt – von der Vorbereitung über die Anmeldung bis hin zu Finanzen und Betrieb. Du hakst Schritte ab, die du erledigt hast, und siehst sofort wie weit du bist. Der Fortschritt hier fließt auch in den Fortschritt-Screen ein.",
                ElementName   = "",
                SceneName     = "Gründerpfad",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Dieser Balken zeigt dir auf einen Blick, wie viele Schritte des Gründerpfads du bereits abgehakt hast. Die Prozentzahl rechts aktualisiert sich automatisch jedes Mal, wenn du einen Schritt als erledigt markierst.",
                ElementName   = "panel-gesamtfortschritt",
                SceneName     = "Gründerpfad",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Hier siehst du alle Phasen deiner Gründung mit ihren einzelnen Schritten. Jede Phase lässt sich aufklappen. Ein Klick auf die Checkbox links neben einem Schritt markiert ihn als erledigt – das speichert sich automatisch.",
                ElementName   = "main-container",
                SceneName     = "Gründerpfad",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Die Wissensdatenbank ist deine Anlaufstelle für Anleitungen und Erklärungen rund um deine Gründung. Alle Einträge sind in Themenkategorien geordnet. Du kannst dir jeden Eintrag in Ruhe durchlesen und so gezielt Fragen zu Themen wie Rechtsformen, Steuern oder Buchhaltung nachschlagen.",
                ElementName   = "",
                SceneName     = "Wissensdatenbank",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Jede Kachel steht für eine Themenkategorie – zum Beispiel Recht, Finanzen oder Marketing. Ein Klick auf eine Kachel öffnet die Liste aller Einträge zu diesem Thema.",
                ElementName   = "Grid-Container",
                SceneName     = "Wissensdatenbank",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Nach dem Klick auf eine Kategorie öffnet sich dieses Popup mit allen Wissenseinträgen zu dem Thema. Klicke auf einen einzelnen Eintrag, um ihn vollständig zu lesen.",
                ElementName   = "Detail-Menu-Container",
                PopupElementName = "Detail-Popup-Overlay",
                SceneName     = "Wissensdatenbank",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Hier siehst du den vollständigen Text des ausgewählten Eintrags. Du kannst scrollen, wenn der Inhalt länger ist. Mit Schließen kommst du zurück zur Kategorieübersicht.",
                ElementName   = "Detail-List-Container",
                PopupElementName = "Detail-Popup-Overlay",
                SceneName     = "Wissensdatenbank",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Der Erfolge-Screen zeigt dir alle Meilensteine und Errungenschaften, die du in Ventoriq sammeln kannst. Erfolge werden automatisch freigeschaltet – durch abgehakte Schritte im Gründerpfad, ausgefüllte Dokumente, angelegte Kunden, erstellte Angebote und vieles mehr. Es gibt einmalige Erfolge und stufenbasierte Erfolge mit Bronze-, Silber-, Gold- und Platin-Level.",
                ElementName   = "",
                SceneName     = "Erfolge",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Dieser Balken zeigt, wie viele der verfügbaren Erfolge du bereits freigeschaltet hast. Die Zahl rechts gibt dir den genauen Stand an.",
                ElementName   = "panel-gesamtfortschritt",
                SceneName     = "Erfolge",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Alle Erfolge sind hier nach Kategorien gruppiert – zum Beispiel Gründung, Dokumente, Kunden oder Finanzen. Freigeschaltete Erfolge leuchten auf, gesperrte erscheinen ausgegraut mit einem Schloss-Symbol. Bei stufenbasierten Erfolgen siehst du direkt, auf welcher Stufe du gerade bist.",
                ElementName   = "erfolge-grid",
                SceneName     = "Erfolge",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier verwaltest du zentral alle Angebote und Rechnungen deines Unternehmens, behältst Fristen sowie Status im Blick und exportierst deine Belege schnell als PDF.",
                ElementName   = "",
                SceneName     = "Buchhaltung",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Die Kopfzeile bietet dir den Einstieg in die Übersicht. Durch Klick auf die Spaltenköpfe (z. B. Bezeichnung, Art, Erstellt, Fällig, Status) kannst du die gesamte Liste nach deinen Wünschen sortieren",
                ElementName   = "tabelle-header",
                SceneName     = "Buchhaltung",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Über das Dropdown-Menü in jeder Zeile kannst du den Bearbeitungsstand eines Belegs (z. B. Entwurf, Versendet, Bezahlt oder Storniert) direkt anpassen und aktualisieren.",
                ElementName   = "",
                SceneName     = "Buchhaltung",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Über das Dropdown-Menü in jeder Zeile kannst du den Bearbeitungsstand eines Belegs (z. B. Entwurf, Versendet, Bezahlt oder Storniert) direkt anpassen und aktualisieren.",
                ElementName   = "",
                SceneName     = "Buchhaltung",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Über das Export-Symbol erstellst du im Handumdrehen ein PDF deines Belegs. Das Ordner-Symbol führt dich direkt zum lokalen Speicherort auf deinem System.",
                ElementName   = "",
                SceneName     = "Buchhaltung",
                inKurzversion = true
            },
        };

        Debug.Log($"[TutorialManager] {alleSchritte.Count} Standardschritte eingetragen.");
    }

    [ContextMenu("Ersten Start zurücksetzen")]
    private void ErstenStartZuruecksetzen()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[TutorialManager] Alle Tutorial-Keys zurückgesetzt.");
    }

    private GameObject FindInaktivesGameObject(string objName)
    {
        var obj = GameObject.Find(objName);
        if (obj != null) return obj;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var target = FindChildRecursive(root.transform, objName);
                if (target != null) return target;
            }
        }

        return null;
    }

    private GameObject FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent.gameObject;

        foreach (Transform child in parent)
        {
            var result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void ZeigePopupVisualElement(string popupName)
    {
        var alleDocuments = FindObjectsByType<UIDocument>();
        foreach (var doc in alleDocuments)
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;

            var popupEl = root.Q<VisualElement>(popupName);
            if (popupEl != null)
            {
                popupEl.style.display = DisplayStyle.Flex;
                popupEl.style.position = Position.Absolute;
                return;
            }
        }
        Debug.LogWarning($"[TutorialManager] UI Element Popup '{popupName}' wurde nicht gefunden.");
    }

    private void SchliesseAllePopups()
    {
        foreach (var schritt in alleSchritte)
        {
            if (!string.IsNullOrEmpty(schritt.PopupElementName))
            {
                VersteckePopupVisualElement(schritt.PopupElementName);
            }
        }

        foreach (var schritt in alleSchritte)
        {
            if (!string.IsNullOrEmpty(schritt.GameObjectName))
            {
                if ((startScreenObj != null && schritt.GameObjectName == startScreenObj.name) ||
                    (loginScreenObj != null && schritt.GameObjectName == loginScreenObj.name) ||
                    (registrationScreenObj != null && schritt.GameObjectName == registrationScreenObj.name))
                {
                    continue;
                }

                var go = FindInaktivesGameObject(schritt.GameObjectName);
                if (go != null)
                {
                    go.SetActive(false);
                }
            }
        }
    }

    private void VersteckePopupVisualElement(string popupName)
    {
        var alleDocuments = FindObjectsByType<UIDocument>();
        foreach (var doc in alleDocuments)
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;

            var popupEl = root.Q<VisualElement>(popupName);
            if (popupEl != null)
            {
                popupEl.style.display = DisplayStyle.None;
                popupEl.style.position = Position.Absolute;
            }
        }
    }

    [ContextMenu("Ersten Start zurücksetzen")]
    public void ResetErsterStart()
    {
        PlayerPrefs.DeleteKey("ErsterAppStart_TutorialAngezeigt");
        PlayerPrefs.Save();
        Debug.Log("[TutorialManager] Erster App-Start zurückgesetzt!");
    }
}