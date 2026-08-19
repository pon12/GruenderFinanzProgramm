// ================================================================
// FinanzKennzahlenService.cs
//
// Zentrale Berechnung von Finanz-Kennzahlen für die Erfolge-Auswertung.
// Bewusst getrennt von Finanzen 1 / Finanzen 2 (die rechnen pro Jahr für
// die Anzeige) - hier geht's um LEBENSZEIT-Summen über alle Jahre, weil
// ein Erfolg wie "erster Rohgewinn erzielt" nicht davon abhängen soll,
// welches Jahr man sich gerade im Finanzierungs-Screen anschaut.
// ================================================================
using System.Collections.Generic;
using System.Linq;

public static class FinanzKennzahlenService
{
    public struct Kennzahlen
    {
        public float Umsatzerloese;
        public float GesamtEinnahmen;
        public float Rohgewinn;
        public float SummeInvestition;
        public float SummeUmlaufvermoegen;
        public float SummeGruenderkosten;
        public float Sacheinlagen;
        public float Kredite;
        public float Darlehen;
        public float Wertpapiere;
        public float BoerseKrypto;
        public float Geldeinlagen;
        public float Liquiditaetsreserve;
        public float Kapitalbedarf;
        public float GesamtKapital;
        public float Betriebsausgaben;
        public int   AnzahlKassenbuchEintraege;
    }

    public static Kennzahlen Berechne(DataBase db)
    {
        var k = new Kennzahlen();
        if (db == null) return k;

        float gehaelter = 0f, sonstigeKostenEink = 0f, sonstigeAusg = 0f;
        float marketing = 0f, reisekosten = 0f, steuern = 0f, finanzamt = 0f,
              tilgungsraten = 0f, privatentnahme = 0f;

        var einkList = db.getAllEinkommenEntries();
        if (einkList != null)
        {
            k.AnzahlKassenbuchEintraege += einkList.Count;
            foreach (var e in einkList)
            {
                k.GesamtEinnahmen += e.Amount;
                switch (e.getArt() ?? "")
                {
                    case "Umsatzerlöse":     k.Umsatzerloese += e.Amount; break;
                    case "Privateinzahlung": k.Geldeinlagen  += e.Amount; break;
                    case "Darlehen":         k.Darlehen      += e.Amount; break;
                    case "Kredite":          k.Kredite       += e.Amount; break;
                    case "Sacheinlagen":     k.Sacheinlagen  += e.Amount; break;
                    case "Wertpapiere":      k.Wertpapiere   += e.Amount; break;
                    case "Börse / Krypto":   k.BoerseKrypto  += e.Amount; break;
                }
            }
        }

        var ausgList = db.getAllAusgabenEntries();
        if (ausgList != null)
        {
            k.AnzahlKassenbuchEintraege += ausgList.Count;
            foreach (var a in ausgList)
            {
                switch (a.getArt() ?? "")
                {
                    case "Gehälter":              gehaelter += a.Amount; break;
                    case "Sonstige Kosten":       sonstigeKostenEink += a.Amount; break;
                    case "Marketing":             marketing += a.Amount; break;
                    case "Reisekosten":           reisekosten += a.Amount; break;
                    case "Steuern":                steuern += a.Amount; break;
                    case "Finanzamt":              finanzamt += a.Amount; break;
                    case "Tilgungsraten":          tilgungsraten += a.Amount; break;
                    case "Barentnahme / Privatentnahme": privatentnahme += a.Amount; break;
                    case "Büroausstattung":
                    case "Fuhrpark":
                    case "Maschinen / Anlagen":
                    case "Software / Lizenzen":    k.SummeInvestition += a.Amount; break;
                    case "Corporate Design":
                    case "Homepage":
                    case "Grundausstattung":       k.SummeGruenderkosten += a.Amount; break;
                    case "Umlaufvermögen":          k.SummeUmlaufvermoegen += a.Amount; break;
                    default:                       sonstigeAusg += a.Amount; break;
                }
            }
        }

        float direkteKosten = gehaelter + sonstigeKostenEink + sonstigeAusg;
        k.Rohgewinn = k.GesamtEinnahmen - direkteKosten;
        k.Betriebsausgaben = marketing + reisekosten + steuern + finanzamt + tilgungsraten + privatentnahme;

        // Liquiditätsreserve = Umsatz + Sacheinlagen + Kredite + Darlehen +
        // Büroausstattung(+Investition gesamt) + Wertpapiere + Börse/Krypto
        k.Liquiditaetsreserve = k.Umsatzerloese + k.Sacheinlagen + k.Kredite + k.Darlehen
            + k.SummeInvestition + k.Wertpapiere + k.BoerseKrypto;

        // Kapitalbedarf = Anlagevermögen + Umlaufvermögen + Gründungskosten +
        // Sicherheitsreserve (identische Formel wie "Gesamtkapitalbedarf" in
        // Finanzen 2 - Liquiditaetsreserve hier entspricht dort "Sicherheitsreserve").
        k.Kapitalbedarf = k.SummeInvestition + k.SummeUmlaufvermoegen + k.SummeGruenderkosten + k.Liquiditaetsreserve;
        k.GesamtKapital = k.Geldeinlagen + k.Darlehen;

        return k;
    }
}
