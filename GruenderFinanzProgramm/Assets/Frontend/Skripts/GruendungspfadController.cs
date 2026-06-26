// ================================================================
// GruendungspfadController.cs
// ================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class GruendungspfadController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Texture2D helpIconTexture;

    // ============================================================
    // DATENMODELL
    // ============================================================

    [System.Serializable]
    public class PfadSchritt
    {
        public string id;
        public string titel;
        public string beschreibung;
        public bool   istPflicht;
        public bool   erledigt;
    }

    [System.Serializable]
    public class PfadPhase
    {
        public string            name;
        public string            icon;
        public List<PfadSchritt> schritte;
    }

    [System.Serializable]
    private class SpeicherDaten
    {
        public List<string>      erledigteIds       = new List<string>();
        public List<PfadSchritt> eigeneSchritte     = new List<PfadSchritt>();
        public float[]           segmentFortschritt = new float[5];
        public float             gesamtFortschritt  = 0f;
    }

    // ============================================================
    // PHASEN-DEFINITION
    // ============================================================

    [Header("Phasen & Pflicht-Schritte (im Inspector befüllen)")]
    [SerializeField] private List<PfadPhase> phasen = new List<PfadPhase>
    {
        new PfadPhase
        {
            name = "Vorbereitung", icon = "\U0001f4cb",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "vorb_1", titel = "Gesch\u00e4ftsidee ausarbeiten",    beschreibung = "Idee konkretisieren und Alleinstellungsmerkmal definieren",        istPflicht = true },
                new PfadSchritt { id = "vorb_2", titel = "Marktanalyse durchf\u00fchren",      beschreibung = "Zielgruppe, Wettbewerber und Marktpotenzial analysieren",           istPflicht = true },
                new PfadSchritt { id = "vorb_3", titel = "Businessplan erstellen",              beschreibung = "Finanz-, Marketing- und Betriebsplan ausarbeiten",                  istPflicht = true },
                new PfadSchritt { id = "vorb_4", titel = "Rechtsform w\u00e4hlen",              beschreibung = "GmbH, UG, GbR, Einzelunternehmen \u2013 passende Form ausw\u00e4hlen", istPflicht = true },
                new PfadSchritt { id = "vorb_5", titel = "Finanzierung kl\u00e4ren",            beschreibung = "Eigenkapital, Kredit, F\u00f6rdermittel oder Investoren pr\u00fcfen",  istPflicht = true },
            }
        },
        new PfadPhase
        {
            name = "Anmeldung", icon = "\U0001f4dd",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "anm_1", titel = "Gewerbeanmeldung",               beschreibung = "Beim Gewerbeamt oder Finanzamt anmelden",                           istPflicht = true },
                new PfadSchritt { id = "anm_2", titel = "Notar beauftragen",              beschreibung = "F\u00fcr GmbH/UG: Gesellschaftsvertrag beurkunden lassen",           istPflicht = true },
                new PfadSchritt { id = "anm_3", titel = "Handelsregistereintrag",         beschreibung = "Eintragung beim zust\u00e4ndigen Amtsgericht beantragen",            istPflicht = true },
                new PfadSchritt { id = "anm_4", titel = "Steuernummer beantragen",        beschreibung = "Fragebogen zur steuerlichen Erfassung beim Finanzamt einreichen",   istPflicht = true },
                new PfadSchritt { id = "anm_5", titel = "IHK/HWK Mitgliedschaft",         beschreibung = "Industrie- oder Handelskammer Pflichtmitgliedschaft kl\u00e4ren",   istPflicht = true },
            }
        },
        new PfadPhase
        {
            name = "Finanzen", icon = "\U0001f4b0",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "fin_1", titel = "Gesch\u00e4ftskonto er\u00f6ffnen",  beschreibung = "Separates Konto f\u00fcr gesch\u00e4ftliche Transaktionen anlegen",  istPflicht = true },
                new PfadSchritt { id = "fin_2", titel = "Buchhaltung einrichten",              beschreibung = "Software oder Steuerberater f\u00fcr laufende Buchhaltung",          istPflicht = true },
                new PfadSchritt { id = "fin_3", titel = "Versicherungen abschlie\u00dfen",    beschreibung = "Betriebs-, Haftpflicht- und Berufsunf\u00e4higkeitsversicherung",    istPflicht = true },
                new PfadSchritt { id = "fin_4", titel = "Startkapital verwalten",             beschreibung = "Liquidit\u00e4tsplanung f\u00fcr die ersten 6\u201312 Monate",        istPflicht = true },
            }
        },
        new PfadPhase
        {
            name = "Betrieb", icon = "\U0001f680",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "betr_1", titel = "Erste Kunden akquirieren",      beschreibung = "Netzwerk nutzen, Marketing starten, erste Auftr\u00e4ge gewinnen",    istPflicht = true },
                new PfadSchritt { id = "betr_2", titel = "Website & Branding",             beschreibung = "Professionellen Online-Auftritt und Corporate Design erstellen",      istPflicht = true },
                new PfadSchritt { id = "betr_3", titel = "Prozesse dokumentieren",         beschreibung = "Arbeitsabl\u00e4ufe, Vorlagen und Standards festhalten",              istPflicht = true },
                new PfadSchritt { id = "betr_4", titel = "Jahresabschluss vorbereiten",    beschreibung = "Buchhaltungsunterlagen f\u00fcr Steuerberater bereitstellen",         istPflicht = true },
            }
        },
        new PfadPhase
        {
            name = "Sonstiges", icon = "\u2705",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "sonst_1", titel = "Team aufbauen",                 beschreibung = "Erste Mitarbeiter einstellen oder Freelancer beauftragen",           istPflicht = true },
                new PfadSchritt { id = "sonst_2", titel = "F\u00f6rdermittel beantragen",  beschreibung = "Staatliche Zusch\u00fcsse und F\u00f6rderprogramme f\u00fcr Gr\u00fcnder pr\u00fcfen", istPflicht = true },
                new PfadSchritt { id = "sonst_3", titel = "Skalierung planen",             beschreibung = "Wachstumsstrategie und n\u00e4chste Meilensteine definieren",         istPflicht = true },
            }
        },
    };

    // ============================================================
    // PHASEN-TOOLTIPS
    // ============================================================

    private static readonly Dictionary<string, string> PhasenTooltips = new Dictionary<string, string>
    {
        ["Vorbereitung"] =
            "Lege hier die Grundlagen deiner Gr\u00fcndung fest. " +
            "Arbeite Gesch\u00e4ftsidee, Marktanalyse und Businessplan aus, " +
            "bevor du mit der formellen Anmeldung beginnst.",
        ["Anmeldung"] =
            "Alles rund um die offizielle Gr\u00fcndung: Gewerbeanmeldung, Notar, " +
            "Handelsregister, Steuernummer und Kammermitgliedschaft. " +
            "Diese Schritte machen dein Unternehmen rechtlich existent.",
        ["Finanzen"] =
            "Richte die finanzielle Basis deines Unternehmens ein: " +
            "Gesch\u00e4ftskonto, Buchhaltung, Versicherungen und Liquidit\u00e4tsplanung " +
            "sichern deinen wirtschaftlichen Betrieb.",
        ["Betrieb"] =
            "Starte aktiv durch: Gewinne erste Kunden, baue deine Online-Pr\u00e4senz auf, " +
            "dokumentiere deine Prozesse und bereite den Jahresabschluss vor.",
        ["Sonstiges"] =
            "Optionale aber wichtige Schritte f\u00fcr dein Wachstum: " +
            "Team aufbauen, F\u00f6rdermittel nutzen und eine Skalierungsstrategie entwickeln.",
    };

    // ============================================================
    // SPEICHER
    // ============================================================

    private const int SAVE_DOCUMENT_TYPE = 9001;
    private SpeicherDaten speicherDaten = new SpeicherDaten();
    private int savedDocId = -1;

    private void LadeFortschritt()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        var docs     = db.getAllUserDocuments();
        var existing = docs?.FirstOrDefault(d => d.documentType == SAVE_DOCUMENT_TYPE);

        if (existing != null)
        {
            savedDocId = existing.id;
            try { speicherDaten = JsonUtility.FromJson<SpeicherDaten>(existing.text) ?? new SpeicherDaten(); }
            catch { speicherDaten = new SpeicherDaten(); }
        }
    }

    private void SpeichereFortschritt()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        var alleErledigtenIds = new List<string>();
        foreach (var phase in phasen)
            foreach (var s in phase.schritte)
                if (s.erledigt) alleErledigtenIds.Add(s.id);
        foreach (var s in speicherDaten.eigeneSchritte)
            if (s.erledigt) alleErledigtenIds.Add(s.id);

        speicherDaten.erledigteIds = alleErledigtenIds;

        string json = JsonUtility.ToJson(speicherDaten, false);
        db.deleteAllUserDocumentsByType(SAVE_DOCUMENT_TYPE);
        savedDocId = db.createUserDocument(SAVE_DOCUMENT_TYPE, "GruendungspfadSave", json);

        MeldeFortshrittAnDashboard();
    }

    private void WendeFortschrittAn()
    {
        foreach (var phase in phasen)
            foreach (var s in phase.schritte)
                s.erledigt = speicherDaten.erledigteIds.Contains(s.id);

        foreach (var s in speicherDaten.eigeneSchritte)
            s.erledigt = speicherDaten.erledigteIds.Contains(s.id);
    }

    private void MeldeFortshrittAnDashboard()
    {
        float[] segmente    = new float[5];
        int gesamtAlle      = 0;
        int gesamtErledigt  = 0;

        for (int i = 0; i < Mathf.Min(phasen.Count, 5); i++)
        {
            var phase  = phasen[i];
            var eigene = speicherDaten.eigeneSchritte
                .Where(s => s.id.StartsWith(phase.name + "_eigen_")).ToList();
            var alle = phase.schritte.Concat(eigene).ToList();

            int g = alle.Count;
            int e = alle.Count(s => s.erledigt);
            segmente[i]    = g > 0 ? (float)e / g : 0f;
            gesamtAlle    += g;
            gesamtErledigt += e;
        }

        var eigeneOhnePhase = speicherDaten.eigeneSchritte
            .Where(s => !phasen.Any(p => s.id.StartsWith(p.name + "_eigen_"))).ToList();
        gesamtAlle    += eigeneOhnePhase.Count;
        gesamtErledigt += eigeneOhnePhase.Count(s => s.erledigt);

        float gesamt = gesamtAlle > 0 ? (float)gesamtErledigt / gesamtAlle : 0f;
        speicherDaten.segmentFortschritt = segmente;
        speicherDaten.gesamtFortschritt  = gesamt;

        AppEventManager.DokumenteFortschrittGeaendert(
            segmente[0], segmente[1], segmente[2], segmente[3], segmente[4]);
    }

    // ============================================================
    // UI
    // ============================================================

    private VisualElement root;
    private VisualElement mainContainer;

    private VisualElement neuerSchrittPopup;
    private TextField     neuerSchrittTitel;
    private TextField     neuerSchrittBeschreibung;
    private DropdownField neuerSchrittPhase;
    private Button        neuerSchrittSpeichern;
    private Button        neuerSchrittAbbrechen;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root          = uiDocument.rootVisualElement;
        mainContainer = root.Q<VisualElement>("main-container");

        neuerSchrittPopup        = root.Q<VisualElement>("neuer-schritt-popup");
        neuerSchrittTitel        = root.Q<TextField>("neuer-schritt-titel");
        neuerSchrittBeschreibung = root.Q<TextField>("neuer-schritt-beschreibung");
        neuerSchrittPhase        = root.Q<DropdownField>("neuer-schritt-phase");
        neuerSchrittSpeichern    = root.Q<Button>("btn-neuer-schritt-speichern");
        neuerSchrittAbbrechen    = root.Q<Button>("btn-neuer-schritt-abbrechen");

        root.Q<Button>("btn-schritt-hinzufuegen")?.RegisterCallback<ClickEvent>(_ => OeffneNeuerSchrittPopup());

        if (neuerSchrittSpeichern != null) neuerSchrittSpeichern.clicked += NeuerSchrittSpeichern;
        if (neuerSchrittAbbrechen != null) neuerSchrittAbbrechen.clicked += () =>
        {
            if (neuerSchrittPopup != null) neuerSchrittPopup.style.display = DisplayStyle.None;
        };

        if (neuerSchrittPhase != null)
            neuerSchrittPhase.choices = phasen.Select(p => p.name).ToList();

        LadeFortschritt();
        WendeFortschrittAn();
        WendeDokumenteFortschrittAn();
        BaueUI();

        // Feste UXML-Icons registrieren
        RegistriereHelpTooltips();
    }

    // ============================================================
    // HELP TOOLTIPS
    // ============================================================

    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Der Gr\u00fcndungspfad begleitet dich Schritt f\u00fcr Schritt durch deine Unternehmensgr\u00fcndung. " +
            "Hake erledigte Schritte ab um deinen Fortschritt zu verfolgen. " +
            "Du kannst auch eigene Schritte hinzuf\u00fcgen.");

        HelpTooltip.Registriere(root, "btn-help-fortschritt",
            "Zeigt wie viele Pflichtschritte du bereits abgeschlossen hast. " +
            "Der Balken f\u00fcllt sich mit jedem abgehakten Schritt. " +
            "Der Gesamtfortschritt wird auch im Dashboard angezeigt.");

        HelpTooltip.Registriere(root, "btn-help-schritt-hinzufuegen",
            "F\u00fcge einen eigenen optionalen Schritt zu einer beliebigen Phase hinzu. " +
            "Eigene Schritte k\u00f6nnen jederzeit gel\u00f6scht werden. " +
            "Pflichtschritte bleiben immer erhalten.");

        HelpTooltip.Registriere(root, "btn-help-popup",
            "Gib einen Titel und optional eine kurze Beschreibung ein. " +
            "W\u00e4hle dann die Phase, zu der der Schritt geh\u00f6rt. " +
            "Eigene Schritte werden gespeichert und im Fortschritt mitgez\u00e4hlt.");
    }

    // ============================================================
    // DOKUMENTE → GRÜNDERPFAD MAPPING
    // ============================================================

    private static readonly Dictionary<string, string> DokZuSchrittId
        = new Dictionary<string, string>
    {
        { "Unternehmensstammdaten",                  "vorb_1" },
        { "Gr\u00fcndungsurkunde / Gesellschaftsvertrag", "anm_2"  },
        { "Handelsregisterauszug",                   "anm_3"  },
        { "Gewerbeanmeldung",                        "anm_1"  },
        { "Gesellschafterliste",                     "anm_2"  },
        { "Kontodaten (IBAN/BIC)",                   "fin_1"  },
        { "Zahlungsbedingungen",                     "fin_1"  },
        { "AGB",                                     "betr_3" },
        { "Disclaimer",                              "betr_3" },
        { "SEPA-Basislastschrift-Mandat",            "fin_1"  },
        { "Widerrufsbelehrung",                      "betr_3" },
        { "Businessplan",                            "vorb_3" },
        { "Markt- & Wettbewerbsanalyse",             "vorb_2" },
        { "Er\u00f6ffnungsbilanz",                   "fin_2"  },
        { "Datenschutzerkl\u00e4rung (DSGVO)",       "betr_3" },
        { "Steuernummer-Bescheid / USt-IdNr",        "anm_4"  },
        { "Impressum",                               "betr_2" },
        { "Dienstleistungskatalog / Preisliste",     "betr_1" },
        { "Corporate Identity Manual",               "betr_2" },
        { "Muster-Arbeitsvertrag",                   "sonst_1"},
        { "Gr\u00fcndungs-Checkliste",               "vorb_1" },
        { "Inventarliste",                           "fin_2"  },
        { "Inventur",                                "fin_2"  },
    };

    private void WendeDokumenteFortschrittAn()
    {
        var gespeichert = DocumentDashboard.GetSavedDocuments();
        if (gespeichert?.savedDocs == null) return;

        foreach (var doc in gespeichert.savedDocs)
        {
            if (!doc.istPflichtdokument) continue;

            bool hatInhalt   = !string.IsNullOrWhiteSpace(doc.inhalt);
            bool hatFeldwert = doc.strukturFelder != null &&
                               doc.strukturFelder.Any(f => !string.IsNullOrWhiteSpace(f.wert));

            if (!hatInhalt && !hatFeldwert) continue;
            if (!DokZuSchrittId.TryGetValue(doc.title, out string schrittId)) continue;

            foreach (var phase in phasen)
            {
                var schritt = phase.schritte.FirstOrDefault(s => s.id == schrittId);
                if (schritt != null) schritt.erledigt = true;
            }
        }
    }

    // ============================================================
    // UI AUFBAU
    // ============================================================

    private void BaueUI()
    {
        if (mainContainer == null) return;
        mainContainer.Clear();

        int gesamt   = phasen.Sum(p => p.schritte.Count) + speicherDaten.eigeneSchritte.Count;
        int erledigt = phasen.Sum(p => p.schritte.Count(s => s.erledigt))
                       + speicherDaten.eigeneSchritte.Count(s => s.erledigt);
        float prozent = gesamt > 0 ? (float)erledigt / gesamt : 0f;

        var fortschrittLabel = root.Q<Label>("lbl-gesamtfortschritt");
        if (fortschrittLabel != null)
            fortschrittLabel.text = $"{Mathf.RoundToInt(prozent * 100)}% abgeschlossen ({erledigt}/{gesamt} Schritte)";

        var balkenFill = root.Q<VisualElement>("fortschritt-balken-fill");
        if (balkenFill != null)
            balkenFill.style.width = new StyleLength(new Length(prozent * 100f, LengthUnit.Percent));

        foreach (var phase in phasen)
        {
            var alleSchritte = phase.schritte.ToList();
            var eigene = speicherDaten.eigeneSchritte
                .Where(s => s.id.StartsWith(phase.name + "_eigen_")).ToList();
            alleSchritte.AddRange(eigene);

            int phaseGesamt    = alleSchritte.Count;
            int phaseErledigt  = alleSchritte.Count(s => s.erledigt);
            float phaseProzent = phaseGesamt > 0 ? (float)phaseErledigt / phaseGesamt : 0f;

            var phaseCard = new VisualElement();
            phaseCard.AddToClassList("phase-card");

            // Header mit Fragezeichen rechts
            var header = new VisualElement();
            header.AddToClassList("phase-header");

            var iconLabel = new Label(phase.icon);
            iconLabel.AddToClassList("phase-icon");
            header.Add(iconLabel);

            var headerText = new VisualElement();
            headerText.style.flexGrow = 1;

            var phaseName = new Label(phase.name);
            phaseName.AddToClassList("phase-name");
            headerText.Add(phaseName);

            var phaseProgress = new Label($"{phaseErledigt}/{phaseGesamt} erledigt");
            phaseProgress.AddToClassList("phase-progress-label");
            headerText.Add(phaseProgress);

            header.Add(headerText);

            // Fragezeichen-Icon per C# in den Header
            var helpIcon = new VisualElement();
            helpIcon.name = "btn-help-phase-" + phase.name.ToLower();
            HelpTooltip.SetzeBasisStilOeffentlich(helpIcon);
            if (helpIconTexture != null)
            {
                helpIcon.style.backgroundImage              = new StyleBackground(helpIconTexture);
                helpIcon.style.unityBackgroundImageTintColor = new StyleColor(
                    new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f));
            }
            header.Add(helpIcon);

            if (PhasenTooltips.TryGetValue(phase.name, out string tooltipText))
                HelpTooltip.RegistriereInKarte(root, helpIcon, tooltipText);

            phaseCard.Add(header);

            var phaseMiniBalkenBg = new VisualElement();
            phaseMiniBalkenBg.AddToClassList("phase-mini-bar-bg");
            var phaseMiniBalkenFill = new VisualElement();
            phaseMiniBalkenFill.AddToClassList("phase-mini-bar-fill");
            phaseMiniBalkenFill.style.width = new StyleLength(new Length(phaseProzent * 100f, LengthUnit.Percent));
            phaseMiniBalkenBg.Add(phaseMiniBalkenFill);
            phaseCard.Add(phaseMiniBalkenBg);

            var schritteListe = new VisualElement();
            schritteListe.AddToClassList("schritte-liste");
            foreach (var schritt in alleSchritte)
                schritteListe.Add(BaueSchrittRow(schritt, phase.name));

            phaseCard.Add(schritteListe);
            mainContainer.Add(phaseCard);
        }
    }

    private VisualElement BaueSchrittRow(PfadSchritt schritt, string phaseName)
    {
        var row = new VisualElement();
        row.AddToClassList("schritt-row");
        if (schritt.erledigt) row.AddToClassList("schritt-erledigt");

        var checkbox = new Toggle();
        checkbox.AddToClassList("schritt-checkbox");
        checkbox.value = schritt.erledigt;
        checkbox.RegisterValueChangedCallback(evt =>
        {
            schritt.erledigt = evt.newValue;
            SpeichereFortschritt();
            BaueUI();
        });

        var textBlock = new VisualElement();
        textBlock.style.flexGrow  = 1;
        textBlock.style.marginLeft = 12;

        var titelLabel = new Label(schritt.titel);
        titelLabel.AddToClassList("schritt-titel");
        if (schritt.erledigt) titelLabel.AddToClassList("schritt-titel-erledigt");
        textBlock.Add(titelLabel);

        if (!string.IsNullOrEmpty(schritt.beschreibung))
        {
            var descLabel = new Label(schritt.beschreibung);
            descLabel.AddToClassList("schritt-beschreibung");
            textBlock.Add(descLabel);
        }

        row.Add(checkbox);
        row.Add(textBlock);

        if (!schritt.istPflicht)
        {
            var deleteBtn = new Button { text = "\u00d7" };
            deleteBtn.AddToClassList("schritt-delete-btn");
            deleteBtn.clicked += () =>
            {
                speicherDaten.eigeneSchritte.RemoveAll(s => s.id == schritt.id);
                SpeichereFortschritt();
                BaueUI();
            };
            row.Add(deleteBtn);
        }

        return row;
    }

    private void OeffneNeuerSchrittPopup()
    {
        if (neuerSchrittPopup == null) return;
        neuerSchrittPopup.style.display = DisplayStyle.Flex;
        if (neuerSchrittTitel        != null) neuerSchrittTitel.value        = "";
        if (neuerSchrittBeschreibung != null) neuerSchrittBeschreibung.value = "";
        if (neuerSchrittPhase        != null && phasen.Count > 0)
            neuerSchrittPhase.value = phasen[0].name;
    }

    private void NeuerSchrittSpeichern()
    {
        string titel = neuerSchrittTitel?.value.Trim() ?? "";
        if (string.IsNullOrEmpty(titel)) return;

        string phase = neuerSchrittPhase?.value ?? (phasen.Count > 0 ? phasen[0].name : "");
        string id    = phase + "_eigen_" + System.DateTime.Now.Ticks.ToString();

        var neuer = new PfadSchritt
        {
            id           = id,
            titel        = titel,
            beschreibung = neuerSchrittBeschreibung?.value.Trim() ?? "",
            istPflicht   = false,
            erledigt     = false,
        };

        speicherDaten.eigeneSchritte.Add(neuer);
        SpeichereFortschritt();
        BaueUI();

        if (neuerSchrittPopup != null) neuerSchrittPopup.style.display = DisplayStyle.None;
    }
}
