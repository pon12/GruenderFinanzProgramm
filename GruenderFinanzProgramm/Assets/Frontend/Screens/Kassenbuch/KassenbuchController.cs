using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public class KassenbuchController : MonoBehaviour
{
    private VisualElement _overlay;
    private VisualElement tableInput;
    private VisualTreeAsset outputTemplate;

    private Label balanceLabel;

    private string _aktuellerTyp;
    private DataBase db;

    void OnEnable()
    {
        db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            Debug.LogError("[Kassenbuch] Keine aktive Nutzer-Datenbank gefunden.");
            return;
        }

        db.setupKassenbuchTable();

        outputTemplate = Resources.Load<VisualTreeAsset>("Kassenbuch_Field");

        var root = GetComponent<UIDocument>().rootVisualElement;

        _overlay = root.Q<VisualElement>("popup-overlay");
        tableInput = root.Q<VisualElement>("unity-content-container");
        balanceLabel = root.Q<Label>("balanceLabel");

        if (_overlay == null)
        {
            Debug.LogError("[Kassenbuch] popup-overlay nicht gefunden.");
            return;
        }

        if (tableInput == null)
        {
            Debug.LogError("[Kassenbuch] unity-content-container nicht gefunden.");
            return;
        }

        if (balanceLabel == null)
        {
            Debug.LogError("[Kassenbuch] balanceLabel nicht gefunden.");
            return;
        }

        _overlay.RemoveFromHierarchy();
        root.Add(_overlay);
        _overlay.style.display = DisplayStyle.None;

        root.Q<Button>("btnAusgaben").clicked += () => OpenPopup("Ausgabe");
        root.Q<Button>("btnEinnahmen").clicked += () => OpenPopup("Einnahme");
        root.Q<Button>("btn-speichern").clicked += OnSpeichern;
        root.Q<Button>("btn-abbrechen").clicked += ClosePopup;
        Button sortDatumAscButton = root.Q<Button>("btn-sort-datum-asc");
        Button sortDatumDescButton = root.Q<Button>("btn-sort-datum-desc");
        Button sortBetragAscButton = root.Q<Button>("btn-sort-betrag-asc");
        Button sortBetragDescButton = root.Q<Button>("btn-sort-betrag-desc");
        Button sortTypButton = root.Q<Button>("btn-sort-typ");

        if (sortDatumAscButton != null)
            sortDatumAscButton.clicked += sortiereNachDatumAufsteigend;

        if (sortDatumDescButton != null)
            sortDatumDescButton.clicked += sortiereNachDatumAbsteigend;

        if (sortBetragAscButton != null)
            sortBetragAscButton.clicked += sortiereNachBetragAufsteigend;

        if (sortBetragDescButton != null)
            sortBetragDescButton.clicked += sortiereNachBetragAbsteigend;

        if (sortTypButton != null)
            sortTypButton.clicked += sortiereNachTyp;

        createList();
    }

    void OnDisable()
    {
        // aktuell nichts nötig
    }

    private void OpenPopup(string typ)
    {
        _aktuellerTyp = typ;
        _overlay.Q<Label>("popup-title").text = $"{typ} hinzufügen";
        _overlay.style.display = DisplayStyle.Flex;
    }

    private void ClosePopup()
    {
        _overlay.Q<TextField>("input-betrag").value = "";
        _overlay.Q<TextField>("input-verwendungzweck").value = "";
        _overlay.Q<TextField>("input-datum").value = "";
        _overlay.style.display = DisplayStyle.None;
    }

    private void OnSpeichern()
    {
        string betragText = _overlay.Q<TextField>("input-betrag").value.Trim();
        string zweck = _overlay.Q<TextField>("input-verwendungzweck").value.Trim();
        string datum = _overlay.Q<TextField>("input-datum").value.Trim();

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
        {
            db.createEinkommen(eintrag.Betrag, eintrag.Beschreibung, eintrag.Datum);
        }
        else
        {
            db.createAusgaben(eintrag.Betrag, eintrag.Beschreibung, eintrag.Datum);
        }

        Debug.Log($"[DB] {eintrag.Typ} gespeichert: {eintrag.Betrag}€ – {eintrag.Beschreibung} ({eintrag.Datum})");
    }

    public void Loeschen(string typ, int id)
    {
        if (typ == "Einnahme")
        {
            db.deleteEinkommen(id);
        }
        else
        {
            db.deleteAusgaben(id);
        }

        createList();

        Debug.Log($"[DB] {typ} mit ID {id} gelöscht.");
    }

    public void createList()
    {
        createList(getCombinedEntries());
    }

    private void createList(List<KassenbuchEintrag> entries)
    {
        float differenz = db.getDifferenz();
        balanceLabel.text = differenz.ToString() + "€";

        balanceLabel.style.color = differenz < 0
            ? new StyleColor(new UnityEngine.Color(230f / 255f, 57f / 255f, 70f / 255f))
            : new StyleColor(new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f));

        tableInput.Clear();

        if (outputTemplate == null)
        {
            Debug.LogError("outputTemplate is null!");
            return;
        }

        if (tableInput == null)
        {
            Debug.LogError("tableInput is null!");
            return;
        }

        foreach (KassenbuchEintrag entry in entries)
        {
            VisualElement newEntryCopy = outputTemplate.Instantiate();

            Label nameLabel = newEntryCopy.Q<Label>("Name");
            Label typLabel = newEntryCopy.Q<Label>("Typ");
            Label betragLabel = newEntryCopy.Q<Label>("Betrag");
            Label erstellTagLabel = newEntryCopy.Q<Label>("ErstellTag");
            Button loeschenBtn = newEntryCopy.Q<Button>("BtnLoeschen");

            if (nameLabel == null)
            {
                Debug.LogError("Label 'Name' nicht gefunden!");
                continue;
            }

            nameLabel.text = entry.Beschreibung;
            betragLabel.text = entry.Betrag.ToString() + "€";
            erstellTagLabel.text = entry.Datum;
            typLabel.text = entry.Typ;

            if (betragLabel != null)
            {
                betragLabel.style.color = entry.Typ == "Einnahme"
                    ? new StyleColor(new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f))
                    : new StyleColor(new UnityEngine.Color(230f / 255f, 57f / 255f, 70f / 255f));
            }

            if (loeschenBtn != null)
            {
                int id = entry.Id;
                string typ = entry.Typ;
                loeschenBtn.clicked += () => Loeschen(typ, id);
            }

            tableInput.Add(newEntryCopy);
            newEntryCopy.visible = true;
        }
    }

    public void deleteEntry(int id)
    {
        foreach (Einkommen currentEinkommen in db.getAllEinkommenEntries())
        {
            if (currentEinkommen.getId() == id)
            {
                db.deleteEinkommen(id);
                createList();
                return;
            }
        }

        foreach (Ausgaben currentAusgaben in db.getAllAusgabenEntries())
        {
            if (currentAusgaben.getId() == id)
            {
                db.deleteAusgaben(id);
                createList();
                return;
            }
        }
    }

    private List<KassenbuchEintrag> getCombinedEntries()
    {
        List<KassenbuchEintrag> entries = new List<KassenbuchEintrag>();

        foreach (Einkommen einkommen in db.getAllEinkommenEntries())
        {
            entries.Add(new KassenbuchEintrag(
                einkommen.getId(),
                "Einnahme",
                einkommen.Amount,
                einkommen.Description,
                einkommen.Datum
            ));
        }

        foreach (Ausgaben ausgabe in db.getAllAusgabenEntries())
        {
            entries.Add(new KassenbuchEintrag(
                ausgabe.getId(),
                "Ausgabe",
                ausgabe.Amount,
                ausgabe.Description,
                ausgabe.Datum
            ));
        }

        return entries;
    }

    // --------------------------------------------------
    // Sortierfunktionen
    // Diese Methoden können später direkt an Buttons gehangen werden.
    // --------------------------------------------------

    public void sortiereNachDatumAufsteigend()
    {
        createList(sortByDate(false));
    }

    public void sortiereNachDatumAbsteigend()
    {
        createList(sortByDate(true));
    }

    public void sortiereNachBetragAufsteigend()
    {
        createList(sortByAmount(false));
    }

    public void sortiereNachBetragAbsteigend()
    {
        createList(sortByAmount(true));
    }

    public void sortiereNachTyp()
    {
        createList(sortByType());
    }

    public void sortiereNachBeschreibung()
    {
        createList(sortByDescription());
    }

    private List<KassenbuchEintrag> sortByAmount(bool descending)
    {
        List<KassenbuchEintrag> entries = getCombinedEntries();

        return descending
            ? entries.OrderByDescending(e => e.Betrag).ToList()
            : entries.OrderBy(e => e.Betrag).ToList();
    }

    private List<KassenbuchEintrag> sortByType()
    {
        return getCombinedEntries()
            .OrderBy(e => e.Typ)
            .ToList();
    }

    private List<KassenbuchEintrag> sortByDescription()
    {
        return getCombinedEntries()
            .OrderBy(e => e.Beschreibung)
            .ToList();
    }

    private List<KassenbuchEintrag> sortByDate(bool descending)
    {
        List<KassenbuchEintrag> entries = getCombinedEntries();

        return descending
            ? entries.OrderByDescending(e => parseDate(e.Datum)).ToList()
            : entries.OrderBy(e => parseDate(e.Datum)).ToList();
    }

    private System.DateTime parseDate(string datum)
    {
        if (System.DateTime.TryParseExact(
            datum,
            "dd.MM.yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out System.DateTime parsedDate))
        {
            return parsedDate;
        }

        return System.DateTime.MinValue;
    }
}

public class KassenbuchEintrag
{
    public int Id;
    public string Typ;
    public float Betrag;
    public string Beschreibung;
    public string Datum;

    public KassenbuchEintrag(string typ, float betrag, string beschreibung, string datum)
    {
        Id = 0;
        Typ = typ;
        Betrag = betrag;
        Beschreibung = beschreibung;
        Datum = datum;
    }

    public KassenbuchEintrag(int id, string typ, float betrag, string beschreibung, string datum)
    {
        Id = id;
        Typ = typ;
        Betrag = betrag;
        Beschreibung = beschreibung;
        Datum = datum;
    }
}