using UnityEngine;
using UnityEngine.UIElements;
using SQLite4Unity3d;
using System.IO;

public class KassenbuchController : MonoBehaviour
{
    private VisualElement _overlay;
    private string _aktuellerTyp;
    private SQLiteConnection _db;

    void OnEnable()
    {
        string path = Path.Combine(Application.persistentDataPath, "kassenbuch.db");
        _db = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        _db.CreateTable<Einkommen>();
        _db.CreateTable<Ausgaben>();

        var root = GetComponent<UIDocument>().rootVisualElement;

        _overlay = root.Q<VisualElement>("popup-overlay");
        _overlay.RemoveFromHierarchy();
        root.Add(_overlay);
        _overlay.style.display = DisplayStyle.None;

        root.Q<Button>("btnAusgaben").clicked   += () => OpenPopup("Ausgabe");
        root.Q<Button>("btnEinnahmen").clicked  += () => OpenPopup("Einnahme");
        root.Q<Button>("btn-speichern").clicked += OnSpeichern;
        root.Q<Button>("btn-abbrechen").clicked += ClosePopup;
    }

    void OnDisable()
    {
        _db?.Close();
    }

    private void OpenPopup(string typ)
    {
        _aktuellerTyp = typ;
        _overlay.Q<Label>("popup-title").text = $"{typ} hinzufügen";
        _overlay.style.display = DisplayStyle.Flex;
    }

    private void ClosePopup()
    {
        _overlay.Q<TextField>("input-betrag").value          = "";
        _overlay.Q<TextField>("input-verwendungzweck").value = "";
        _overlay.Q<TextField>("input-datum").value           = "";
        _overlay.style.display = DisplayStyle.None;
    }

    private void OnSpeichern()
    {
        string betragText = _overlay.Q<TextField>("input-betrag").value.Trim();
        string zweck      = _overlay.Q<TextField>("input-verwendungzweck").value.Trim();
        string datum      = _overlay.Q<TextField>("input-datum").value.Trim();

        if (!float.TryParse(betragText, out float betrag) || string.IsNullOrEmpty(zweck) || string.IsNullOrEmpty(datum))
        {
            Debug.LogWarning("Ungültige Eingabe.");
            return;
        }

        var eintrag = new KassenbuchEintrag(_aktuellerTyp, betrag, zweck, datum);
        SpeichereEintrag(eintrag);
        ClosePopup();
    }

    private void SpeichereEintrag(KassenbuchEintrag eintrag)
    {
        if (eintrag.Typ == "Einnahme")
            _db.Insert(new Einkommen { Amount = eintrag.Betrag, Description = eintrag.Beschreibung , Datum = eintrag.Datum });
        else
            _db.Insert(new Ausgaben  { Amount = eintrag.Betrag, Description = eintrag.Beschreibung, Datum = eintrag.Datum });

        Debug.Log($"[DB] {eintrag.Typ} gespeichert: {eintrag.Betrag}€ – {eintrag.Beschreibung} ({eintrag.Datum})");
    }

    public void Loeschen(string typ, int id)
    {
        if (typ == "Einnahme") _db.Delete<Einkommen>(id);
        else                   _db.Delete<Ausgaben>(id);

        Debug.Log($"[DB] {typ} mit ID {id} gelöscht.");
    }
}

public class KassenbuchEintrag
{
    public string Typ;
    public float  Betrag;
    public string Beschreibung;
    public string Datum;

    public KassenbuchEintrag(string typ, float betrag, string beschreibung, string datum)
    {
        Typ          = typ;
        Betrag       = betrag;
        Beschreibung = beschreibung;
        Datum        = datum;
    }
}