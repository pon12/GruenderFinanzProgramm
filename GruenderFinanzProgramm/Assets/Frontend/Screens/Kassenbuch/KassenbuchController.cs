using UnityEngine;
using UnityEngine.UIElements;

public class KassenbuchController : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _overlay;
    private Button _btnAusgaben;
    private Button _btnEinnahmen;
    private Button _btnSpeichern;
    private Button _btnAbbrechen;

    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        _overlay      = _root.Q<VisualElement>("popup-overlay");
        _btnAusgaben  = _root.Q<Button>("btnAusgaben");
        _btnEinnahmen = _root.Q<Button>("btnEinnahmen");
        _btnSpeichern = _root.Q<Button>("btn-speichern");
        _btnAbbrechen = _root.Q<Button>("btn-abbrechen");

        // Overlay aus Flex-Row loesen und direkt auf root legen
        _overlay.RemoveFromHierarchy();
        _root.Add(_overlay);

        // Groesse und Zentrierung per inline style erzwingen
        _overlay.style.position   = Position.Absolute;
        _overlay.style.left       = 0;
        _overlay.style.top        = 0;
        _overlay.style.right      = 0;
        _overlay.style.bottom     = 0;
        _overlay.style.alignItems      = Align.Center;
        _overlay.style.justifyContent  = Justify.Center;

        _btnAusgaben.clicked  += () => OpenPopup("Ausgabe");
        _btnEinnahmen.clicked += () => OpenPopup("Einnahme");
        _btnAbbrechen.clicked += ClosePopup;
        _btnSpeichern.clicked += OnSpeichern;
    }

    private void OpenPopup(string typ)
    {
        _overlay.style.display = DisplayStyle.Flex;
        _overlay.Q<Label>("popup-title").text = typ + " hinzufügen";
    }

    private void ClosePopup()
    {
        _overlay.style.display = DisplayStyle.None;
        _overlay.Q<TextField>("input-betrag").value        = "";
        _overlay.Q<TextField>("input-verwendungzweck").value = "";
        _overlay.Q<TextField>("input-datum").value         = "";
    }

    private void OnSpeichern()
    {
        var betrag = _overlay.Q<TextField>("input-betrag").value;
        var zweck  = _overlay.Q<TextField>("input-verwendungzweck").value;
        var datum  = _overlay.Q<TextField>("input-datum").value;

        Debug.Log($"Gespeichert: {betrag} | {zweck} | {datum}");

        ClosePopup();
    }
}
