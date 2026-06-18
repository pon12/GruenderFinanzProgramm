using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DienstleistungenScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset popupAsset;

    private List<Service> daten = new List<Service>();

    private ScrollView tabelleBody;
    private VisualElement popupOverlay;
    private int editIndex = -1;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        tabelleBody  = root.Query<ScrollView>("tabelle-body").First();
        popupOverlay = root.Query<VisualElement>("popup-overlay").First();

        var btnNeu = root.Q<Button>("btn-neu");
        if (btnNeu == null) { Debug.LogError("btn-neu nicht gefunden!"); return; }

        btnNeu.clicked += () => OeffnePopup(-1);
        popupOverlay.RegisterCallback<ClickEvent>(evt => { if (evt.target == popupOverlay) SchliessPopup(); });

        BeispieldatenLaden();
        TabelleAktualisieren();
    }

    private void BeispieldatenLaden()
    {
        if (daten.Count > 0) return;
        daten.Add(new Service { name = "Beratung",       description = "Professionelle Unternehmensberatung", priceModel = "Festpreis",   price = 500.0 });
        daten.Add(new Service { name = "Webdesign",      description = "Modernes und responsives Webdesign",  priceModel = "Stundensatz", price = 80.0  });
        daten.Add(new Service { name = "Programmierung", description = "Individuelle Softwareentwicklung",    priceModel = "Festpreis",   price = 120.0 });
        daten.Add(new Service { name = "Logo",           description = "Kreative Logogestaltung",             priceModel = "Pauschal",    price = 300.0 });
        daten.Add(new Service { name = "Lizenskey",      description = "Softwarelizenzen & Aktivierungskeys", priceModel = "Festpreis",   price = 50.0  });
    }

    private void TabelleAktualisieren()
    {
        tabelleBody.Clear();

        for (int i = 0; i < daten.Count; i++)
        {
            int index = i;
            var d = daten[i];

            var zeile = new VisualElement();
            zeile.AddToClassList("tabelle-zeile");

            zeile.Add(Zelle(d.name,                         "col-titel"));
            zeile.Add(Zelle(d.description,                  "col-beschreibung"));
            zeile.Add(Zelle(d.priceModel,                   "col-preismodell"));
            zeile.Add(Zelle(d.price.ToString("F2") + " €", "col-betrag"));

            var aktionen = new VisualElement();
            aktionen.AddToClassList("col-aktionen");

            var btnEdit = new Button(() => OeffnePopup(index));
            btnEdit.text = "Bearbeiten";
            btnEdit.AddToClassList("zeile-btn");

            var btnDel = new Button(() => { daten.RemoveAt(index); TabelleAktualisieren(); });
            btnDel.text = "Löschen";
            btnDel.AddToClassList("zeile-btn");
            btnDel.AddToClassList("zeile-btn-loeschen");

            aktionen.Add(btnEdit);
            aktionen.Add(btnDel);
            zeile.Add(aktionen);

            tabelleBody.Add(zeile);
        }
    }

    private VisualElement Zelle(string text, string spaltenKlasse)
    {
        var label = new Label(text);
        label.AddToClassList("tabelle-zelle");
        label.AddToClassList(spaltenKlasse);
        return label;
    }

    private void OeffnePopup(int index)
    {
        editIndex = index;
        popupOverlay.Clear();

        var popup = popupAsset.Instantiate();

        var d = index >= 0 ? daten[index] : new Service { name = "", description = "", priceModel = "Festpreis", price = 0.0 };

        popup.Q<TextField>("feld-titel").value              = d.name;
        popup.Q<TextField>("feld-beschreibung").value       = d.description;
        popup.Q<DropdownField>("feld-preismodell").value    = d.priceModel;
        popup.Q<TextField>("feld-betrag").value             = d.price.ToString("F2");

        popup.Q<Button>("btn-fertig").clicked += () =>
        {
            double.TryParse(
                popup.Q<TextField>("feld-betrag").value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double betrag
            );

            var neu = new Service
            {
                name        = popup.Q<TextField>("feld-titel").value,
                description = popup.Q<TextField>("feld-beschreibung").value,
                priceModel  = popup.Q<DropdownField>("feld-preismodell").value,
                price       = betrag
            };

            if (editIndex >= 0) daten[editIndex] = neu;
            else                daten.Add(neu);

            SchliessPopup();
            TabelleAktualisieren();
        };

        popupOverlay.Add(popup);
        popupOverlay.style.display = DisplayStyle.Flex;
    }

    private void SchliessPopup()
    {
        popupOverlay.style.display = DisplayStyle.None;
        popupOverlay.Clear();
        editIndex = -1;
    }
}