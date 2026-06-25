using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
<<<<<<< Updated upstream

public class Finanzen2 : MonoBehaviour
{
    private VisualElement _root;
    private BarChartElement _chart;

    private void Awake()
    {
        UIDocument document = GetComponent<UIDocument>();

        if (document == null)
        {
            Debug.LogError("[Finanzen2] Kein UIDocument gefunden.");
            return;
        }

        _root = document.rootVisualElement;
    }
=======
using System.Linq;
using System.Reflection;

public class Finanzen2 : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    
    private void OnEnable() { }
    private void OnDisable() { }
>>>>>>> Stashed changes

    private void Start()
    {
        SetupDiagramm();
        LadeRentabilitaet();
    }

    private void SetupDiagramm()
    {
        VisualElement diagramm = _root.Q<VisualElement>("diagramm");
        VisualElement yAxis = _root.Q<VisualElement>("chart-y-axis");
        VisualElement xAxis = _root.Q<VisualElement>("chart-x-axis");

        if (diagramm == null)
        {
            Debug.LogError("[Finanzen2] Diagramm nicht gefunden.");
            return;
        }

        _chart = new BarChartElement(yAxis, xAxis);

        _chart.style.position = Position.Absolute;
        _chart.style.left = 0;
        _chart.style.right = 0;
        _chart.style.top = 0;
        _chart.style.bottom = 0;

        diagramm.Add(_chart);

        SetDaten(new float[]
        {
            100000f, 40000f, 8000f,
            220000f, 90000f, 45000f
        });
    }

    public void SetDaten(float[] daten)
    {
<<<<<<< Updated upstream
        if (_chart != null)
            _chart.SetValues(daten);
=======
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        
        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        int baseYear = DateTime.Now.Year;
        var alleAusgaben = db.getAllAusgabenEntries();

        // 1. Bereich "Kosten"
        VisualElement containerKosten = uiDocument.rootVisualElement.Q<VisualElement>("Kosten");
        if (containerKosten != null)
        {
            containerKosten.Clear();
            containerKosten.Add(CreateHeader());
            string[] katKosten = { "Honorar", "Material" };

            foreach (var kat in katKosten)
            {
                float y1 = BerechneAusgabenNachKategorie(alleAusgaben as IEnumerable, kat, baseYear);
                float y2 = BerechneAusgabenNachKategorie(alleAusgaben as IEnumerable, kat, baseYear + 1);
                containerKosten.Add(CreateRow(kat, y1, y2));
            }
        }

        // 2. Bereich "Betriebsausgaben"
        VisualElement containerBetriebsausgaben = uiDocument.rootVisualElement.Q<VisualElement>("Betriebsausgaben");
        if (containerBetriebsausgaben != null)
        {
            containerBetriebsausgaben.Clear();
            containerBetriebsausgaben.Add(CreateHeader());
            // HINWEIS: Hier stehen jetzt die Namen, die vermutlich im Kassenbuch-Dropdown stehen
            string[] katBetriebsausgaben = { "Auto", "Marketing", "Reisekosten" };
            
            float summeBetriebY1 = 0;
            float summeBetriebY2 = 0;
            
            foreach (var kat in katBetriebsausgaben)
            {
                float y1 = BerechneAusgabenNachKategorie(alleAusgaben as IEnumerable, kat, baseYear);
                float y2 = BerechneAusgabenNachKategorie(alleAusgaben as IEnumerable, kat, baseYear + 1);
                summeBetriebY1 += y1;
                summeBetriebY2 += y2;
                containerBetriebsausgaben.Add(CreateRow(kat, y1, y2));
            }
            containerBetriebsausgaben.Add(CreateRow("Summe Betriebsausgaben", summeBetriebY1, summeBetriebY2, true));
        }

        // 3. Bereich "Dienstleistungen"
        VisualElement containerDienstleistungen = uiDocument.rootVisualElement.Q<VisualElement>("Dienstleistungen");
        if (containerDienstleistungen != null)
        {
            containerDienstleistungen.Clear();
            containerDienstleistungen.Add(CreateHeader());
            var alleServices = db.getAllServices() ?? new List<Service>();
            var alleRechnungen = db.getAllInvoices();
            float summeUmsatzY1 = 0;
            float summeUmsatzY2 = 0;

            foreach (var service in alleServices)
            {
                float y1 = BerechneDienstleistungUmsatz(service, alleRechnungen as IEnumerable, baseYear);
                float y2 = BerechneDienstleistungUmsatz(service, alleRechnungen as IEnumerable, baseYear + 1);
                summeUmsatzY1 += y1;
                summeUmsatzY2 += y2;
                containerDienstleistungen.Add(CreateRow(service.name, y1, y2));
            }
            containerDienstleistungen.Add(CreateRow("Summe Umsätze", summeUmsatzY1, summeUmsatzY2, true));
            float direkteKostenY1 = BerechneAusgabenNachKategorie(alleAusgaben as IEnumerable, "Material", baseYear) + BerechneAusgabenNachKategorie(alleAusgaben as IEnumerable, "Honorar", baseYear);
            float direkteKostenY2 = BerechneAusgabenNachKategorie(alleAusgaben as IEnumerable, "Material", baseYear + 1) + BerechneAusgabenNachKategorie(alleAusgaben as IEnumerable, "Honorar", baseYear + 1);
            containerDienstleistungen.Add(CreateRow("Summe direkte Kosten", direkteKostenY1, direkteKostenY2)); 
            float rohgewinnY1 = summeUmsatzY1 - direkteKostenY1;
            float rohgewinnY2 = summeUmsatzY2 - direkteKostenY2;
            containerDienstleistungen.Add(CreateRow("Rohgewinn", rohgewinnY1, rohgewinnY2, true)); 
        }
    }

    private float BerechneAusgabenNachKategorie(IEnumerable alleAusgaben, string kategorie, int year)
    {
        float summe = 0;
        if (alleAusgaben == null) return 0;

        foreach (object a in alleAusgaben)
        {
            if (a == null) continue;
            string status = HoleFeldOderProperty(a, "Status", "Kategorie")?.ToString() ?? "";
            string datumStr = HoleFeldOderProperty(a, "Datum")?.ToString() ?? "";

            // DEBUG: Hier siehst du in der Konsole, was gefunden wurde
            if (status.Contains(kategorie) || kategorie.Contains(status)) { /* Debug.Log($"Match gefunden: {status} == {kategorie}"); */ }

            if (status.Equals(kategorie, StringComparison.OrdinalIgnoreCase) && DateTime.TryParse(datumStr, out DateTime d) && d.Year == year)
            {
                object betragObj = HoleFeldOderProperty(a, "Amount", "Betrag", "Wert");
                if (float.TryParse(betragObj?.ToString(), out float parsedBetrag)) summe += parsedBetrag; 
            }
        }
        return summe;
    }

    private float BerechneDienstleistungUmsatz(Service service, IEnumerable alleRechnungen, int year)
    {
        float umsatz = 0;
        if (alleRechnungen == null) return 0;
        foreach (object r in alleRechnungen)
        {
            if (r == null) continue;
            string status = HoleFeldOderProperty(r, "Status")?.ToString() ?? "";
            string datumStr = HoleFeldOderProperty(r, "Datum")?.ToString() ?? "";
            if (status == "Bezahlt" && DateTime.TryParse(datumStr, out DateTime d) && d.Year == year)
            {
                IEnumerable positionen = HoleFeldOderProperty(r, "Positionen", "Items") as IEnumerable;
                if (positionen != null)
                {
                    foreach (object pos in positionen)
                    {
                        if (pos == null) continue;
                        string artikelName = HoleFeldOderProperty(pos, "ArtikelName", "Name", "Artikel")?.ToString() ?? "";
                        if (artikelName == service.name) 
                        {
                            object preisObj = HoleFeldOderProperty(pos, "GesamtPreis", "Preis", "Gesamt");
                            if (float.TryParse(preisObj?.ToString(), out float parsedPreis)) umsatz += parsedPreis; 
                        }
                    }
                }
            }
        }
        return umsatz;
    }

    private object HoleFeldOderProperty(object obj, params string[] namen)
    {
        if (obj == null) return null;
        Type t = obj.GetType();
        foreach (string name in namen)
        {
            var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null) return prop.GetValue(obj);
            var feld = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (feld != null) return feld.GetValue(obj);
        }
        return null;
>>>>>>> Stashed changes
    }

    // =========================================================
    // RENTABILITÄT
    // =========================================================
    private void LadeRentabilitaet()
    {
        var container = _root.Q<VisualElement>("Rentabilitaet");

        if (container == null)
        {
            Debug.LogWarning("Rentabilitaet nicht gefunden");
            return;
        }

        container.Clear();

        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        var einkommen = db.getAllEinkommenEntries();
        var ausgaben = db.getAllAusgabenEntries();

        float u1 = 0, u2 = 0;
        float d1 = 0, d2 = 0;

        float gk1 = 0, gk2 = 0;
        float pa1 = 0, pa2 = 0;
        float ba1 = 0, ba2 = 0;
        float ab1 = 0, ab2 = 0;
        float z1 = 0, z2 = 0;

        int baseYear = GetBaseYear(einkommen, ausgaben);

        if (einkommen != null)
        {
            foreach (var e in einkommen)
            {
                if (!TryParse(e.Datum, out var d)) continue;

                if (d.Year == baseYear) u1 += (float)e.Amount;
                if (d.Year == baseYear + 1) u2 += (float)e.Amount;
            }
        }

        if (ausgaben != null)
        {
            foreach (var a in ausgaben)
            {
                if (!TryParse(a.Datum, out var d)) continue;

                string desc = a.Description.ToLower();

                if (desc.Contains("direkt"))
                {
                    if (d.Year == baseYear) d1 += (float)a.Amount;
                    if (d.Year == baseYear + 1) d2 += (float)a.Amount;
                }
                else if (desc.Contains("gründung"))
                {
                    if (d.Year == baseYear) gk1 += (float)a.Amount;
                    if (d.Year == baseYear + 1) gk2 += (float)a.Amount;
                }
                else if (desc.Contains("personal"))
                {
                    if (d.Year == baseYear) pa1 += (float)a.Amount;
                    if (d.Year == baseYear + 1) pa2 += (float)a.Amount;
                }
                else if (desc.Contains("betrieb"))
                {
                    if (d.Year == baseYear) ba1 += (float)a.Amount;
                    if (d.Year == baseYear + 1) ba2 += (float)a.Amount;
                }
                else if (desc.Contains("abschreibung"))
                {
                    if (d.Year == baseYear) ab1 += (float)a.Amount;
                    if (d.Year == baseYear + 1) ab2 += (float)a.Amount;
                }
                else if (desc.Contains("zins"))
                {
                    if (d.Year == baseYear) z1 += (float)a.Amount;
                    if (d.Year == baseYear + 1) z2 += (float)a.Amount;
                }
            }
        }

        float rg1 = u1 - d1;
        float rg2 = u2 - d2;

        float betriebs1 = rg1 - (gk1 + pa1 + ba1 + ab1);
        float betriebs2 = rg2 - (gk2 + pa2 + ba2 + ab2);

        float ergebnis1 = betriebs1 - z1;
        float ergebnis2 = betriebs2 - z2;

        container.Add(CreateRentHeader());

        container.Add(CreateRentRow("Umsatzerlöse", u1, u2));
        container.Add(CreateRentRow("Direkte Kosten", d1, d2));
        container.Add(CreateRentRow("Rohgewinn", rg1, rg2));

        container.Add(CreateRentRow("Gründungskosten", gk1, gk2));
        container.Add(CreateRentRow("Personalaufwand", pa1, pa2));
        container.Add(CreateRentRow("Betriebsaufwand", ba1, ba2));
        container.Add(CreateRentRow("Abschreibungen", ab1, ab2));

        container.Add(CreateRentRow("Betriebsergebnis", betriebs1, betriebs2));

        container.Add(CreateRentRow("Zinsen", z1, z2));

        container.Add(CreateRentRow("Ergebnis (vor Steuern)", ergebnis1, ergebnis2));

        container.Add(CreateRentRowBold("Überschuss / Fehlbetrag", ergebnis1, ergebnis2));
    }

    // =========================================================
    // UI
    // =========================================================
    private VisualElement CreateRentHeader()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 6;

        row.Add(CreateCell("Name", true));
        row.Add(CreateCell("Jahr 1", true));
        row.Add(CreateCell("Jahr 2", true));

        return row;
    }

<<<<<<< Updated upstream
    private VisualElement CreateRentRow(string name, float y1, float y2)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;

        row.Add(CreateCell(name, false));
        row.Add(CreateCell(y1.ToString("N0") + " €", false));
        row.Add(CreateCell(y2.ToString("N0") + " €", false));

        return row;
    }

    private VisualElement CreateRentRowBold(string name, float y1, float y2)
=======
    private VisualElement CreateRow(string name, float y1, float y2, bool isBold = false)
>>>>>>> Stashed changes
    {
        var row = CreateRentRow(name, y1, y2);

        foreach (var c in row.Children())
            if (c is Label l)
                l.style.unityFontStyleAndWeight = FontStyle.Bold;

        return row;
    }

<<<<<<< Updated upstream
    // =========================================================
    // HELPERS
    // =========================================================
    private bool TryParse(string text, out DateTime result)
    {
        return DateTime.TryParse(text, out result);
    }

    private int GetBaseYear(List<Einkommen> einkommen, List<Ausgaben> ausgaben)
=======
    private VisualElement CreateCell(string text, bool isBold, int widthPercent)
>>>>>>> Stashed changes
    {
        DateTime min = DateTime.MaxValue;

        if (einkommen != null)
        {
            foreach (var e in einkommen)
                if (TryParse(e.Datum, out var d))
                    if (d < min) min = d;
        }

        if (ausgaben != null)
        {
            foreach (var a in ausgaben)
                if (TryParse(a.Datum, out var d))
                    if (d < min) min = d;
        }

        return min == DateTime.MaxValue ? DateTime.Now.Year : min.Year;
    }

    // =========================================================
    // CHART (FIX: KEINE VERTIKALEN GRID-LINIEN MEHR)
    // =========================================================
    private class BarChartElement : VisualElement
    {
        private float[] _values;
        private readonly VisualElement _yAxis;
        private readonly VisualElement _xAxis;

        public BarChartElement(VisualElement yAxis, VisualElement xAxis)
        {
            _yAxis = yAxis;
            _xAxis = xAxis;
            generateVisualContent += Draw;
        }

        public void SetValues(float[] values)
        {
            _values = values;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;

            float width = contentRect.width;
            float height = contentRect.height;

            if (width <= 0 || height <= 0 || _values == null)
                return;

            float max = 1f;
            foreach (var v in _values)
                if (v > max) max = v;

            // =====================================================
            // NUR HORIZONTALE GRID-LINIEN
            // =====================================================
            p.strokeColor = new Color(1f, 1f, 1f, 0.12f);
            p.lineWidth = 1f;

            if (_yAxis != null)
            {
                foreach (var label in _yAxis.Children())
                {
                    float y = label.worldBound.center.y - worldBound.yMin;

                    p.BeginPath();
                    p.MoveTo(new Vector2(0, y));
                    p.LineTo(new Vector2(width, y));
                    p.Stroke();
                }
            }

            // ❌ KEINE X-AXIS GRID LINES (VERTIKAL ENTFERNT)

            int years = 2;
            float groupWidth = width / years;

            float barWidth = 40f;
            float spacing = 10f;

            for (int year = 0; year < years; year++)
            {
                float startX = year * groupWidth + groupWidth / 2f - 70f;

                for (int type = 0; type < 3; type++)
                {
                    int index = year * 3 + type;
                    if (index >= _values.Length) continue;

                    float value = _values[index];
                    float h = (value / max) * height;

                    float x = startX + type * (barWidth + spacing);
                    float y = height - h;

                    Color barColor =
                        type == 0 ? new Color(0.2f, 0.8f, 0.3f, 1f) :
                        type == 1 ? new Color(0.95f, 0.6f, 0.1f, 1f) :
                                    new Color(0.3f, 0.6f, 1f, 1f);

                    p.fillColor = barColor;

                    p.BeginPath();
                    p.MoveTo(new Vector2(x, height));
                    p.LineTo(new Vector2(x, y));
                    p.LineTo(new Vector2(x + barWidth, y));
                    p.LineTo(new Vector2(x + barWidth, height));
                    p.ClosePath();
                    p.Fill();
                }
            }
        }
    }

    private VisualElement CreateCell(string text, bool isHeader)
    {
        var label = new Label(text);

        label.style.color = Color.white;
        label.style.unityTextAlign = TextAnchor.MiddleLeft;

        label.style.flexGrow = 1;
        label.style.width = Length.Percent(33);

        label.style.fontSize = isHeader ? 16 : 14;

        if (isHeader)
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

        return label;
    }

<<<<<<< Updated upstream
    
=======
    private VisualElement CreateHeader2() => CreateHeader(); // Placeholder
    private VisualElement CreateRow2(string name, float y1, bool isBold = false) => CreateRow(name, y1, 0, isBold);
>>>>>>> Stashed changes
}