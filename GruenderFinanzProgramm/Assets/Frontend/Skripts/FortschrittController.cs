// ================================================================
// FortschrittController.cs
//
// Fortschritt-Dashboard – zeigt:
//   1. Gesamtfortschritt (Gründerpfad + Dokumente kombiniert)
//   2. Gründerpfad-Phasen mit Mini-Balken
//   3. Nächste offene Schritte
//   4. Letzte Erfolge
// ================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class FortschrittController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Help Icon (Help circle.png zuweisen)")]
    [SerializeField] private Texture2D helpIconTexture;

    // Phasen-Definition (muss mit GruendungspfadController übereinstimmen)
    private static readonly (string name, string icon, string[] schrittIds)[] Phasen =
    {
        ("Vorbereitung", "\U0001f4cb", new[] { "vorb_1","vorb_2","vorb_3","vorb_4","vorb_5" }),
        ("Anmeldung",    "\U0001f4dd", new[] { "anm_1","anm_2","anm_3","anm_4","anm_5"     }),
        ("Finanzen",     "\U0001f4b0", new[] { "fin_1","fin_2","fin_3","fin_4"              }),
        ("Betrieb",      "\U0001f680", new[] { "betr_1","betr_2","betr_3","betr_4"         }),
        ("Sonstiges",    "\u2705",     new[] { "sonst_1","sonst_2","sonst_3"               }),
    };

    private static readonly Dictionary<string, string> SchrittTitel = new()
    {
        { "vorb_1", "Geschäftsidee ausarbeiten"    }, { "vorb_2", "Marktanalyse durchführen"    },
        { "vorb_3", "Businessplan erstellen"        }, { "vorb_4", "Rechtsform wählen"           },
        { "vorb_5", "Finanzierung klären"           },
        { "anm_1",  "Gewerbeanmeldung"              }, { "anm_2",  "Notar beauftragen"           },
        { "anm_3",  "Handelsregistereintrag"        }, { "anm_4",  "Steuernummer beantragen"     },
        { "anm_5",  "IHK/HWK Mitgliedschaft"        },
        { "fin_1",  "Geschäftskonto eröffnen"       }, { "fin_2",  "Buchhaltung einrichten"      },
        { "fin_3",  "Versicherungen abschließen"    }, { "fin_4",  "Startkapital verwalten"      },
        { "betr_1", "Erste Kunden akquirieren"      }, { "betr_2", "Website & Branding"          },
        { "betr_3", "Prozesse dokumentieren"        }, { "betr_4", "Jahresabschluss vorbereiten" },
        { "sonst_1","Team aufbauen"                 }, { "sonst_2","Fördermittel beantragen"     },
        { "sonst_3","Skalierung planen"             },
    };

    [System.Serializable]
    private class PfadSpeicherDaten
    {
        public List<string> erledigteIds = new List<string>();
    }

    // UI-Referenzen
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
        RegistriereHelpTooltips();
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
        var erledigt       = LadeErledigteSchritte();
        int dokAusgefuellt = LadeAnzahlAusgefuelltePflichtDoks();

        int pfadGesamt   = Phasen.Sum(p => p.schrittIds.Length);
        int pfadErledigt = Phasen.Sum(p => p.schrittIds.Count(id => erledigt.Contains(id)));
        int dokGesamt    = 21;
        int gesamt       = pfadGesamt + dokGesamt;
        int erledigtGes  = pfadErledigt + dokAusgefuellt;
        float prozent    = gesamt > 0 ? (float)erledigtGes / gesamt : 0f;

        if (lblGesamtProzent != null)
            lblGesamtProzent.text = $"{Mathf.RoundToInt(prozent * 100)}%";
        if (lblGesamtText != null)
            lblGesamtText.text = $"{erledigtGes} von {gesamt} Schritten";

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

            var iconLabel = new Label(done ? "\u2705" : icon);
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
            var done = new Label("\U0001f389 Alle Schritte erledigt!");
            done.style.color    = new StyleColor(new Color(128f / 255f, 207f / 255f, 149f / 255f));
            done.style.fontSize = 14;
            done.style.marginTop = 12;
            naechsteSchritteContainer.Add(done);
        }
    }

    private void BaueLetzteErfolge(HashSet<string> erledigt, int dokAusgefuellt)
    {
        if (letzteErfolgeContainer == null) return;
        letzteErfolgeContainer.Clear();

        var letzteErfolge = new List<(string icon, string titel, string kat)>();

        if (erledigt.Contains("vorb_1")) letzteErfolge.Add(("\U0001f4cb", "Geschäftsidee ausgearbeitet",  "Gründerpfad"));
        if (erledigt.Contains("vorb_2")) letzteErfolge.Add(("\U0001f50d", "Marktanalyse durchgeführt",    "Gründerpfad"));
        if (erledigt.Contains("vorb_3")) letzteErfolge.Add(("\U0001f4ca", "Businessplan erstellt",         "Gründerpfad"));
        if (erledigt.Contains("anm_1"))  letzteErfolge.Add(("\U0001f3db", "Gewerbe angemeldet",            "Gründerpfad"));
        if (erledigt.Contains("anm_3"))  letzteErfolge.Add(("\U0001f4dc", "Handelsregistereintrag",        "Gründerpfad"));
        if (erledigt.Contains("fin_1"))  letzteErfolge.Add(("\U0001f3e6", "Geschäftskonto eröffnet",       "Gründerpfad"));
        if (erledigt.Contains("fin_2"))  letzteErfolge.Add(("\U0001f4c2", "Buchhaltung eingerichtet",      "Gründerpfad"));
        if (erledigt.Contains("betr_2")) letzteErfolge.Add(("\U0001f3a8", "Website & Branding erstellt",   "Gründerpfad"));

        if (dokAusgefuellt >= 1)  letzteErfolge.Add(("\U0001f4c4", "Erstes Dokument ausgefüllt",  "Dokumente"));
        if (dokAusgefuellt >= 5)  letzteErfolge.Add(("\U0001f4c1", "5 Dokumente ausgefüllt",       "Dokumente"));
        if (dokAusgefuellt >= 10) letzteErfolge.Add(("\U0001f5c2", "10 Dokumente ausgefüllt",      "Dokumente"));

        foreach (var (chipIcon, titel, kat) in letzteErfolge.TakeLast(6))
        {
            var chip = new VisualElement();
            chip.AddToClassList("erfolg-chip");

            var iconLabel = new Label(chipIcon);
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
            hinweis.style.color    = new StyleColor(new Color(140f / 255f, 140f / 255f, 140f / 255f));
            hinweis.style.fontSize = 13;
            letzteErfolgeContainer.Add(hinweis);
        }
    }

    // ============================================================
    // HELP TOOLTIPS
    // ============================================================

    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Hier siehst du deinen gesamten Gründungsfortschritt auf einen Blick. " +
            "Der Balken kombiniert Gründerpfad-Schritte und ausgefüllte Pflichtdokumente. " +
            "Je mehr du erledigst, desto weiter füllt er sich.");

        HelpTooltip.Registriere(root, "btn-help-fortschritt",
            "Der Gesamtfortschritt setzt sich aus Gründerpfad-Schritten (21) " +
            "und Pflichtdokumenten (21) zusammen. " +
            "Jeder abgehakte Schritt und jedes ausgefüllte Dokument zählt.");

        HelpTooltip.Registriere(root, "btn-help-gruenderpfad",
            "Zeigt den Fortschritt der fünf Gründerpfad-Phasen. " +
            "Gehe zum Gründerpfad-Screen um einzelne Schritte abzuhaken " +
            "und den Balken weiter zu füllen.");

        HelpTooltip.Registriere(root, "btn-help-naechste-schritte",
            "Die nächsten offenen Schritte im Gründerpfad. " +
            "Erledige sie der Reihe nach um den Fortschritt voranzutreiben. " +
            "Die Liste aktualisiert sich automatisch.");

        HelpTooltip.Registriere(root, "btn-help-letzte-erfolge",
            "Zuletzt freigeschaltete Errungenschaften aus Gründerpfad und Dokumenten. " +
            "Den vollständigen Überblick findest du im Erfolge-Screen.");
    }
}
