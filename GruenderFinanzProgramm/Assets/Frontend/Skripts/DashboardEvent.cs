using System;

/// <summary>
/// Der zentrale Event-Manager. Er dient als Postbote zwischen den einzelnen 
/// Screens und dem Dashboard.
/// </summary>
public static class DashboardEvents
{
    // Das zentrale Event, das gefeuert wird, wenn sich irgendwelche Daten ändern
    public static event Action OnDataChanged;

    /// <summary>
    /// Diese Funktion wird von ANDEREN Screens aufgerufen, wenn dort Daten gespeichert wurden.
    /// </summary>
    public static void TriggerDataChanged()
    {
        // Wenn jemand das Event abonniert hat (z.B. das Dashboard), wird es jetzt benachrichtigt
        OnDataChanged?.Invoke();
    }
}