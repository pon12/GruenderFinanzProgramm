// DocumentDashboard.cs – Dokumente-Pool
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DocumentDashboard : MonoBehaviour
{
    [Header("UI Document Asset")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Templates")]
    [SerializeField] private VisualTreeAsset categoryCardTemplate;

    // UI-Elemente Hauptbildschirm
    private VisualElement root;
    private Button        deleteButton;
    private VisualElement gridContainer;

    // Erstell-Popup (nur für flexible Kategorien)
    private VisualElement popupOverlay;
    private Button        popupCancelButton;
    private Button        popupSubmitButton;
    private DropdownField categoryDropdown;
    private TextField     docNameInput;
    private Button        btnTypeStandard;
    private Button        btnTypeDiagramm;
    private Button        btnTypeChecklist;

    // Kategorie-Listen-Popup
    private VisualElement detailPopupOverlay;
    private VisualElement detailListContainer;
    private VisualElement globalListContainer;
    private Button        detailCloseButton;
    private Label         detailPopupTitle;
    private Button        listCreateNewButton;

    // Bearbeiten-Popup (flexible Dokumente: Titel + Freitext)
    private VisualElement editPopupOverlay;
    private TextField     editDocNameInput;
    private TextField     editInhaltInput;
    private Button        editPopupSubmitButton;
    private Button        editPopupCancelButton;
    private Button        btnEditTypeStandard;
    private Button        btnEditTypeDiagramm;
    private Button        btnEditTypeChecklist;
    private Label         editLockedHint;
    private VisualElement editTemplateGroup;
    private VisualElement editStrukturFelderBox;

    // Lösch-Bestätigungs-Popup
    private VisualElement deleteConfirmOverlay;
    private Button        deleteConfirmYesButton;
    private Button        deleteConfirmCancelButton;
    private Label         deleteConfirmHint;

    // Systemzustände
    private string       selectedType          = "Standard";
    private string       selectedEditType       = "Standard";
    private string       activeCategoryForList  = "";
    private DocumentData  activeDocForEditing;
    private List<TextField> aktiveStrukturFelder = new List<TextField>();

    // ============================================================
    // FELD-DEFINITION – ein einzelnes strukturiertes Eingabefeld
    // ============================================================
    private class FeldDefinition
    {
        public string key;         // interner Schlüssel, z.B. "iban"
        public string label;       // Anzeigename, z.B. "IBAN"
        public string placeholder; // Platzhaltertext im Feld
    }

    // ============================================================
    // KATEGORIEN-DEFINITION
    //
    // istFest = true -> nicht löschbar, nicht verschiebbar.
    //                    Werte bleiben editierbar.
    // pflichtDocs    -> Pflichtdokumente dieser Kategorie, jeweils
    //                    mit eigener Feldstruktur (felderProDoc).
    // ============================================================
    private class KategorieDefinition
    {
        public string       name;
        public bool         istFest;
        public List<string> pflichtDocs;
    }

    private readonly List<KategorieDefinition> kategorien = new List<KategorieDefinition>
    {
        new KategorieDefinition { name = "Gründung",   istFest = true,  pflichtDocs = new List<string> { "Unternehmensstammdaten", "Gründungsurkunde", "Handelsregisterauszug" } },
        new KategorieDefinition { name = "Bezahlweise", istFest = true,  pflichtDocs = new List<string> { "Kontodaten (IBAN/BIC)", "Zahlungsbedingungen", "AGB", "Disclaimer", "Barzahlung", "Überweisung" } },
        new KategorieDefinition { name = "Finanzen",    istFest = false, pflichtDocs = new List<string>() },
        new KategorieDefinition { name = "Marketing",   istFest = false, pflichtDocs = new List<string>() },
        new KategorieDefinition { name = "Steuern",     istFest = false, pflichtDocs = new List<string>() },
        new KategorieDefinition { name = "Personal",    istFest = false, pflichtDocs = new List<string>() },
        new KategorieDefinition { name = "Recht",       istFest = false, pflichtDocs = new List<string>() },
    };

    // ============================================================
    // FELD-DEFINITIONEN PRO PFLICHTDOKUMENT
    //
    // Key muss exakt dem Dokumenttitel in pflichtDocs entsprechen.
    // ============================================================
    private readonly Dictionary<string, List<FeldDefinition>> felderProPflichtDoc =
        new Dictionary<string, List<FeldDefinition>>
    {
        ["Unternehmensstammdaten"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "firma",     label = "Firmenname", placeholder = "z.B. Mustermann GmbH" },
            new FeldDefinition { key = "rechtsform", label = "Rechtsform", placeholder = "z.B. GmbH" },
            new FeldDefinition { key = "branche",    label = "Branche",   placeholder = "z.B. IT & Software" },
            new FeldDefinition { key = "standort",   label = "Standort",  placeholder = "z.B. Berlin" },
        },
        ["Gründungsurkunde"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "datum",        label = "Gründungsdatum", placeholder = "TT.MM.JJJJ" },
            new FeldDefinition { key = "notar",        label = "Notar",          placeholder = "Name des Notars" },
            new FeldDefinition { key = "aktenzeichen", label = "Aktenzeichen",   placeholder = "z.B. UR-Nr. 123/2026" },
        },
        ["Handelsregisterauszug"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "hrNummer",      label = "HR-Nummer",   placeholder = "z.B. HRB 12345" },
            new FeldDefinition { key = "amtsgericht",   label = "Amtsgericht", placeholder = "z.B. Amtsgericht Berlin" },
            new FeldDefinition { key = "eintragsdatum", label = "Eintragsdatum", placeholder = "TT.MM.JJJJ" },
        },
        ["Kontodaten (IBAN/BIC)"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "iban",         label = "IBAN",         placeholder = "DE00 0000 0000 0000 0000 00" },
            new FeldDefinition { key = "bic",          label = "BIC",          placeholder = "z.B. COBADEFFXXX" },
            new FeldDefinition { key = "bank",         label = "Bank",         placeholder = "z.B. Commerzbank" },
            new FeldDefinition { key = "kontoinhaber", label = "Kontoinhaber", placeholder = "Name laut Konto" },
        },
        ["Zahlungsbedingungen"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "zahlungsziel", label = "Zahlungsziel (Tage)", placeholder = "z.B. 14" },
            new FeldDefinition { key = "skonto",       label = "Skonto (%)",          placeholder = "z.B. 2" },
            new FeldDefinition { key = "mahnstufe",    label = "Mahnstufe",           placeholder = "z.B. 1. Mahnung nach 7 Tagen" },
        },
    };

    private bool IstKategorieFest(string kategorieName)
        => kategorien.Find(k => k.name == kategorieName)?.istFest ?? false;

    [System.Serializable]
    public class StrukturFeldWert
    {
        public string key;
        public string wert;
    }

    [System.Serializable]
    public class DocumentData
    {
        public string id;
        public string category;
        public string title;
        public string type;
        public bool   istPflichtdokument;
        public string inhalt;
        public List<StrukturFeldWert> strukturFelder;
    }

    [System.Serializable]
    public class DocumentSaveData
    {
        public List<DocumentData> savedDocs = new List<DocumentData>();
    }

    private DocumentSaveData speicherDaten = new DocumentSaveData();
    private string           saveFilePath;

    // ─────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────

    void OnEnable()
    {
        saveFilePath = Application.persistentDataPath + "/MyDashboardSave.json";

        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        // 1. Hauptbildschirm
        deleteButton  = root.Q<Button>("Delete-Button");
        gridContainer = root.Q<VisualElement>("Grid-Container");

        // 2. Erstell-Popup (nur flexible Kategorien)
        popupOverlay      = root.Q<VisualElement>("Popup-Overlay");
        popupCancelButton = root.Q<Button>("Btn-Cancel");
        var btnAbbrechen  = root.Q<Button>("Btn-Abbrechen");
        if (btnAbbrechen != null) btnAbbrechen.clicked += ClosePopup;
        popupSubmitButton = root.Q<Button>("Btn-Submit");
        categoryDropdown  = root.Q<DropdownField>("dropKategorie");
        docNameInput      = root.Q<TextField>("Doc-Name-Input");

        // 3. Zweispaltiges Listen-Popup
        detailPopupOverlay  = root.Q<VisualElement>("Detail-Popup-Overlay");
        detailListContainer = root.Q<VisualElement>("Detail-List-Container");
        globalListContainer = root.Q<VisualElement>("Global-List-Container");
        detailCloseButton   = root.Q<Button>("Btn-Detail-Close");
        detailPopupTitle    = root.Q<Label>("Detail-Popup-Title");
        listCreateNewButton = root.Q<Button>("Btn-List-Create-New");

        // 4. Bearbeiten-Popup
        editPopupOverlay      = root.Q<VisualElement>("Edit-Popup-Overlay");
        editDocNameInput      = root.Q<TextField>("Edit-Doc-Name-Input");
        editInhaltInput       = root.Q<TextField>("Edit-Inhalt-Input");
        editPopupSubmitButton = root.Q<Button>("Btn-Edit-Submit");
        editPopupCancelButton = root.Q<Button>("Btn-Edit-Cancel");
        btnEditTypeStandard   = root.Q<Button>("Btn-Edit-Type-Standard");
        btnEditTypeDiagramm   = root.Q<Button>("Btn-Edit-Type-Diagramm");
        btnEditTypeChecklist  = root.Q<Button>("Btn-Edit-Type-Checklist");
        editLockedHint        = root.Q<Label>("Edit-Locked-Hint");
        editTemplateGroup     = root.Q<VisualElement>("Edit-Buttons-Type-Group");
        editStrukturFelderBox = root.Q<VisualElement>("Edit-Struktur-Felder-Box");

        // 5. Lösch-Bestätigungs-Popup
        deleteConfirmOverlay      = root.Q<VisualElement>("Delete-Confirm-Overlay");
        deleteConfirmYesButton    = root.Q<Button>("Btn-Delete-Confirm-Yes");
        deleteConfirmCancelButton = root.Q<Button>("Btn-Delete-Confirm-Cancel");
        deleteConfirmHint         = root.Q<Label>("Delete-Confirm-Hint");

        // Dropdown befüllen (nur flexible Kategorien)
        if (categoryDropdown != null)
        {
            var namen = kategorien.Select(k => k.name).ToList();
            categoryDropdown.choices = namen;
            if (namen.Count > 0)
                categoryDropdown.value = namen[0];
        }

        // Event-Verdrahtung Hauptmenü
        if (deleteButton != null) deleteButton.clicked += OpenDeleteConfirmPopup;

        // Event-Verdrahtung Erstell-Popup
        if (popupCancelButton != null) popupCancelButton.clicked += ClosePopup;
        if (popupSubmitButton != null) popupSubmitButton.clicked += CreateNewDocumentEntry;

        btnTypeStandard  = root.Q<Button>("Btn-Type-Standard");
        btnTypeDiagramm  = root.Q<Button>("Btn-Type-Diagramm");
        btnTypeChecklist = root.Q<Button>("Btn-Type-Checklist");

        if (btnTypeStandard  != null) btnTypeStandard.clicked  += () => ApplyTemplate("Standard");
        if (btnTypeDiagramm  != null) btnTypeDiagramm.clicked  += () => ApplyTemplate("Diagramm");
        if (btnTypeChecklist != null) btnTypeChecklist.clicked += () => ApplyTemplate("Checklist");

        // Event-Verdrahtung Listen-Popup
        if (detailCloseButton   != null) detailCloseButton.clicked += CloseDetailPopup;
        if (listCreateNewButton != null)
            listCreateNewButton.clicked += () =>
            {
                CloseDetailPopup();
                OpenPopup(activeCategoryForList);
            };

        // Event-Verdrahtung Bearbeiten-Popup
        if (editPopupCancelButton != null) editPopupCancelButton.clicked += CloseEditPopup;
        if (editPopupSubmitButton != null) editPopupSubmitButton.clicked += SaveEditedDocumentEntry;
        if (btnEditTypeStandard  != null) btnEditTypeStandard.clicked   += () => SelectEditType("Standard");
        if (btnEditTypeDiagramm  != null) btnEditTypeDiagramm.clicked   += () => SelectEditType("Diagramm");
        if (btnEditTypeChecklist != null) btnEditTypeChecklist.clicked  += () => SelectEditType("Checklist");

        // Event-Verdrahtung Lösch-Bestätigung
        if (deleteConfirmCancelButton != null) deleteConfirmCancelButton.clicked += CloseDeleteConfirmPopup;
        if (deleteConfirmYesButton    != null) deleteConfirmYesButton.clicked    += ConfirmDeleteAllDocuments;

        LoadDataLocally();
        SicherePflichtdokumente();
        SpawnAllCardsAtStart();
    }

    // ─────────────────────────────────────────
    // PFLICHTDOKUMENTE SICHERSTELLEN
    // ─────────────────────────────────────────
    private void SicherePflichtdokumente()
    {
        bool geaendert = false;

        foreach (var kategorie in kategorien.Where(k => k.istFest))
        {
            foreach (string pflichtTitel in kategorie.pflichtDocs)
            {
                var bestehendesDoc = speicherDaten.savedDocs.FirstOrDefault(d =>
                    d.category == kategorie.name &&
                    d.istPflichtdokument &&
                    d.title == pflichtTitel);

                if (bestehendesDoc == null)
                {
                    var neuesDoc = new DocumentData
                    {
                        id                 = System.Guid.NewGuid().ToString(),
                        category           = kategorie.name,
                        title              = pflichtTitel,
                        type               = "Standard",
                        istPflichtdokument = true,
                        inhalt             = "",
                        strukturFelder     = ErzeugeLeereStrukturFelder(pflichtTitel)
                    };
                    speicherDaten.savedDocs.Add(neuesDoc);
                    geaendert = true;
                }
                else if (bestehendesDoc.strukturFelder == null || bestehendesDoc.strukturFelder.Count == 0)
                {
                    bestehendesDoc.strukturFelder = ErzeugeLeereStrukturFelder(pflichtTitel);
                    geaendert = true;
                }
                else
                {
                    // Neue Felder ergänzen, falls die Definition erweitert wurde
                    if (felderProPflichtDoc.TryGetValue(pflichtTitel, out var definitionen))
                    {
                        foreach (var def in definitionen)
                        {
                            bool vorhanden = bestehendesDoc.strukturFelder.Any(f => f.key == def.key);
                            if (!vorhanden)
                            {
                                bestehendesDoc.strukturFelder.Add(new StrukturFeldWert { key = def.key, wert = "" });
                                geaendert = true;
                            }
                        }
                    }
                }
            }
        }

        if (geaendert) SaveDataLocally();
    }

    private List<StrukturFeldWert> ErzeugeLeereStrukturFelder(string dokumentTitel)
    {
        var ergebnis = new List<StrukturFeldWert>();
        if (felderProPflichtDoc.TryGetValue(dokumentTitel, out var definitionen))
        {
            foreach (var def in definitionen)
                ergebnis.Add(new StrukturFeldWert { key = def.key, wert = "" });
        }
        return ergebnis;
    }

    // ─────────────────────────────────────────
    // ERSTELLLOGIK (nur für flexible Kategorien)
    // ─────────────────────────────────────────

    private void OpenPopup(string preselectedCategory = "")
    {
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.Flex;
        if (docNameInput != null) docNameInput.value         = "";
        selectedType = "Standard";
        MarkiereAusgewaehlteVorlage(btnTypeStandard, btnTypeDiagramm, btnTypeChecklist, selectedType);

        if (!string.IsNullOrEmpty(preselectedCategory) && categoryDropdown != null)
            if (kategorien.Any(k => k.name == preselectedCategory && !k.istFest))
                categoryDropdown.value = preselectedCategory;
    }

    private void ClosePopup()
    {
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.None;
    }

    private void ApplyTemplate(string typeName)
    {
        selectedType = typeName;
        MarkiereAusgewaehlteVorlage(btnTypeStandard, btnTypeDiagramm, btnTypeChecklist, selectedType);
    }

    private void CreateNewDocumentEntry()
    {
        string selectedCategory = categoryDropdown != null ? categoryDropdown.value : "";

        string docText = docNameInput != null ? docNameInput.value.Trim() : "";
        if (string.IsNullOrEmpty(docText))
        {
            if (docNameInput != null)
            {
                docNameInput.AddToClassList("input-error");
                docNameInput.schedule.Execute(() => docNameInput.RemoveFromClassList("input-error")).ExecuteLater(1200);
            }
            return;
        }

        if (string.IsNullOrEmpty(selectedCategory)) return;

        DocumentData newDoc = new DocumentData
        {
            id                 = System.Guid.NewGuid().ToString(),
            category           = selectedCategory,
            title              = docText,
            type               = selectedType,
            istPflichtdokument = false,
            inhalt             = "",
            strukturFelder     = new List<StrukturFeldWert>()
        };

        speicherDaten.savedDocs.Add(newDoc);
        SaveDataLocally();
        SpawnAllCardsAtStart();
        ClosePopup();
    }

    // ─────────────────────────────────────────
    // DASHBOARD-KACHELN
    // ─────────────────────────────────────────

    private void SpawnAllCardsAtStart()
    {
        if (gridContainer == null || categoryCardTemplate == null) return;
        gridContainer.Clear();

        foreach (var kategorie in kategorien)
        {
            VisualElement cardInstance = categoryCardTemplate.Instantiate();

            Label titleLabel = cardInstance.Q<Label>("lblName");
            if (titleLabel != null) titleLabel.text = kategorie.name;

            VisualElement lockBadge = cardInstance.Q<VisualElement>("lock-badge");
            if (lockBadge != null)
                lockBadge.style.display = kategorie.istFest ? DisplayStyle.Flex : DisplayStyle.None;

            VisualElement imgShowList = cardInstance.Q("Btn-Show-List");
            if (imgShowList != null)
                imgShowList.RegisterCallback<ClickEvent>(evt => OpenDetailPopup(kategorie.name));

            Button alleAnzeigenBtn = cardInstance.Q<Button>("btnPlus");
            if (alleAnzeigenBtn != null) alleAnzeigenBtn.clicked += () => OpenDetailPopup(kategorie.name);

            List<DocumentData> kategorieDocs =
                speicherDaten.savedDocs.FindAll(d => d.category == kategorie.name);

            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                VisualElement contentBox = cardInstance.Q<VisualElement>($"Slot-{slotIndex}-Content");
                VisualElement iconBox    = cardInstance.Q<VisualElement>($"Slot-{slotIndex}-Icon");
                Button        plusBtn    = cardInstance.Q<Button>($"Slot-{slotIndex}-Plus");

                if (contentBox == null) continue;
                contentBox.Clear();

                bool hatDokument = slotIndex < kategorieDocs.Count;

                if (hatDokument)
                {
                    var aktuellesDoc = kategorieDocs[slotIndex];

                    string iconGlyph = aktuellesDoc.istPflichtdokument ? "🔒" :
                                        aktuellesDoc.type == "Diagramm"  ? "📊" :
                                        aktuellesDoc.type == "Checklist" ? "✅" : "📄";
                    if (iconBox != null)
                    {
                        iconBox.Clear();
                        var iconLabel = new Label(iconGlyph);
                        iconLabel.style.fontSize        = 13;
                        iconLabel.style.unityTextAlign  = TextAnchor.MiddleCenter;
                        iconLabel.style.flexGrow        = 1;
                        iconBox.Add(iconLabel);
                    }

                    string titel = aktuellesDoc.title.Split('\n')[0];
                    if (titel.Length > 22) titel = titel.Substring(0, 19) + "...";
                    Label titelLabel = new Label(titel);
                    titelLabel.AddToClassList("doc-mini-title");
                    contentBox.Add(titelLabel);

                    string vorschauText = BildeInhaltVorschau(aktuellesDoc);
                    if (!string.IsNullOrEmpty(vorschauText))
                    {
                        Label inhaltLabel = new Label(vorschauText);
                        inhaltLabel.AddToClassList("doc-mini-inhalt");
                        contentBox.Add(inhaltLabel);
                    }
                    else
                    {
                        Label leerHinweis = new Label("Kein Inhalt hinterlegt");
                        leerHinweis.AddToClassList("doc-mini-inhalt-leer");
                        contentBox.Add(leerHinweis);
                    }

                    if (plusBtn != null)
                    {
                        plusBtn.text     = "✎";
                        plusBtn.clicked += () => OpenEditPopup(aktuellesDoc);
                    }
                }
                else
                {
                    if (iconBox != null) { iconBox.Clear(); iconBox.style.opacity = 0.4f; }

                    Label leerLabel = new Label("Leer");
                    leerLabel.AddToClassList("doc-mini-empty-label");
                    contentBox.Add(leerLabel);

                    if (plusBtn != null)
                    {
                        plusBtn.text = "+";
                        if (!kategorie.istFest)
                            plusBtn.clicked += () => OpenPopup(kategorie.name);
                        else
                            plusBtn.SetEnabled(false);
                    }
                }
            }

            gridContainer.Add(cardInstance);
        }
    }

    // Baut eine kurze Vorschau aus den Strukturfeldern (Pflichtdokument)
    // oder dem Freitext (flexibles Dokument) für die Kachelansicht.
    private string BildeInhaltVorschau(DocumentData doc)
    {
        if (doc.istPflichtdokument && doc.strukturFelder != null && doc.strukturFelder.Count > 0)
        {
            var ersterAusgefuellterWert = doc.strukturFelder.FirstOrDefault(f => !string.IsNullOrEmpty(f.wert));
            if (ersterAusgefuellterWert == null) return "";

            string label = HoleFeldLabel(doc.title, ersterAusgefuellterWert.key);
            string wert  = ersterAusgefuellterWert.wert;
            if (wert.Length > 18) wert = wert.Substring(0, 15) + "...";
            return $"{label}: {wert}";
        }

        if (!string.IsNullOrEmpty(doc.inhalt))
        {
            string vorschau = doc.inhalt.Split('\n')[0];
            if (vorschau.Length > 24) vorschau = vorschau.Substring(0, 21) + "...";
            return vorschau;
        }

        return "";
    }

    private string HoleFeldLabel(string dokumentTitel, string key)
    {
        if (felderProPflichtDoc.TryGetValue(dokumentTitel, out var definitionen))
        {
            var def = definitionen.FirstOrDefault(f => f.key == key);
            if (def != null) return def.label;
        }
        return key;
    }

    // ─────────────────────────────────────────
    // LISTEN-POPUP
    // ─────────────────────────────────────────

    private void OpenDetailPopup(string kategorie)
    {
        activeCategoryForList = kategorie;
        if (detailPopupOverlay != null) detailPopupOverlay.style.display = DisplayStyle.Flex;
        if (detailPopupTitle   != null) detailPopupTitle.text            = kategorie;

        bool istFest = IstKategorieFest(kategorie);
        if (listCreateNewButton != null)
            listCreateNewButton.style.display = istFest ? DisplayStyle.None : DisplayStyle.Flex;

        RefreshDetailList();
    }

    private void RefreshDetailList()
    {
        if (detailListContainer == null || globalListContainer == null) return;
        detailListContainer.Clear();
        globalListContainer.Clear();

        List<DocumentData> kategorieDocs =
            speicherDaten.savedDocs.FindAll(d => d.category == activeCategoryForList);
        BuildListColumn(kategorieDocs, detailListContainer, isGlobal: false);

        BuildListColumn(speicherDaten.savedDocs, globalListContainer, isGlobal: true);
    }

    private void BuildListColumn(List<DocumentData> docs, VisualElement container, bool isGlobal)
    {
        if (docs.Count == 0)
        {
            Label emptyLabel = new Label(isGlobal ? "Keine Dokumente vorhanden." : "Kategorie ist leer.");
            emptyLabel.AddToClassList("list-empty-hint");
            container.Add(emptyLabel);
            return;
        }

        foreach (var doc in docs)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("list-row-item");

            string icon         = doc.istPflichtdokument ? "🔒" : doc.type == "Diagramm" ? "📊" : doc.type == "Checklist" ? "✅" : "📄";
            string displayTitle = doc.title.Split('\n')[0];
            if (displayTitle.Length > 30) displayTitle = displayTitle.Substring(0, 27) + "...";
            string textToShow   = isGlobal
                ? $"{icon} [{doc.category}] {displayTitle}"
                : $"{icon} {displayTitle}";

            Label nameLabel = new Label(textToShow);
            nameLabel.AddToClassList("list-row-label");
            row.Add(nameLabel);

            string vorschau = BildeInhaltVorschau(doc);
            if (!string.IsNullOrEmpty(vorschau))
            {
                Label inhaltLabel = new Label(vorschau);
                inhaltLabel.AddToClassList("list-row-inhalt-preview");
                row.Add(inhaltLabel);
            }

            VisualElement btnGroup = new VisualElement();
            btnGroup.AddToClassList("row-btn-group");

            if (isGlobal)
            {
                if (doc.category != activeCategoryForList && !doc.istPflichtdokument)
                {
                    Button moveBtn = new Button { text = "Hinzufügen" };
                    moveBtn.AddToClassList("btn-add-global");
                    moveBtn.clicked += () => MoveDocumentToActiveCategory(doc);
                    btnGroup.Add(moveBtn);
                }
            }
            else
            {
                Button editBtn = new Button { text = "Bearbeiten" };
                editBtn.AddToClassList("btn-action-text");
                editBtn.AddToClassList("btn-edit-pen");
                editBtn.tooltip  = "Dokument bearbeiten";
                editBtn.clicked += () => OpenEditPopup(doc);
                btnGroup.Add(editBtn);

                if (!doc.istPflichtdokument)
                {
                    Button deleteBtn = new Button { text = "Löschen" };
                    deleteBtn.AddToClassList("btn-action-text");
                    deleteBtn.AddToClassList("btn-minus-delete");
                    deleteBtn.tooltip  = "Dokument löschen";
                    deleteBtn.clicked += () => DeleteSingleDocument(doc.id);
                    btnGroup.Add(deleteBtn);
                }
                else
                {
                    Label gesperrtLabel = new Label("Geschützt");
                    gesperrtLabel.AddToClassList("locked-badge-inline");
                    btnGroup.Add(gesperrtLabel);
                }
            }

            row.Add(btnGroup);
            container.Add(row);
        }
    }

    private void MoveDocumentToActiveCategory(DocumentData doc)
    {
        DocumentData docInStorage = speicherDaten.savedDocs.Find(d => d.id == doc.id);
        if (docInStorage == null) return;
        if (docInStorage.istPflichtdokument) return;

        docInStorage.category = activeCategoryForList;
        SaveDataLocally();
        RefreshDetailList();
        SpawnAllCardsAtStart();
    }

    private void DeleteSingleDocument(string docId)
    {
        var doc = speicherDaten.savedDocs.Find(d => d.id == docId);
        if (doc == null || doc.istPflichtdokument) return;

        speicherDaten.savedDocs.RemoveAll(d => d.id == docId);
        SaveDataLocally();
        RefreshDetailList();
        SpawnAllCardsAtStart();
    }

    private void CloseDetailPopup()
    {
        if (detailPopupOverlay != null)
            detailPopupOverlay.style.display = DisplayStyle.None;
    }

    // ─────────────────────────────────────────
    // BEARBEITEN-POPUP
    //
    // Zwei Modi:
    //  - Flexibles Dokument:  Titel + Freitext + Template-Picker
    //  - Pflichtdokument:     Titel (read-only Hinweis) + Strukturfelder,
    //                          kein Template-Picker
    // ─────────────────────────────────────────

    private void OpenEditPopup(DocumentData doc)
    {
        activeDocForEditing = doc;
        selectedEditType    = doc.type;

        if (editPopupOverlay != null)
            editPopupOverlay.style.display = DisplayStyle.Flex;

        if (editLockedHint != null)
            editLockedHint.style.display = doc.istPflichtdokument ? DisplayStyle.Flex : DisplayStyle.None;

        if (editDocNameInput != null)
        {
            editDocNameInput.value = doc.title;
            editDocNameInput.schedule.Execute(() => editDocNameInput.Focus()).ExecuteLater(50);
        }

        bool zeigeStrukturFelder = doc.istPflichtdokument && felderProPflichtDoc.ContainsKey(doc.title);

        if (editTemplateGroup != null)
            editTemplateGroup.style.display = doc.istPflichtdokument ? DisplayStyle.None : DisplayStyle.Flex;

        var inhaltGroup = editInhaltInput?.parent;
        if (inhaltGroup != null)
            inhaltGroup.style.display = zeigeStrukturFelder ? DisplayStyle.None : DisplayStyle.Flex;
        if (editInhaltInput != null)
            editInhaltInput.value = doc.inhalt ?? "";

        if (editStrukturFelderBox != null)
        {
            editStrukturFelderBox.Clear();
            editStrukturFelderBox.style.display = zeigeStrukturFelder ? DisplayStyle.Flex : DisplayStyle.None;
            aktiveStrukturFelder.Clear();

            if (zeigeStrukturFelder)
            {
                var definitionen = felderProPflichtDoc[doc.title];

                if (doc.strukturFelder == null) doc.strukturFelder = new List<StrukturFeldWert>();

                foreach (var def in definitionen)
                {
                    var bestehenderWert  = doc.strukturFelder.FirstOrDefault(f => f.key == def.key);
                    string aktuellerWert = bestehenderWert?.wert ?? "";

                    var feldGroup = new VisualElement();
                    feldGroup.AddToClassList("form-group");

                    var feldLabel = new Label(def.label);
                    feldLabel.AddToClassList("field-label");
                    feldGroup.Add(feldLabel);

                    var feldInput = new TextField { value = aktuellerWert };
                    feldInput.name    = $"struktur-feld-{def.key}";
                    feldInput.tooltip = def.placeholder;
                    feldGroup.Add(feldInput);

                    editStrukturFelderBox.Add(feldGroup);
                    aktiveStrukturFelder.Add(feldInput);

                    feldInput.userData = def.key;
                }
            }
        }

        MarkiereAusgewaehlteVorlage(btnEditTypeStandard, btnEditTypeDiagramm, btnEditTypeChecklist, selectedEditType);
    }

    private void CloseEditPopup()
    {
        if (editPopupOverlay != null)
            editPopupOverlay.style.display = DisplayStyle.None;
        activeDocForEditing = null;
        aktiveStrukturFelder.Clear();
    }

    private void SelectEditType(string typeName)
    {
        selectedEditType = typeName;
        MarkiereAusgewaehlteVorlage(btnEditTypeStandard, btnEditTypeDiagramm, btnEditTypeChecklist, selectedEditType);
    }

    private void SaveEditedDocumentEntry()
    {
        if (activeDocForEditing == null) return;

        string updatedTitle = (editDocNameInput != null && !string.IsNullOrEmpty(editDocNameInput.value))
            ? editDocNameInput.value
            : "Unbenannt";

        DocumentData docInList = speicherDaten.savedDocs.Find(d => d.id == activeDocForEditing.id);
        if (docInList != null)
        {
            docInList.title = updatedTitle;

            bool hatStrukturFelder = docInList.istPflichtdokument && felderProPflichtDoc.ContainsKey(docInList.title);

            if (hatStrukturFelder)
            {
                if (docInList.strukturFelder == null) docInList.strukturFelder = new List<StrukturFeldWert>();

                foreach (var feldInput in aktiveStrukturFelder)
                {
                    string key = feldInput.userData as string;
                    if (key == null) continue;

                    var bestehenderEintrag = docInList.strukturFelder.FirstOrDefault(f => f.key == key);
                    if (bestehenderEintrag != null)
                        bestehenderEintrag.wert = feldInput.value;
                    else
                        docInList.strukturFelder.Add(new StrukturFeldWert { key = key, wert = feldInput.value });
                }
            }
            else
            {
                docInList.type   = selectedEditType;
                docInList.inhalt = editInhaltInput != null ? editInhaltInput.value : "";
            }
        }

        SaveDataLocally();
        RefreshDetailList();
        SpawnAllCardsAtStart();
        CloseEditPopup();
    }

    // ─────────────────────────────────────────
    // LÖSCH-BESTÄTIGUNG ("Alle löschen")
    // ─────────────────────────────────────────

    private void OpenDeleteConfirmPopup()
    {
        int anzahlGeschuetzt = speicherDaten.savedDocs.Count(d => d.istPflichtdokument);
        int anzahlLoeschbar  = speicherDaten.savedDocs.Count(d => !d.istPflichtdokument);

        if (deleteConfirmHint != null)
        {
            deleteConfirmHint.text = anzahlGeschuetzt > 0
                ? $"{anzahlLoeschbar} Dokument(e) werden gelöscht. {anzahlGeschuetzt} geschützte Pflichtdokument(e) (🔒) bleiben erhalten."
                : $"{anzahlLoeschbar} Dokument(e) werden unwiderruflich gelöscht.";
        }

        if (deleteConfirmOverlay != null)
            deleteConfirmOverlay.style.display = DisplayStyle.Flex;
    }

    private void CloseDeleteConfirmPopup()
    {
        if (deleteConfirmOverlay != null)
            deleteConfirmOverlay.style.display = DisplayStyle.None;
    }

    private void ConfirmDeleteAllDocuments()
    {
        speicherDaten.savedDocs.RemoveAll(d => !d.istPflichtdokument);
        SaveDataLocally();
        SpawnAllCardsAtStart();
        CloseDeleteConfirmPopup();
    }

    // ─────────────────────────────────────────
    // SPEICHERVERWALTUNG
    // ─────────────────────────────────────────

    private void SaveDataLocally()
    {
        string json = JsonUtility.ToJson(speicherDaten, true);
        File.WriteAllText(saveFilePath, json);
    }

    private void LoadDataLocally()
    {
        if (File.Exists(saveFilePath))
            speicherDaten = JsonUtility.FromJson<DocumentSaveData>(File.ReadAllText(saveFilePath));
    }

    // ─────────────────────────────────────────
    // STATISCHER ZUGRIFF FÜR EXPORT-SCREEN
    // ─────────────────────────────────────────

    public static string GetSaveFilePath()
    {
        return Application.persistentDataPath + "/MyDashboardSave.json";
    }

    public static DocumentSaveData GetSavedDocuments()
    {
        string path = GetSaveFilePath();
        if (!File.Exists(path)) return new DocumentSaveData();
        return JsonUtility.FromJson<DocumentSaveData>(File.ReadAllText(path));
    }

    // Liefert die Unternehmensstammdaten als Key-Value-Dictionary.
    // Verwendung: var daten = DocumentDashboard.GetUnternehmenFelder();
    //             string name = daten.GetValueOrDefault("firmenname", "");
    public static Dictionary<string, string> GetUnternehmenFelder()
    {
        var ergebnis = new Dictionary<string, string>();
        var alle     = GetSavedDocuments();

        var doc = alle.savedDocs.FirstOrDefault(d =>
            d.category == "Gründung" && d.title == "Unternehmensstammdaten");

        if (doc?.strukturFelder != null)
        {
            foreach (var feld in doc.strukturFelder)
                ergebnis[feld.key] = feld.wert;
        }

        return ergebnis;
    }

    // Liefert Kontodaten (IBAN/BIC) als Key-Value-Dictionary.
    // Verwendung: var konto = DocumentDashboard.GetKontodatenFelder();
    //             string iban = konto.GetValueOrDefault("iban", "");
    public static Dictionary<string, string> GetKontodatenFelder()
    {
        var ergebnis = new Dictionary<string, string>();
        var alle     = GetSavedDocuments();

        var kontoDoc = alle.savedDocs.FirstOrDefault(d =>
            d.category == "Bezahlweise" && d.title == "Kontodaten (IBAN/BIC)");

        if (kontoDoc?.strukturFelder != null)
        {
            foreach (var feld in kontoDoc.strukturFelder)
                ergebnis[feld.key] = feld.wert;
        }

        return ergebnis;
    }

    // Bleibt für Rückwärtskompatibilität erhalten.
    public static List<DocumentData> GetBezahlweiseDaten()
    {
        var alle = GetSavedDocuments();
        return alle.savedDocs.FindAll(d => d.category == "Bezahlweise");
    }

    // Gibt den inhalt eines Bezahlweise-Dokuments anhand des Titels zurück.
    public static string GetBezahlweiseInhalt(string titel)
    {
        var alle = GetSavedDocuments();
        var doc  = alle.savedDocs.FirstOrDefault(d =>
            d.category == "Bezahlweise" && d.title == titel);
        return doc?.inhalt ?? "";
    }

    // ─────────────────────────────────────────
    // VORLAGENAUSWAHL VISUELL MARKIEREN
    // ─────────────────────────────────────────
    private void MarkiereAusgewaehlteVorlage(Button standard, Button diagramm, Button checklist, string aktiverTyp)
    {
        standard?.RemoveFromClassList("selected-template");
        diagramm?.RemoveFromClassList("selected-template");
        checklist?.RemoveFromClassList("selected-template");

        switch (aktiverTyp)
        {
            case "Standard":  standard?.AddToClassList("selected-template");  break;
            case "Diagramm":  diagramm?.AddToClassList("selected-template");  break;
            case "Checklist": checklist?.AddToClassList("selected-template"); break;
        }
    }
}
