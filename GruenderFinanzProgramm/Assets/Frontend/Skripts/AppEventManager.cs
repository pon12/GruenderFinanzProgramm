// ================================================================
// AppEventManager.cs  – statisch, kein GameObject nötig
// Ablegen unter: Assets/Frontend/Skripts/AppEventManager.cs
// ================================================================
public static class AppEventManager
{
    // Kunden – gefeuert von KundendatenbankController.RefreshKundenListe()
    public static event System.Action<int> OnKundenAnzahlGeaendert;
    public static void KundenAnzahlGeaendert(int anzahl)
        => OnKundenAnzahlGeaendert?.Invoke(anzahl);

    // Angebote – gefeuert von AngebotController (via BelegScreenController)
    public static event System.Action<int> OnAngeboteAnzahlGeaendert;
    public static void AngeboteAnzahlGeaendert(int anzahl)
        => OnAngeboteAnzahlGeaendert?.Invoke(anzahl);

    // Rechnungen – gefeuert von RechnungController (via BelegScreenController)
    public static event System.Action<int> OnRechnungenAnzahlGeaendert;
    public static void RechnungenAnzahlGeaendert(int anzahl)
        => OnRechnungenAnzahlGeaendert?.Invoke(anzahl);

    // Kassenbuch – gefeuert von KassenbuchController.createList()
    // umsatzJahr  = Summe aller Einkommen im aktuellen Jahr
    // kontostand  = getDifferenz()
    // monate      = float[12] für den Chart
    public static event System.Action<float, float, float[]> OnKassenbuchGeaendert;
    public static void KassenbuchGeaendert(float umsatzJahr, float kontostand, float[] monate)
        => OnKassenbuchGeaendert?.Invoke(umsatzJahr, kontostand, monate);
}
