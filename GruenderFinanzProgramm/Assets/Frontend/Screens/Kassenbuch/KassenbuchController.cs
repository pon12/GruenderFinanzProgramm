using UnityEngine;
using UnityEngine.UIElements;
using SQLite4Unity3d;
using System.IO;
using System;
using UnityEditor;
using System.Collections.Generic;
using iTextSharp.text;
using Unity.VisualScripting;

public class KassenbuchController : MonoBehaviour
{
    private VisualElement _overlay;
    private VisualElement tableInput;
    private VisualTreeAsset outputTemplate;

    private Label balanceLabel;

    private string _aktuellerTyp;
    //private SQLiteConnection _db;

    private DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

    void OnEnable()
    {
        //string path = Path.Combine(Application.persistentDataPath, "kassenbuch.db");
        //_db = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        //_db.CreateTable<Einkommen>();
        //_db.CreateTable<Ausgaben>();
        db = UserDatabaseAccess.getCurrentUserDatabase();
        outputTemplate = Resources.Load<VisualTreeAsset>("Kassenbuch_Field");

        var root = GetComponent<UIDocument>().rootVisualElement;

        _overlay = root.Q<VisualElement>("popup-overlay");
        tableInput = root.Q<VisualElement>("unity-content-container"); //tableBody

        balanceLabel = root.Q<Label>("balanceLabel");
        //newEntry = root.Q<VisualElement>("NewEntry");
        //newEntry.visible = false;
        _overlay.RemoveFromHierarchy();
        root.Add(_overlay);
        _overlay.style.display = DisplayStyle.None;

        root.Q<Button>("btnAusgaben").clicked   += () => OpenPopup("Ausgabe");
        root.Q<Button>("btnEinnahmen").clicked  += () => OpenPopup("Einnahme");
        root.Q<Button>("btn-speichern").clicked += OnSpeichern;
        root.Q<Button>("btn-abbrechen").clicked += ClosePopup;

        createList();
    }

    void OnDisable()
    {
        //_db?.Close();
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
        Debug.Log("Test: " + eintrag.Betrag + eintrag.Beschreibung);
        SpeichereEintrag(eintrag);
        ClosePopup();
        createList();
    }

    private void SpeichereEintrag(KassenbuchEintrag eintrag)
    {
        if (eintrag.Typ == "Einnahme")
            //_db.Insert(new Einkommen { Amount = eintrag.Betrag, Description = eintrag.Beschreibung , Datum = eintrag.Datum });
            db.createEinkommen(eintrag.Betrag, eintrag.Beschreibung, eintrag.Datum);
        else
            //db.Insert(new Ausgaben  { Amount = eintrag.Betrag, Description = eintrag.Beschreibung, Datum = eintrag.Datum });
            db.createAusgaben(eintrag.Betrag, eintrag.Beschreibung, eintrag.Datum);

        Debug.Log($"[DB] {eintrag.Typ} gespeichert: {eintrag.Betrag}€ – {eintrag.Beschreibung} ({eintrag.Datum})");
    }

    public void Loeschen(string typ, int id)
    {
        if (typ == "Einnahme") {
            //_db.Delete<Einkommen>(id);
            db.deleteEinkommen(id);
        }
        else {
            //_db.Delete<Ausgaben>(id);
            db.deleteEinkommen(id);
        }

        Debug.Log($"[DB] {typ} mit ID {id} gelöscht.");
    }


public void createList()
{


    balanceLabel.text = db.getDifferenz().ToString() + "€";

    tableInput.Clear();
    if (outputTemplate == null)
    {
        Debug.LogError("outputTemplate is null! Check the Resources path.");
        return;
    }

    if (tableInput == null)
    {
        Debug.LogError("tableInput is null! The scrollview wasn't found.");
        return;
    }

    List<Einkommen> einkommenList = db.getAllEinkommenEntries();
    List<Ausgaben> ausgabenList = db.getAllAusgabenEntries();
    if (einkommenList == null || einkommenList.Count == 0)
    {
        Debug.Log("No entries to display.");
        return;
    }

    foreach (Einkommen currentEinkommen in einkommenList)
    {
        VisualElement newEntryCopy = outputTemplate.Instantiate();
        
        Label nameLabel = newEntryCopy.Q<Label>("Name");
        Label typLabel = newEntryCopy.Q<Label>("Typ");
        Label betragLabel = newEntryCopy.Q<Label>("Betrag");
        Label erstellTagLabel = newEntryCopy.Q<Label>("ErstellTag");
        if (nameLabel == null)
        {
            Debug.LogError("Label 'Name' not found in template!");
            continue;
        }

        tableInput.Add(newEntryCopy);
        nameLabel.text = currentEinkommen.getDescription();
        betragLabel.text = currentEinkommen.getAmount();
        erstellTagLabel.text = currentEinkommen.getDatum();
        typLabel.text = "Einkommen";
        
        newEntryCopy.visible = true;
    }

        foreach (Ausgaben currentAusgaben in ausgabenList)
    {
        VisualElement newEntryCopy = outputTemplate.Instantiate();
        
        Label nameLabel = newEntryCopy.Q<Label>("Name");
        Label typLabel = newEntryCopy.Q<Label>("Typ");
        Label betragLabel = newEntryCopy.Q<Label>("Betrag");
        Label erstellTagLabel = newEntryCopy.Q<Label>("ErstellTag");
        if (nameLabel == null)
        {
            Debug.LogError("Label 'Name' not found in template!");
            continue;
        }

        tableInput.Add(newEntryCopy);
        nameLabel.text = currentAusgaben.getDescription();
            betragLabel.text = currentAusgaben.getAmount();
        erstellTagLabel.text = currentAusgaben.getDatum();
        typLabel.text = "Ausgabe";
        newEntryCopy.visible = true;
    }
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