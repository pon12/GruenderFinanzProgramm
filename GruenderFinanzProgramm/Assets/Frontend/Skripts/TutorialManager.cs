using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

// ── Datenstuktur für einen Tutorial-Schritt ──────────────────────────────────
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

    private bool zeigeLoginNachTutorial = false;

    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

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
    private Label         erklaerungsText;
    private Label         schrittAnzeige;
    private Button        zurueckButton;
    private Button        weiterButton;
    private Button        beendenButton;
    private Button        langButton;
    private Button        kurzButton;
    private Button        keinTutorialButton;

    // Lambda-Referenzen für sauberes Unsubscribe
    private System.Action _onLang;
    private System.Action _onKurz;

    private List<TutorialSchritt> aktiveSchritte = new();
    private int aktuellerIndex = 0;

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

    /// In LoginScreenController nach erfolgreichem Login aufrufen:
    /// TutorialManager.Instance.PruefeErstenStart(nutzername);
    public void PruefeErstenStart(string nutzername)
    {
        string schluessel = nutzername + SchluesselSuffix;
        if (PlayerPrefs.GetInt(schluessel, 0) == 1) return;

        PlayerPrefs.SetInt(schluessel, 1);
        PlayerPrefs.Save();
        TutorialAuswahlOeffnen();
    }

    /// In Einstellungen.cs aufrufen:
    /// TutorialManager.Instance.TutorialAuswahlOeffnen();
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

        aktuellerIndex = 0;
        SetzeVisible(startDialogWrapper, false);
        SetzeVisible(tutorialModus,      true);
        StartCoroutine(LadeSceneUndZeigeSchritt());

        
    }

    private void AuswahlSchliessen()
    {
        SetzeVisible(startDialogWrapper, false);
        SetzeVisible(overlay,            false);
    }

    private void NaechsterSchritt()
    {
        if (aktuellerIndex < aktiveSchritte.Count - 1)
        {
            aktuellerIndex++;
            StartCoroutine(LadeSceneUndZeigeSchritt());
        }
        else
        {
            TutorialSchliessen();
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

    // Warten, bis das UI Toolkit die Elemente gezeichnet hat
    yield return null;
    yield return null;

    // Scrollen und Highlighten ausführen
    ZeigeElementMitScroll(schritt.ScrollViewName, schritt.ElementName);

    SetzeButtonsAktiv(true);
}

private void FokussiereUndHighlighteElement(string elementName)
{
    if (string.IsNullOrEmpty(elementName))
    {
        SetzeVisible(highlightRahmen, false);
        return;
    }

    // Suchen nach dem Element und dessen übergeordneter ScrollView
    VisualElement targetElement = null;
    ScrollView parentScrollView = null;

    var alleDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
    foreach (var doc in alleDocuments)
    {
        if (doc == uiDocument) continue; // Eigenes Overlay-UI ausklammern
        var root = doc.rootVisualElement;
        if (root == null) continue;

        var found = root.Q<VisualElement>(elementName);
        if (found != null && found.resolvedStyle.display != DisplayStyle.None)
        {
            targetElement = found;
            // Prüfen, ob das Element in einer ScrollView liegt
            parentScrollView = targetElement.GetFirstAncestorOfType<ScrollView>();
            break;
        }
    }

    if (targetElement == null)
    {
        SetzeVisible(highlightRahmen, false);
        Debug.LogWarning($"[TutorialManager] Element '{elementName}' nicht gefunden.");
        return;
    }

    // Falls das Element in einer ScrollView liegt, automatisch dorthin scrollen!
    if (parentScrollView != null)
    {
        parentScrollView.ScrollTo(targetElement);
    }

    // Highlight-Rahmen aktivieren und nach dem Layout-Update auf das Element ausrichten
    SetzeVisible(highlightRahmen, true);
    highlightRahmen.schedule.Execute(() =>
    {
        HighlightPositionieren(elementName);
    }).ExecuteLater(10); // 10ms Verzögerung sorgt für exakte Ausrichtung nach dem Scroll-Vorgang
}

private void ZeigeElementMitScroll(string scrollViewName, string elementName)
{
    if (string.IsNullOrEmpty(elementName))
    {
        SetzeVisible(highlightRahmen, false);
        return;
    }

    VisualElement targetElement = null;
    ScrollView scrollView = null;

    // Suche in allen UI Documents nach den Elementen
    var alleDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
    foreach (var doc in alleDocuments)
    {
        if (doc == uiDocument) continue;
        var root = doc.rootVisualElement;
        if (root == null) continue;

        if (targetElement == null)
            targetElement = root.Q<VisualElement>(elementName);

        if (!string.IsNullOrEmpty(scrollViewName) && scrollView == null)
            scrollView = root.Q<ScrollView>(scrollViewName);
    }

    if (targetElement == null)
    {
        SetzeVisible(highlightRahmen, false);
        Debug.LogWarning($"[TutorialManager] Element '{elementName}' nicht gefunden.");
        return;
    }

    // Falls eine ScrollView angegeben ist, direkt zum Ziel-Element springen
    if (scrollView != null)
    {
        scrollView.ScrollTo(targetElement);
    }

    // Rahmen einblenden und nach dem Scroll-Vorgang positionieren
    SetzeVisible(highlightRahmen, true);
    highlightRahmen.schedule.Execute(() =>
    {
        HighlightPositionieren(elementName);
    }).ExecuteLater(50); // 50ms Verzögerung stellt sicher, dass die Position nach dem Scrollen stimmt
}

private void SetzeButtonsAktiv(bool aktiv)
{
    weiterButton?.SetEnabled(aktiv);
    zurueckButton?.SetEnabled(aktiv && aktuellerIndex > 0);
    beendenButton?.SetEnabled(aktiv);
}

    private void VorherigerSchritt()
{
    if (aktuellerIndex > 0)
    {
        aktuellerIndex--;
        StartCoroutine(LadeSceneUndZeigeSchritt()); // <- Ruft nun ebenfalls den Screen-Wechsel auf
    }
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

    // Highlight-Rahmen vorerst ausblenden, bis fokussiert/gescrollt wurde
    SetzeVisible(highlightRahmen, false);
}

    private void HighlightPositionieren(string elementName)
    {
        var rect = HoleElementPosition(elementName);

        if (rect == null)
        {
            SetzeVisible(highlightRahmen, false);
            Debug.LogWarning($"[TutorialManager] Element '{elementName}' nicht gefunden.");
            return;
        }

        const float Padding = 8f;
        var r = rect.Value;

        highlightRahmen.style.left   = r.x - Padding;
        highlightRahmen.style.top    = r.y - Padding;
        highlightRahmen.style.width  = r.width  + Padding * 2;
        highlightRahmen.style.height = r.height + Padding * 2;
    }

    /// Durchsucht alle aktiven UIDocuments in der Scene nach dem Element.
    private Rect? HoleElementPosition(string elementName)
    {
        var alleDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var doc in alleDocuments)
        {
            if (doc == uiDocument) continue;
            var root = doc.rootVisualElement;
            if (root == null) continue;

            var element = root.Q<VisualElement>(elementName);
            if (element == null) continue;
            if (element.resolvedStyle.display == DisplayStyle.None) continue;

            return element.worldBound;
        }
        return null;
    }

    private void TutorialSchliessen()
    {
        // 1. Tutorial-UI und Popups ausblenden
        SchliesseAllePopups();
        SetzeVisible(tutorialModus, false);
        SetzeVisible(overlay,       false);

        // 2. Tutorial-Status zurücksetzen
        aktuellerIndex = 0;
        aktiveSchritte.Clear();

        // 3. Ziel-Screen basierend auf dem Modus laden/anzeigen
        if (zeigeLoginNachTutorial)
        {
            // Erststart-Fall: Zum Login-Screen im MainMenuManager wechseln
            zeigeLoginNachTutorial = false; // Zurücksetzen für spätere Durchläufe

            var mainMenu = Object.FindFirstObjectByType<MainMenuManager>();
            if (mainMenu != null)
            {
                mainMenu.ShowLogin();
            }
            else
            {
                // Falls MainMenuManager in einer anderen Szene liegt
                UnityEngine.SceneManagement.SceneManager.LoadScene("3-Start-Screens"); // Name deiner Hauptmenü-Szene
            }
        }
        else
        {
            // Manueller Durchlauf (z.B. aus den Einstellungen gestartet): Zurück zu den Einstellungen
            string einstellungenSzeneName = "Einstellungen";

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != einstellungenSzeneName)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(einstellungenSzeneName);
            }
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

    /// Rechtsklick auf die Komponente → "Standardschritte befüllen"
    /// Trägt alle Schritte mit Platzhaltertext vor – einfach Texte anpassen.
    [ContextMenu("Standardschritte befüllen")]
    private void StandardschritteBefllen()
    {
        alleSchritte = new List<TutorialSchritt>
        {
            /* ── Login ──────────────────────────────────────────────────────
            new() {
                Erklaerung    = "Willkommen bei Ventoriq! Diese kurze Einführung zeigt dir die wichtigsten Funktionen der App.",
                ElementName   = "",
                GameObjectName= "startScreenObj",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Mit diesem Button erstellst du ein neues Konto und richtest dein Profil ein.",
                ElementName   = "KontoErstellen",
                GameObjectName= "loginScreenObj",
                inKurzversion = true
            },
            */

            /* ── Registrierung ──────────────────────────────────────────────
            new() {
                Erklaerung    = "Gib hier deinen Profilnamen ein. Dieser wird in der App und auf deinen Dokumenten verwendet.",
                ElementName   = "Profileingabe",
                GameObjectName= "registrationScreenObj",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Akzeptiere hier die AGB und Datenschutzrichtlinie um fortzufahren.",
                ElementName   = "Toggle",
                GameObjectName= "registrationScreenObj",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Klicke hier um deinen verschlüsselten Passkey zu entschlüsseln und zu sehen. Notiere ihn sicher!",
                ElementName   = "btnEntschluesseln",
                GameObjectName= "registrationScreenObj",
                inKurzversion = true
            },
            */

            // ── Dashboard ──────────────────────────────────────────────────

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
                inKurzversion = true
            },

            new()
            {
                Erklaerung    = "In diesem Bereich wird ihre finanzielle Entwicklung des gesamten Jahres übersichtlich als Diagramm aufbereitet. Auf der linken Seite sehen sie die Beträge, während die Achse unten ihre Umsätze von Januar bis Dezember abbildet. So erkennen sie auf einen Blick, in welchen Monaten ihr Geschäft besonders gut läuft.",
                ElementName   = "kassenbuch-statistik-panel",
                SceneName     = "Dashboard",
                inKurzversion = true
            },

            new()
            {
                Erklaerung    = "Mit dem Fristenkalender verpassen die garantiert keine wichtigen Termine oder steuerlichen Abgabefristen mehr. Navigieren sie einfach durch die Monate und Jahre, um ihre Fälligkeiten bequem im Voraus zu planen. Ein Klick auf den jeweiligen Tag zeigt ihnen sofort alle anstehenden Aufgaben an.",
                ElementName   = "kalender-panel",
                SceneName     = "Dashboard",
                inKurzversion = true
            },

            // ── Settings ────────────────────────────────────────────────────
            
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
                inKurzversion = true
            },

            new() {
                Erklaerung    = "... den Anzeigemodus und Ihren Persönlichen Begleiter, welchen sie ein und ausschalten können ...",
                ElementName   = "card-layout",
                ScrollViewName  = "settings-scroll",
                SceneName     = "Einstellungen",
                inKurzversion = true
            },

            new() {
                Erklaerung    = "... und die Sicherheitseinstellungen, wo sie Ihren aktuellen Passkey zurücksetzen können.",
                ElementName   = "card-sicherheit",
                ScrollViewName  = "settings-scroll",
                SceneName     = "Einstellungen",
                inKurzversion = true
            },

            new() {
                Erklaerung    = "Ganz unten finden Sie den Tutorial-Button – Sie können diese Einführung jederzeit erneut starten.",
                ElementName   = "card-hilfe",
                ScrollViewName  = "settings-scroll",
                SceneName     = "Einstellungen",
                inKurzversion = true
            },
            
            // ── Dokumente ──────────────────────────────────────────────────

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
                inKurzversion = true
            },

            // ── Fortschritt ────────────────────────────────────────────────
            new() {
                Erklaerung    = "Hier sehen sie ihre zuletzt erreichten Meilensteine auf dem Weg zur Gründung.",
                ElementName   = "panel-letzte-erfolge",
                SceneName     = "Fortschritt",
                inKurzversion = true
            },

            // ── Angebot ────────────────────────────────────────────────────
            new() {
                Erklaerung    = "Der Angebot-Screen zeigt dir alle Werkzeuge um professionelle Angebote zu erstellen.",
                ElementName   = "",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier wählst du den Kunden aus dem Ihr das Angebot erstellt oder trägst einen neuen ein.",
                ElementName   = "card-partner",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier pflegst du Angebotsnummer, Datum, Status und Währung.",
                ElementName   = "card-grunddaten",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Hier kannst du Dateien und Dokumente direkt an das Angebot anhängen.",
                ElementName   = "card-anhaenge",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Trage hier alle Artikel und Leistungen mit Menge und Preis ein.",
                ElementName   = "card-positionen",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Hier kannst du Rabatt, Skonto und interne Notizen zum Angebot hinterlegen.",
                ElementName   = "card-details",
                inKurzversion = false
            },

            // ── Kundendatenbank ────────────────────────────────────────────
            new() {
                Erklaerung    = "Hier siehst du dein eigenes Profil das als Absender auf deinen Dokumenten erscheint.",
                ElementName   = "card-Lokaler-Nutzer",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Mit diesem Button legst du einen neuen Kunden in deiner Datenbank an.",
                ElementName   = "btn-add-coustomer",
                inKurzversion = true
            },

            // ── Finanzen ───────────────────────────────────────────────────
            new() {
                Erklaerung    = "Hier siehst du deine Rentabilitätskennzahlen – Umsatz, Kosten und Gewinn im Überblick.",
                ElementName   = "panel-rentabilitaet",
                inKurzversion = true
            },
            new() {
                Erklaerung    = "Dieser Bereich zeigt deine Liquidität zum Geschäftsbeginn – was du zum Start benötigst.",
                ElementName   = "panel-liquiditaet",
                inKurzversion = false
            },
            new() {
                Erklaerung    = "Die Liquiditätsplanung gibt dir einen Überblick über Ein- und Ausgaben über die Zeit.",
                ElementName   = "panel-liquiditaetsplanung",
                inKurzversion = false
            },

            // ── Dokumente ──────────────────────────────────────────────────
            new() {
                Erklaerung    = "Wähle hier eine Vorlage für dein neues Dokument: Standard-Text, Diagramm oder Checkliste.",
                ElementName   = "Template-Grid",
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
    // 1. Zuerst aktiv suchen
    var obj = GameObject.Find(objName);
    if (obj != null) return obj;

    // 2. Durchsuche alle geladenen Szenen inklusive Root-Objekte
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
    var alleDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
    foreach (var doc in alleDocuments)
    {
        var root = doc.rootVisualElement;
        if (root == null) continue;

        var popupEl = root.Q<VisualElement>(popupName);
        if (popupEl != null)
        {
            popupEl.style.display = DisplayStyle.Flex;
            return;
        }
    }
    Debug.LogWarning($"[TutorialManager] UI Element Popup '{popupName}' wurde nicht gefunden.");
}

private void SchliesseAllePopups()
{
    // 1. Alle UI-Toolkit Popups durchgehen und ausblenden
    foreach (var schritt in alleSchritte)
    {
        if (!string.IsNullOrEmpty(schritt.PopupElementName))
        {
            VersteckePopupVisualElement(schritt.PopupElementName);
        }
    }

    // 2. Alle GameObject-basierten Popups / Zusatz-Screens ausblenden,
    // außer wenn sie einer der 3 Haupt-Screens sind (die werden separat verwaltet)
    foreach (var schritt in alleSchritte)
    {
        if (!string.IsNullOrEmpty(schritt.GameObjectName))
        {
            // Hauptscreens überspringen, da diese in Step 3 gezielt geschaltet werden
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
    var alleDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
    foreach (var doc in alleDocuments)
    {
        var root = doc.rootVisualElement;
        if (root == null) continue;

        var popupEl = root.Q<VisualElement>(popupName);
        if (popupEl != null)
        {
            popupEl.style.display = DisplayStyle.None;
        }
    }
}

// ── Public API ───────────────────────────────────────────────────────────

/// <summary>
/// Prüft beim allerersten App-Start, ob das Tutorial gezeigt werden soll.
/// </summary>
public void PruefeErstenAppStart()
    {
        const string Key = "ErsterAppStart_TutorialAngezeigt";

        // Falls die App schon einmal gestartet wurde, abbrechen
        if (PlayerPrefs.GetInt(Key, 0) == 1) return;

        // Als gestartet markieren und speichern
        PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();

        // Merken: Beim allerersten Start soll danach der Login-Screen kommen!
        zeigeLoginNachTutorial = true;

        // Tutorial starten
        TutorialAuswahlOeffnen(); 
    }

// Hilfsmethode zum Testen im Unity Inspector
[ContextMenu("Ersten Start zurücksetzen")]
    public void ResetErsterStart()
    {
        PlayerPrefs.DeleteKey("ErsterAppStart_TutorialAngezeigt");
        PlayerPrefs.Save();
        Debug.Log("[TutorialManager] Erster App-Start zurückgesetzt!");
    }
}