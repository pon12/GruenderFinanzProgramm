// ================================================================
// GruendungspfadController.cs
//
// Roadmap mit abhakbaren Checkpoints:
//  - 4 Phasen (Vorbereitung → Anmeldung → Finanzen → Betrieb)
//  - Feste Pflicht-Schritte (vom Entwickler/Chef im Inspector)
//  - Nutzer kann eigene optionale Schritte hinzufügen
//  - Fortschritt pro Nutzer in der DB gespeichert
//  - Gesamtfortschritt wird per AppEventManager ans Dashboard gemeldet
// ================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class GruendungspfadController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    // ============================================================
    // DATENMODELL
    // ============================================================

    [System.Serializable]
    public class PfadSchritt
    {
        public string id;               // eindeutige ID, z.B. "vorbereitung_1"
        public string titel;            // Anzeigename
        public string beschreibung;     // optionale Erklärungs-Zeile
        public bool   istPflicht;       // true = vom Chef vorgegeben, nicht löschbar
        public bool   erledigt;         // wird pro Nutzer gespeichert
    }

    [System.Serializable]
    public class PfadPhase
    {
        public string           name;
        public string           icon;   // Emoji-Icon für die Phasenkarte
        public List<PfadSchritt> schritte;
    }

    [System.Serializable]
    private class SpeicherDaten
    {
        public List<string>    erledigteIds       = new List<string>();
        public List<PfadSchritt> eigeneSchritte   = new List<PfadSchritt>();
        public float[]         segmentFortschritt = new float[5]; // für Dashboard-Balken
    }

    // ============================================================
    // PHASEN-DEFINITION (vom Chef im Inspector befüllbar)
    // ============================================================

    [Header("Phasen & Pflicht-Schritte (im Inspector befüllen)")]
    [SerializeField] private List<PfadPhase> phasen = new List<PfadPhase>
    {
        new PfadPhase
        {
            name = "Vorbereitung",
            icon = "📋",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "vorb_1", titel = "Geschäftsidee ausarbeiten",    beschreibung = "Idee konkretisieren und Alleinstellungsmerkmal definieren",        istPflicht = true },
                new PfadSchritt { id = "vorb_2", titel = "Marktanalyse durchführen",      beschreibung = "Zielgruppe, Wettbewerber und Marktpotenzial analysieren",           istPflicht = true },
                new PfadSchritt { id = "vorb_3", titel = "Businessplan erstellen",        beschreibung = "Finanz-, Marketing- und Betriebsplan ausarbeiten",                  istPflicht = true },
                new PfadSchritt { id = "vorb_4", titel = "Rechtsform wählen",             beschreibung = "GmbH, UG, GbR, Einzelunternehmen – passende Form auswählen",        istPflicht = true },
                new PfadSchritt { id = "vorb_5", titel = "Finanzierung klären",           beschreibung = "Eigenkapital, Kredit, Fördermittel oder Investoren prüfen",         istPflicht = true },
            }
        },
        new PfadPhase
        {
            name = "Anmeldung",
            icon = "📝",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "anm_1", titel = "Gewerbeanmeldung",               beschreibung = "Beim Gewerbeamt oder Finanzamt anmelden",                           istPflicht = true },
                new PfadSchritt { id = "anm_2", titel = "Notar beauftragen",              beschreibung = "Für GmbH/UG: Gesellschaftsvertrag beurkunden lassen",               istPflicht = true },
                new PfadSchritt { id = "anm_3", titel = "Handelsregistereintrag",         beschreibung = "Eintragung beim zuständigen Amtsgericht beantragen",                istPflicht = true },
                new PfadSchritt { id = "anm_4", titel = "Steuernummer beantragen",        beschreibung = "Fragebogen zur steuerlichen Erfassung beim Finanzamt einreichen",   istPflicht = true },
                new PfadSchritt { id = "anm_5", titel = "IHK/HWK Mitgliedschaft",         beschreibung = "Industrie- oder Handelskammer Pflichtmitgliedschaft klären",        istPflicht = true },
            }
        },
        new PfadPhase
        {
            name = "Finanzen",
            icon = "💰",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "fin_1", titel = "Geschäftskonto eröffnen",        beschreibung = "Separates Konto für geschäftliche Transaktionen anlegen",           istPflicht = true },
                new PfadSchritt { id = "fin_2", titel = "Buchhaltung einrichten",         beschreibung = "Software oder Steuerberater für laufende Buchhaltung organisieren", istPflicht = true },
                new PfadSchritt { id = "fin_3", titel = "Versicherungen abschließen",     beschreibung = "Betriebs-, Haftpflicht- und ggf. Berufsunfähigkeitsversicherung",   istPflicht = true },
                new PfadSchritt { id = "fin_4", titel = "Startkapital verwalten",         beschreibung = "Liquiditätsplanung für die ersten 6–12 Monate aufstellen",          istPflicht = true },
            }
        },
        new PfadPhase
        {
            name = "Betrieb",
            icon = "🚀",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "betr_1", titel = "Erste Kunden akquirieren",      beschreibung = "Netzwerk nutzen, Marketing starten, erste Aufträge gewinnen",       istPflicht = true },
                new PfadSchritt { id = "betr_2", titel = "Website & Branding",            beschreibung = "Professionellen Online-Auftritt und Corporate Design erstellen",     istPflicht = true },
                new PfadSchritt { id = "betr_3", titel = "Prozesse dokumentieren",        beschreibung = "Arbeitsabläufe, Vorlagen und Standards festhalten",                  istPflicht = true },
                new PfadSchritt { id = "betr_4", titel = "Jahresabschluss vorbereiten",   beschreibung = "Buchhaltungsunterlagen für Steuerberater oder Finanzamt bereitstellen", istPflicht = true },
            }
        },
        new PfadPhase
        {
            name = "Sonstiges",
            icon = "✅",
            schritte = new List<PfadSchritt>
            {
                new PfadSchritt { id = "sonst_1", titel = "Team aufbauen",                beschreibung = "Erste Mitarbeiter einstellen oder Freelancer beauftragen",            istPflicht = true },
                new PfadSchritt { id = "sonst_2", titel = "Fördermittel beantragen",      beschreibung = "Staatliche Zuschüsse und Förderprogramme für Gründer prüfen",         istPflicht = true },
                new PfadSchritt { id = "sonst_3", titel = "Skalierung planen",            beschreibung = "Wachstumsstrategie und nächste Meilensteine definieren",              istPflicht = true },
            }
        },
    };

    // ============================================================
    // SPEICHER
    // ============================================================

    private const int SAVE_DOCUMENT_TYPE = 9001; // eindeutiger Typ für Gründungspfad-Speicherdaten
    private SpeicherDaten speicherDaten = new SpeicherDaten();
    private int savedDocId = -1;

    private void LadeFortschritt()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        var docs = db.getAllUserDocuments();
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

        // IDs aller aktuell erledigten Schritte (Pflicht + eigene) sammeln
        var alleErledigtenIds = new List<string>();
        foreach (var phase in phasen)
            foreach (var s in phase.schritte)
                if (s.erledigt) alleErledigtenIds.Add(s.id);
        foreach (var s in speicherDaten.eigeneSchritte)
            if (s.erledigt) alleErledigtenIds.Add(s.id);

        speicherDaten.erledigteIds = alleErledigtenIds;

        string json = JsonUtility.ToJson(speicherDaten, false);

        if (savedDocId >= 0)
        {
            var docs = db.getAllUserDocuments();
            var existing = docs?.FirstOrDefault(d => d.id == savedDocId);
            if (existing != null)
            {
                existing.text = json;
                // UserDocument update nicht direkt verfügbar – neu anlegen
                db.createUserDocument(SAVE_DOCUMENT_TYPE, "GruendungspfadSave", json);
            }
        }
        else
        {
            savedDocId = db.createUserDocument(SAVE_DOCUMENT_TYPE, "GruendungspfadSave", json);
        }

        MeldeFortshrittAnDashboard();
    }

    private void WendeFortschrittAn()
    {
        // Geladene IDs auf die Schritte übertragen
        foreach (var phase in phasen)
            foreach (var s in phase.schritte)
                s.erledigt = speicherDaten.erledigteIds.Contains(s.id);

        foreach (var s in speicherDaten.eigeneSchritte)
            s.erledigt = speicherDaten.erledigteIds.Contains(s.id);
    }

    private void MeldeFortshrittAnDashboard()
    {
        float[] segmente = new float[5];

        for (int i = 0; i < Mathf.Min(phasen.Count, 5); i++)
        {
            var phase = phasen[i];
            var eigene = speicherDaten.eigeneSchritte
                .Where(s => s.id.StartsWith(phase.name + "_eigen_")).ToList();
            var alle = phase.schritte.Concat(eigene).ToList();

            int gesamt   = alle.Count;
            int erledigt = alle.Count(s => s.erledigt);
            segmente[i]  = gesamt > 0 ? (float)erledigt / gesamt : 0f;
        }

        // Im Speicher ablegen damit Dashboard beim nächsten Start
        // den Fortschritt lesen kann ohne Gründungspfad-Screen zu öffnen
        speicherDaten.segmentFortschritt = segmente;

        AppEventManager.DokumenteFortschrittGeaendert(
            segmente[0], segmente[1], segmente[2], segmente[3], segmente[4]);
    }

    // ============================================================
    // UI
    // ============================================================

    private VisualElement root;
    private VisualElement mainContainer;

    // Neuer-Schritt-Popup
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

        root = uiDocument.rootVisualElement;
        mainContainer = root.Q<VisualElement>("main-container");

        neuerSchrittPopup       = root.Q<VisualElement>("neuer-schritt-popup");
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

        // Dropdown mit Phasennamen befüllen
        if (neuerSchrittPhase != null)
            neuerSchrittPhase.choices = phasen.Select(p => p.name).ToList();

        LadeFortschritt();
        WendeFortschrittAn();
        BaueUI();
    }

    private void BaueUI()
    {
        if (mainContainer == null) return;
        mainContainer.Clear();

        // Gesamtfortschrittsbalken oben
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

        // Phasenkarten
        foreach (var phase in phasen)
        {
            var alleSchritteDieserPhase = phase.schritte.ToList();

            // Eigene Schritte dieser Phase anhängen
            var eigene = speicherDaten.eigeneSchritte.Where(s => s.id.StartsWith(phase.name + "_eigen_")).ToList();
            alleSchritteDieserPhase.AddRange(eigene);

            int phaseGesamt   = alleSchritteDieserPhase.Count;
            int phaseErledigt = alleSchritteDieserPhase.Count(s => s.erledigt);
            float phaseProzent = phaseGesamt > 0 ? (float)phaseErledigt / phaseGesamt : 0f;

            var phaseCard = new VisualElement();
            phaseCard.AddToClassList("phase-card");

            // Phasen-Header
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

            // Kleiner Fortschrittsbalken pro Phase
            var phaseMiniBalkenBg = new VisualElement();
            phaseMiniBalkenBg.AddToClassList("phase-mini-bar-bg");
            var phaseMiniBalkenFill = new VisualElement();
            phaseMiniBalkenFill.AddToClassList("phase-mini-bar-fill");
            phaseMiniBalkenFill.style.width = new StyleLength(new Length(phaseProzent * 100f, LengthUnit.Percent));
            phaseMiniBalkenBg.Add(phaseMiniBalkenFill);

            phaseCard.Add(header);
            phaseCard.Add(phaseMiniBalkenBg);

            // Schritte-Liste
            var schritteListe = new VisualElement();
            schritteListe.AddToClassList("schritte-liste");

            foreach (var schritt in alleSchritteDieserPhase)
            {
                var row = BaueSchrittRow(schritt, phase.name);
                schritteListe.Add(row);
            }

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
            BaueUI(); // UI neu aufbauen für aktualisierte Fortschrittsbalken
        });

        var textBlock = new VisualElement();
        textBlock.style.flexGrow = 1;
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

        // Löschen-Button nur bei eigenen (nicht-Pflicht) Schritten
        if (!schritt.istPflicht)
        {
            var deleteBtn = new Button { text = "×" };
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
            id             = id,
            titel          = titel,
            beschreibung   = neuerSchrittBeschreibung?.value.Trim() ?? "",
            istPflicht     = false,
            erledigt       = false,
        };

        speicherDaten.eigeneSchritte.Add(neuer);
        SpeichereFortschritt();
        BaueUI();

        if (neuerSchrittPopup != null) neuerSchrittPopup.style.display = DisplayStyle.None;
    }
}
