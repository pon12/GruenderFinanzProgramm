// ================================================================
// GruenderpfadAutoErkennung.cs
//
// Zentrale Logik: welche Gründerpfad-Schritte lassen sich automatisch
// aus vorhandenen echten Daten ableiten (ausgefüllte Pflichtdokumente,
// Kassenbuch-Einträge, Kunden in der KDB)?
//
// WICHTIG: Diese Klasse wird von DREI Controllern genutzt
// (GruendungspfadController, FortschrittController, ErfolgeController),
// damit alle drei IMMER denselben Stand zeigen - unabhängig davon,
// welchen der drei Screens man zuerst öffnet.
// ================================================================
using System.Collections.Generic;
using System.Linq;

public static class GruenderpfadAutoErkennung
{
    // Dokument-Titel (Dokumente-Pool) -> Gründerpfad-Schritt-Id
    public static readonly Dictionary<string, string> DokZuSchrittId = new Dictionary<string, string>
    {
        { "Unternehmensstammdaten",                  "vorb_1" },
        { "Gr\u00fcndungsurkunde",                     "anm_2"  },
        { "Gesellschaftsvertrag",                     "anm_2"  },
        { "Fragebogen zur Steuerlichen Erfassung",    "anm_4"  }, // dasselbe Ziel wie "Steuernummer-Bescheid / USt-IdNr" weiter unten
        { "Anmeldung Berufsgenossenschaft",           "fin_3"  }, // Versicherungen abschließen
        { "Organigramm",                              "sonst_1"}, // dasselbe Ziel wie "Muster-Arbeitsvertrag" weiter unten
        { "Handelsregisterauszug",                   "anm_3"  },
        { "Gewerbeanmeldung",                        "anm_1"  },
        { "Gesellschafterliste",                     "anm_2"  },
        { "Kontodaten (IBAN/BIC)",                   "fin_1"  },
        { "Zahlungsbedingungen",                     "fin_1"  },
        { "AGB",                                     "betr_3" },
        { "Disclaimer",                              "betr_3" },
        { "Barzahlung",                              "fin_2"  }, // Buchhaltung einrichten
        { "Überweisung",                             "fin_2"  }, // Buchhaltung einrichten
        { "SEPA-Basislastschrift-Mandat",            "fin_1"  },
        { "Widerrufsbelehrung",                      "betr_3" },
        { "Mahnverfahren",                            "betr_3" },
        { "Ratenzahlungsbestimmungen",                "betr_3" },
        { "Businessplan",                            "vorb_3" },
        { "Markt- & Wettbewerbsanalyse",             "vorb_2" },
        { "Er\u00f6ffnungsbilanz",                   "fin_2"  },
        { "Datenschutzerkl\u00e4rung (DSGVO)",       "betr_3" },
        { "Steuernummer-Bescheid / USt-IdNr",        "anm_4"  },
        { "Impressum",                               "betr_2" },
        { "Copyright Hinweis",                        "betr_3" },
        { "Lizenzhinweis Einfach",                    "betr_3" },
        { "Dienstleistungskatalog / Preisliste",     "betr_1" },
        { "Corporate Identity Manual",               "betr_2" },
        { "Muster-Arbeitsvertrag",                   "sonst_1"},
        { "Gr\u00fcndungs-Checkliste",                "vorb_1" },
        { "Inventarliste",                            "fin_2"  },
        { "Inventur",                                 "fin_2"  },
    };

    // Welche Schritt-IDs lassen sich aus ausgefüllten Pflichtdokumenten ableiten?
    public static HashSet<string> AusDokumenten()
    {
        var ergebnis = new HashSet<string>();
        try
        {
            var gespeichert = DocumentDashboard.GetSavedDocuments();
            if (gespeichert?.savedDocs == null) return ergebnis;

            foreach (var doc in gespeichert.savedDocs)
            {
                if (!doc.istPflichtdokument) continue;

                bool hatInhalt   = !string.IsNullOrWhiteSpace(doc.inhalt);
                bool hatFeldwert = doc.strukturFelder != null &&
                                   doc.strukturFelder.Any(f => !string.IsNullOrWhiteSpace(f.wert));
                if (!hatInhalt && !hatFeldwert) continue;

                if (DokZuSchrittId.TryGetValue(doc.title, out string schrittId))
                    ergebnis.Add(schrittId);
            }
        }
        catch { /* defensiv - lieber nix automatisch abhaken als crashen */ }

        return ergebnis;
    }

    // Zusätzliche Schritte, die sich direkt aus echten App-Daten ableiten
    // lassen (nicht nur Dokumente): Kassenbuch-Einträge, Kunden in der KDB,
    // Firmendaten, Finanzkennzahlen.
    public static HashSet<string> AusAppDaten(DataBase db)
    {
        var ergebnis = new HashSet<string>();
        if (db == null) return ergebnis;

        try
        {
            bool hatEinkommen = (db.getAllEinkommenEntries()?.Count ?? 0) > 0;
            bool hatAusgaben  = (db.getAllAusgabenEntries()?.Count ?? 0) > 0;

            if (hatEinkommen || hatAusgaben) ergebnis.Add("fin_2"); // Buchhaltung eingerichtet

            // Jahresabschluss vorbereiten: braucht mehr als nur EINEN
            // Buchungstyp - beide Seiten (Einnahmen UND Ausgaben) müssen
            // gebucht sein, damit überhaupt ein sinnvoller Abschluss
            // möglich ist.
            if (hatEinkommen && hatAusgaben) ergebnis.Add("betr_4");

            bool hatKunde = (db.getAllCustomers()?.Count ?? 0) > 0;
            if (hatKunde) ergebnis.Add("betr_1"); // Erste Kunden akquiriert

            // Rechtsform wählen: das freie Textfeld "Rechtsform" in den
            // Unternehmensstammdaten ist ausgefüllt.
            var gespeichert = DocumentDashboard.GetSavedDocuments();
            var stammdaten = gespeichert?.savedDocs?.FirstOrDefault(d => d.title == "Unternehmensstammdaten");
            string rechtsform = stammdaten?.strukturFelder?.FirstOrDefault(f => f.key == "rechtsform")?.wert;
            if (!string.IsNullOrWhiteSpace(rechtsform)) ergebnis.Add("vorb_4");

            // Finanzierung klären & Startkapital verwalten: nutzen dieselben
            // Kennzahlen wie die Finanzplanung-Erfolge (siehe FinanzKennzahlenService).
            var k = FinanzKennzahlenService.Berechne(db);

            bool hatFinanzierungsquelle = k.Geldeinlagen > 0 || k.Kredite > 0 || k.Darlehen > 0 || k.Sacheinlagen > 0;
            if (hatFinanzierungsquelle) ergebnis.Add("vorb_5"); // Finanzierung klären

            bool kapitalbedarfGedeckt = k.Kapitalbedarf > 0 && k.GesamtKapital >= k.Kapitalbedarf;
            if (kapitalbedarfGedeckt) ergebnis.Add("fin_4"); // Startkapital verwalten
        }
        catch { /* defensiv - lieber nix automatisch abhaken als crashen */ }

        return ergebnis;
    }

    // Kombiniert beide Quellen zu einer Gesamtmenge automatisch erkannter Schritte.
    public static HashSet<string> ErmittleAlle(DataBase db)
    {
        var result = AusDokumenten();
        result.UnionWith(AusAppDaten(db));
        return result;
    }
}
