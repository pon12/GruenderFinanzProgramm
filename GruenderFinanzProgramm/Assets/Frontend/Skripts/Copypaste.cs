using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CopyPaste : MonoBehaviour
{
    [Header("Referenzen auf die Eingabefelder")]
    public TMP_InputField benutzernameFeld;
    public TMP_InputField passwortFeld;
    // ---- KOPIEREN ----
    // An einen "Kopieren"-Button binden
    public void BenutzernameKopieren()
    {
        InZwischenablageKopieren(benutzernameFeld.text);
    }
    public void PasswortKopieren()
    {
        InZwischenablageKopieren(passwortFeld.text);
    }
    private void InZwischenablageKopieren(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        GUIUtility.systemCopyBuffer = text;
        Debug.Log("In Zwischenablage kopiert: " + text);
    }
    // ---- EINFÜGEN ----
    // An einen "Einfügen"-Button binden
    public void InBenutzernameEinfuegen()
    {
        AusZwischenablageEinfuegen(benutzernameFeld);
    }
    public void InPasswortEinfuegen()
    {
        AusZwischenablageEinfuegen(passwortFeld);
    }
    private void AusZwischenablageEinfuegen(TMP_InputField feld)
    {
        string zwischenablageText = GUIUtility.systemCopyBuffer;
        if (!string.IsNullOrEmpty(zwischenablageText))
        {
            feld.text = zwischenablageText;
        }
    }
}