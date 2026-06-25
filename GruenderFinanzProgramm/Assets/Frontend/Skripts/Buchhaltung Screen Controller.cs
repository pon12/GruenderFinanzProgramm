using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BuchhaltungScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private static readonly Color Gruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color Rot   = new Color(230f / 255f,  57f / 255f,  70f / 255f);
    private static readonly Color Grau  = new Color(150f / 255f, 150f / 255f, 150f / 255f);

    private VisualElement _root;
    private ScrollView    _liste;

    private void Start()
    {
    if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    _root  = uiDocument.rootVisualElement;
    _liste = _root.Q<ScrollView>("buchhaltung-list-container");
    LadeEintraege(); 
    }


    private void LadeEintraege()
    {
        if (_liste == null) { Debug.LogError("[Buchhaltung] ScrollView nicht gefunden!"); return; }
        _liste.Clear();

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();

            if (db == null)
            {
                Debug.LogError("[Buchhaltung] db ist null");
                ZeigeLeermeldung("Keine Datenbankverbindung.");
                return;
            }

            Debug.Log("[Buchhaltung] DB gefunden, lade Angebote...");
            var angebote = db.getAllOffers() ?? new List<Offer>();
            Debug.Log("[Buchhaltung] Angebote: " + angebote.Count);


/*
            List<Invoice> rechnungen = new List<Invoice>();
            try
            {
                rechnungen = db.getAllInvoices() ?? new List<Invoice>();
                Debug.Log("[Buchhaltung] Rechnungen: " + rechnungen.Count);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Buchhaltung] getAllInvoices() fehlgeschlagen: " + e.Message);
            }
*/

            if (angebote.Count == 0)
            {
                ZeigeLeermeldung();
                return;
            }

            foreach (var angebot in angebote)
                _liste.Add(ErstelleZeile(
                    bezeichnung: "Angebot " + angebot.offerNumber + " – " + angebot.customerName,
                    erstellt:    angebot.date,
                    faellig:     angebot.validUntil,
                    status:      angebot.status
                ));
/*
            foreach (var rechnung in rechnungen)
                _liste.Add(ErstelleZeile(
                    bezeichnung: "Rechnung " + rechnung.invoiceNumber + " – " + rechnung.customerName,
                    erstellt:    rechnung.date,
                    faellig:     rechnung.dueDate,
                    status:      rechnung.status
                ));

*/                
        }
        catch (Exception e)
        {
            Debug.LogError("[Buchhaltung] " + e.Message);
            ZeigeLeermeldung("Fehler beim Laden der Einträge.");
        }
    }

    private VisualElement ErstelleZeile(string bezeichnung, string erstellt, string faellig, string status)
    {
        var zeile = new VisualElement();
        zeile.style.flexDirection   = FlexDirection.Row;
        zeile.style.alignItems      = Align.Center;
        zeile.style.paddingTop      = 10;
        zeile.style.paddingBottom   = 10;
        zeile.style.paddingLeft     = 12;
        zeile.style.paddingRight    = 12;
        zeile.style.marginBottom    = 4;
        zeile.style.backgroundColor = new Color(55f / 255f, 55f / 255f, 55f / 255f);
        zeile.style.borderTopLeftRadius     = 8;
        zeile.style.borderTopRightRadius    = 8;
        zeile.style.borderBottomLeftRadius  = 8;
        zeile.style.borderBottomRightRadius = 8;

        zeile.RegisterCallback<MouseEnterEvent>(_ =>
            zeile.style.backgroundColor = new Color(65f / 255f, 65f / 255f, 65f / 255f));
        zeile.RegisterCallback<MouseLeaveEvent>(_ =>
            zeile.style.backgroundColor = new Color(55f / 255f, 55f / 255f, 55f / 255f));

        // Bezeichnung (flex-grow, nimmt restlichen Platz)
        var lblBezeichnung = new Label(bezeichnung);
        lblBezeichnung.AddToClassList("col-bezeichnung");
        lblBezeichnung.style.color     = Color.white;
        lblBezeichnung.style.fontSize  = 13;
        lblBezeichnung.style.flexGrow  = 1;
        lblBezeichnung.style.overflow  = Overflow.Hidden;
        lblBezeichnung.style.whiteSpace = WhiteSpace.NoWrap;

        // Erstellt
        var lblErstellt = new Label(erstellt);
        lblErstellt.AddToClassList("col-erstellt");
        lblErstellt.style.color    = Grau;
        lblErstellt.style.fontSize = 12;
        lblErstellt.style.width    = 110;

        // Fällig
        var lblFaellig = new Label(faellig);
        lblFaellig.AddToClassList("col-faellig");
        lblFaellig.style.color    = Grau;
        lblFaellig.style.fontSize = 12;
        lblFaellig.style.width    = 110;

        // Status-Badge
        var statusBadge = new Label(status);
        statusBadge.AddToClassList("col-status");
        statusBadge.style.fontSize  = 11;
        statusBadge.style.width     = 100;
        statusBadge.style.paddingTop    = 3;
        statusBadge.style.paddingBottom = 3;
        statusBadge.style.paddingLeft   = 8;
        statusBadge.style.paddingRight  = 8;
        statusBadge.style.borderTopLeftRadius     = 20;
        statusBadge.style.borderTopRightRadius    = 20;
        statusBadge.style.borderBottomLeftRadius  = 20;
        statusBadge.style.borderBottomRightRadius = 20;
        statusBadge.style.unityTextAlign = TextAnchor.MiddleCenter;
        statusBadge.style.backgroundColor = HoleStatusFarbe(status);
        statusBadge.style.color = new Color(0.1f, 0.1f, 0.1f);

        zeile.Add(lblBezeichnung);
        zeile.Add(lblErstellt);
        zeile.Add(lblFaellig);
        zeile.Add(statusBadge);

        return zeile;
    }

    private static Color HoleStatusFarbe(string status)
    {
        return status switch
        {
            "Angenommen" or "Bezahlt"  => Gruen,
            "Abgelehnt"  or "Überfällig" => Rot,
            _ => new Color(180f / 255f, 180f / 255f, 180f / 255f) // Entwurf / Ausstehend
        };
    }

    private void ZeigeLeermeldung(string text = "Noch keine Angebote oder Rechnungen vorhanden.")
    {
        var hinweis = new Label(text);
        hinweis.style.color          = Grau;
        hinweis.style.fontSize       = 14;
        hinweis.style.unityTextAlign = TextAnchor.MiddleCenter;
        hinweis.style.marginTop      = 40;
        hinweis.style.flexGrow       = 1;
        _liste.Add(hinweis);
    }
}