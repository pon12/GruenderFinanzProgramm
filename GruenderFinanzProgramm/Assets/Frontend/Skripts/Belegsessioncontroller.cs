using System.Collections.Generic;

// Speichert den Formularstand beider Belegscreens im RAM für die aktuelle Programmsitzung.
// Wird beim Verlassen eines Screens gespeichert und beim Zurückkehren wiederhergestellt.
// Wird nach erfolgreichem Speichern oder Reset geleert.
public static class BelegSessionDaten
{
    public class PositionsEintrag
    {
        public string Artikel;
        public string Beschreibung;
        public string Menge;
        public string Einheit;
        public string Preis;
    }

    public class BelegSnapshot
    {
        public bool   HatDaten;
        public string Nummer;
        public string Status;
        public string Datum;
        public string Frist;
        public string Referenz;
        public string RabattTyp;
        public string RabattWert;
        public string SkontoWert;
        public string Notiz;
        public int    KundeId;
        public string KundeName;
        public string KundeAdresse;
        public List<PositionsEintrag> Positionen        = new List<PositionsEintrag>();
        public List<string>           AusgewaehlteAnhaenge = new List<string>();
    }

    private static readonly Dictionary<string, BelegSnapshot> _snapshots
        = new Dictionary<string, BelegSnapshot>();

    public static void Speichere(string belegTyp, BelegSnapshot snapshot)
    {
        snapshot.HatDaten    = true;
        _snapshots[belegTyp] = snapshot;
    }

    public static BelegSnapshot Lade(string belegTyp)
    {
        return _snapshots.TryGetValue(belegTyp, out var snap) && snap.HatDaten
            ? snap : null;
    }

    public static void Leere(string belegTyp)
    {
        _snapshots[belegTyp] = new BelegSnapshot { HatDaten = false };
    }
}
