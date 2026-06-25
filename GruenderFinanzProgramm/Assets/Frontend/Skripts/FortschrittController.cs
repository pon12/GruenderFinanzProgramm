// ================================================================
// FortschrittController.cs
//
// Fortschritt-Dashboard – zeigt:
//   1. Gesamtfortschritt (Gründerpfad + Dokumente kombiniert)
//   2. Gründerpfad-Phasen mit Mini-Balken
//   3. Nächste offene Schritte
//   4. Letzte Erfolge
//
// EINRICHTUNG IN UNITY:
//   1. Neues GameObject in der Fortschritt-Scene
//   2. Script + UIDocument drauf
//   3. UIDocument im Inspector zuweisen
// ================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class FortschrittController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    // ─── Phasen-Definition (muss mit GruendungspfadController übereinstimmen) ───
    private static readonly (string name, string icon, string[] schrittIds)[] Phasen =
    {
        ("Vorbereitung", "📋", new[] { "vorb_1","vorb_2","vorb_3","vorb_4","vorb_5" }),
        ("Anmeldung",    "📝", new[] { "anm_1","anm_2","anm_3","anm_4","anm_5"     }),
        ("Finanzen",     "💰", new[] { "fin_1","fin_2","fin_3","fin_4"              }),
        ("Betrieb",      "🚀", new[] { "betr_1","betr_2","betr_3","betr_4"         }),
        ("Sonstiges",    "✅", new[] { "sonst_1","sonst_2","sonst_3"               }),
    };

    // Schritt-Titel für Anzeige in "Nächste Schritte"
    private static readonly Dictionary<string, string> SchrittTitel = new()
    {
        { "vorb_1", "Geschäftsidee ausarbeiten"   }, { "vorb_2", "Marktanalyse durchführen"    },
        { "vorb_3", "Businessplan erstellen"       }, { "vorb_4", "Rechtsform wählen"           },
        { "vorb_5", "Finanzierung klären"          },
        { "anm_1",  "Gewerbeanmeldung"             }, { "anm_2",  "Notar beauftragen"           },
        { "anm_3",  "Handelsregistereintrag"       }, { "anm_4",  "Steuernummer beantragen"     },
        { "anm_5",  "IHK/HWK Mitgliedschaft"       },
        { "fin_1",  "Geschäftskonto eröffnen"      }, { "fin_2",  "Buchhaltung einrichten"      },
        { "fin_3",  "Versicherungen abschließen"   }, { "fin_4",  "Startkapital verwalten"      },
        { "betr_1", "Erste Kunden akquirieren"     }, { "betr_2", "Website & Branding"          },
        { "betr_3", "Prozesse dokumentieren"       }, { "betr_4", "Jahresabschluss vorbereiten" },
        { "sonst_1","Team aufbauen"                }, { "sonst_2","Fördermittel beantragen"     },
        { "sonst_3","Skalierung planen"            },
    };

    [System.Serializable]
    private class PfadSpeicherDaten
    {
        public List<string> erledigteIds = new List<string>();
    }

    // ─── UI Refs ───
    private VisualElement root;
    private Label         lblGesamtProzent;
    private Label         lblGesamtText;
    private VisualElement gesamtBalkenFill;
    private VisualElement phasenContainer;
    private VisualElement naechsteSchritteContainer;
    private VisualElement letzteErfolgeContainer;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        lblGesamtProzent          = root.Q<Label>("lbl-gesamt-prozent");
        lblGesamtText             = root.Q<Label>("lbl-gesamt-text");
        gesamtBalkenFill          = root.Q<VisualElement>("gesamt-balken-fill");
        phasenContainer           = root.Q<VisualElement>("phasen-container");
        naechsteSchritteContainer = root.Q<VisualElement>("naechste-schritte-container");
        letzteErfolgeContainer    = root.Q<VisualElement>("letzte-erfolge-container");

        BaueUI();
    }

    // ============================================================
    // DATEN LADEN
    // ============================================================

    private HashSet<string> LadeErledigteSchritte()
    {
        try
        {
            var db   = UserDatabaseAccess.getCurrentUserDatabase();
            var docs = db?.getAllUserDocuments();
            var save = docs?.FirstOrDefault(d => d.documentType == 9001);
            if (save == null) return new HashSet<string>();

            var daten = JsonUtility.FromJson<PfadSpeicherDaten>(save.text);
            return new HashSet<string>(daten?.erledigteIds ?? new List<string>());
        }
        catch { return new HashSet<string>(); }
    }

    private int LadeAnzahlAusgefuelltePflichtDoks()
    {
        try
        {
            var gespeichert = DocumentDashboard.GetSavedDocuments();
            if (gespeichert?.savedDocs == null) return 0;
            return gespeichert.savedDocs.Count(d =>
                d.istPflichtdokument &&
                (!string.IsNullOrWhiteSpace(d.inhalt) ||
                 (d.strukturFelder != null && d.strukturFelder.Any(f => !string.IsNullOrWhiteSpace(f.wert)))));
        }
        catch { return 0; }
    }

    // ============================================================
    // UI AUFBAUEN
    // ============================================================

    private void BaueUI()
    {
        var erledigt = LadeErledigteSchritte();
        int dokAusgefuellt = LadeAnzahlAusgefuelltePflichtDoks();

        // Gesamtfortschritt: Gründerpfad-Schritte + Pflichtdoks
        int pfadGesamt   = Phasen.Sum(p => p.schrittIds.Length);
        int pfadErledigt = Phasen.Sum(p => p.schrittIds.Count(id => erledigt.Contains(id)));
        int dokGesamt    = 21; // Pflichtdoks gesamt
        int gesamt       = pfadGesamt + dokGesamt;
        int erledigtGes  = pfadErledigt + dokAusgefuellt;
        float prozent    = gesamt > 0 ? (float)erledigtGes / gesamt : 0f;

        if (lblGesamtProzent != null)
            lblGesamtProzent.text = $"{Mathf.RoundToInt(prozent * 100)}%";
        if (lblGesamtText != null)
            lblGesamtText.text = $"{erledigtGes} von {gesamt} Schritten";

        // Segment-Balken befüllen
        string[] segNames = { "vorbereitung", "anmeldung", "finanzen", "betrieb", "sonstiges" };
        for (int i = 0; i < Phasen.Length && i < segNames.Length; i++)
        {
            var (_, _, ids) = Phasen[i];
            int g = ids.Length;
            int e = ids.Count(id => erledigt.Contains(id));
            float p = g > 0 ? (float)e / g : 0f;
            var fill = root.Q<VisualElement>($"seg-fill-{segNames[i]}");
            if (fill != null)
                fill.style.width = new StyleLength(new Length(p * 100f, LengthUnit.Percent));
        }

        BauePhasen(erledigt);
        BaueNaechsteSchritte(erledigt);
        BaueLetzteErfolge(erledigt, dokAusgefuellt);
    }

    private void BauePhasen(HashSet<string> erledigt)
    {
        if (phasenContainer == null) return;
        phasenContainer.Clear();

        foreach (var (name, icon, ids) in Phasen)
        {
            int ges  = ids.Length;
            int erl  = ids.Count(id => erledigt.Contains(id));
            float p  = ges > 0 ? (float)erl / ges : 0f;
            bool done = erl >= ges;

            var karte = new VisualElement();
            karte.AddToClassList("phase-karte");
            if (done) karte.AddToClassList("phase-karte--komplett");

            var iconLabel = new Label(done ? "✅" : icon);
            iconLabel.AddToClassList("phase-icon");
            karte.Add(iconLabel);

            var info = new VisualElement();
            info.AddToClassList("phase-info");

            var nameLabel = new Label(name);
            nameLabel.AddToClassList("phase-name");
            info.Add(nameLabel);

            var barBg = new VisualElement();
            barBg.AddToClassList("phase-mini-bar-bg");
            var barFill = new VisualElement();
            barFill.AddToClassList("phase-mini-bar-fill");
            barFill.style.width = new StyleLength(new Length(p * 100f, LengthUnit.Percent));
            barBg.Add(barFill);
            info.Add(barBg);

            karte.Add(info);

            var prozentLabel = new Label($"{erl}/{ges}");
            prozentLabel.AddToClassList("phase-prozent");
            karte.Add(prozentLabel);

            phasenContainer.Add(karte);
        }
    }

    private void BaueNaechsteSchritte(HashSet<string> erledigt)
    {
        if (naechsteSchritteContainer == null) return;
        naechsteSchritteContainer.Clear();

        int angezeigt = 0;
        foreach (var (phaseName, _, ids) in Phasen)
        {
            foreach (var id in ids)
            {
                if (erledigt.Contains(id)) continue;
                if (!SchrittTitel.TryGetValue(id, out string titel)) continue;

                var row = new VisualElement();
                row.AddToClassList("schritt-offen-row");

                var bullet = new VisualElement();
                bullet.AddToClassList("schritt-offen-bullet");
                row.Add(bullet);

                var textBlock = new VisualElement();
                textBlock.AddToClassList("schritt-offen-text");

                var titelLabel = new Label(titel);
                titelLabel.AddToClassList("schritt-offen-titel");
                textBlock.Add(titelLabel);

                var phaseLabel = new Label(phaseName);
                phaseLabel.AddToClassList("schritt-offen-phase");
                textBlock.Add(phaseLabel);

                row.Add(textBlock);
                naechsteSchritteContainer.Add(row);

                angezeigt++;
                if (angezeigt >= 5) return;
            }
        }

        if (angezeigt == 0)
        {
            var done = new Label("🎉 Alle Schritte erledigt!");
            done.style.color    = new StyleColor(new Color(128f/255f, 207f/255f, 149f/255f));
            done.style.fontSize = 14;
            done.style.marginTop = 12;
            naechsteSchritteContainer.Add(done);
        }
    }

    private void BaueLetzteErfolge(HashSet<string> erledigt, int dokAusgefuellt)
    {
        if (letzteErfolgeContainer == null) return;
        letzteErfolgeContainer.Clear();

        // Erfolge die abgehakt sind – aus ErfolgeController-Logik ableiten
        var letzteErfolge = new List<(string icon, string titel, string kat)>();

        // Aus Gründerpfad
        if (erledigt.Contains("vorb_1")) letzteErfolge.Add(("📋", "Geschäftsidee ausgearbeitet", "Gründerpfad"));
        if (erledigt.Contains("vorb_2")) letzteErfolge.Add(("🔍", "Marktanalyse durchgeführt",   "Gründerpfad"));
        if (erledigt.Contains("vorb_3")) letzteErfolge.Add(("📊", "Businessplan erstellt",        "Gründerpfad"));
        if (erledigt.Contains("anm_1"))  letzteErfolge.Add(("🏛", "Gewerbe angemeldet",           "Gründerpfad"));
        if (erledigt.Contains("anm_3"))  letzteErfolge.Add(("📜", "Handelsregistereintrag",       "Gründerpfad"));
        if (erledigt.Contains("fin_1"))  letzteErfolge.Add(("🏦", "Geschäftskonto eröffnet",      "Gründerpfad"));
        if (erledigt.Contains("fin_2"))  letzteErfolge.Add(("📂", "Buchhaltung eingerichtet",     "Gründerpfad"));
        if (erledigt.Contains("betr_2")) letzteErfolge.Add(("🎨", "Website & Branding erstellt",  "Gründerpfad"));

        // Aus Dokumenten
        if (dokAusgefuellt >= 1)  letzteErfolge.Add(("📄", "Erstes Dokument ausgefüllt",    "Dokumente"));
        if (dokAusgefuellt >= 5)  letzteErfolge.Add(("📁", "5 Dokumente ausgefüllt",         "Dokumente"));
        if (dokAusgefuellt >= 10) letzteErfolge.Add(("🗂", "10 Dokumente ausgefüllt",        "Dokumente"));

        // Max 6 anzeigen
        foreach (var (icon, titel, kat) in letzteErfolge.TakeLast(6))
        {
            var chip = new VisualElement();
            chip.AddToClassList("erfolg-chip");

            var iconLabel = new Label(icon);
            iconLabel.AddToClassList("erfolg-chip-icon");
            chip.Add(iconLabel);

            var textBlock = new VisualElement();
            textBlock.AddToClassList("erfolg-chip-text");

            var titelLabel = new Label(titel);
            titelLabel.AddToClassList("erfolg-chip-titel");
            textBlock.Add(titelLabel);

            var katLabel = new Label(kat);
            katLabel.AddToClassList("erfolg-chip-kat");
            textBlock.Add(katLabel);

            chip.Add(textBlock);
            letzteErfolgeContainer.Add(chip);
        }

        if (!letzteErfolge.Any())
        {
            var hinweis = new Label("Noch keine Erfolge freigeschaltet – leg los!");
            hinweis.style.color    = new StyleColor(new Color(140f/255f, 140f/255f, 140f/255f));
            hinweis.style.fontSize = 13;
            letzteErfolgeContainer.Add(hinweis);
        }
    }
}
