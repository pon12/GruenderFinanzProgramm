using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Finanzen1 : MonoBehaviour
{
    private VisualElement _root;
    private LineChartElement _chart;

    private void Awake()
    {
        UIDocument document = GetComponent<UIDocument>();

        if (document == null)
        {
            Debug.LogError("[Finanzen1] Kein UIDocument gefunden.");
            return;
        }

        _root = document.rootVisualElement;
    }

    private void Start()
    {
        SetupDiagramm();
        LadeMonatlichenCashflow();
        LadeLiquiditaetAmAnfang();
        LadeGruendungsUebersicht();
        LadeAktuelleLiquiditaet();
    }

    private void SetupDiagramm()
    {
        VisualElement diagramm =
            _root.Q<VisualElement>("diagramm");

        VisualElement yAxis =
            _root.Q<VisualElement>("chart-y-axis");

        VisualElement xAxis =
            _root.Q<VisualElement>("chart-x-axis");

        if (diagramm == null)
        {
            Debug.LogError("[Finanzen2] Diagramm nicht gefunden.");
            return;
        }

        _chart = new LineChartElement(yAxis, xAxis);

        _chart.style.position = Position.Absolute;
        _chart.style.left = 0;
        _chart.style.right = 0;
        _chart.style.top = 0;
        _chart.style.bottom = 0;

        diagramm.Add(_chart);
    }

    public void SetDaten(float[] daten)
    {
        if (_chart != null)
        {
            _chart.SetValues(daten);
        }
    }

    private class LineChartElement : VisualElement
    {
        private float[] _values;

        private readonly VisualElement _yAxis;
        private readonly VisualElement _xAxis;

        public LineChartElement(
            VisualElement yAxis,
            VisualElement xAxis)
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

            if (width <= 0 || height <= 0)
                return;

            // =====================================
            // HILFSLINIEN AUF LABEL-POSITIONEN
            // =====================================

            p.strokeColor = new Color(1f, 1f, 1f, 0.15f);
            p.lineWidth = 1f;

            if (_yAxis != null)
            {
                foreach (var label in _yAxis.Children())
                {
                    float y =
                        label.worldBound.center.y -
                        worldBound.yMin;

                    p.BeginPath();
                    p.MoveTo(new Vector2(0, y));
                    p.LineTo(new Vector2(width, y));
                    p.Stroke();
                }
            }

            if (_xAxis != null)
            {
                foreach (var label in _xAxis.Children())
                {
                    float x =
                        label.worldBound.center.x -
                        worldBound.xMin;

                    p.BeginPath();
                    p.MoveTo(new Vector2(x, 0));
                    p.LineTo(new Vector2(x, height));
                    p.Stroke();
                }
            }

            // =====================================
            // KEINE DATEN
            // =====================================

            if (_values == null || _values.Length < 2)
            {
                _values = new float[]
                {
                    0,0,0,0,0,0,
                    0,0,0,0,0,0
                };
            }

            // =====================================
            // MIN / MAX
            // =====================================

            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (float value in _values)
            {
                if (value < min) min = value;
                if (value > max) max = value;
            }

            float range = Mathf.Max(max - min, 1f);

            // =====================================
            // PUNKTE BERECHNEN
            // =====================================

            Vector2[] points = new Vector2[_values.Length];

            for (int i = 0; i < _values.Length; i++)
            {
                float x = width * i / (_values.Length - 1);

                float normalized =
                    (_values[i] - min) / range;

                float y =
                    height -
                    (normalized * height);

                points[i] = new Vector2(x, y);
            }

            // =====================================
            // DIAGRAMMLINIE
            // =====================================

            p.strokeColor = Color.green;
            p.lineWidth = 3f;

            p.BeginPath();
            p.MoveTo(points[0]);

            for (int i = 1; i < points.Length; i++)
            {
                p.LineTo(points[i]);
            }

            p.Stroke();

            // =====================================
            // DATENPUNKTE
            // =====================================

            p.fillColor = Color.green;

            foreach (Vector2 point in points)
            {
                p.BeginPath();
                p.Arc(point, 4f, 0f, 360f);
                p.Fill();
            }
        }
    }
    private void LadeMonatlichenCashflow()
    {
        Label label = _root.Q<Label>("mntlCashflow");

        if (label == null)
        {
            Debug.LogWarning("[Finanzen1] mntlCashflow Label nicht gefunden");
            return;
        }

        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            label.text = "0 €";
            return;
        }

        float einnahmen = 0f;
        float ausgaben = 0f;

        var einkommen = db.getAllEinkommenEntries();
        var ausgabenList = db.getAllAusgabenEntries();

        if (einkommen != null)
        {
            foreach (var e in einkommen)
            {
                einnahmen += (float)e.Amount;
            }
        }

        if (ausgabenList != null)
        {
            foreach (var a in ausgabenList)
            {
                ausgaben += (float)a.Amount;
            }
        }

        float cashflow = einnahmen - ausgaben;

        label.text = cashflow.ToString("N2") + " €";

        Debug.Log("[Finanzen1] Cashflow: " + cashflow);
    }

    private void LadeLiquiditaetAmAnfang()
    {
        Label label = _root.Q<Label>("LiquAnfang");

        if (label == null)
        {
            Debug.LogWarning("[Finanzen1] LiquAnfang Label nicht gefunden");
            return;
        }

        var db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            label.text = "0 €";
            return;
        }

        var einkommen = db.getAllEinkommenEntries();
        var ausgaben = db.getAllAusgabenEntries();

        if ((einkommen == null || einkommen.Count == 0) &&
            (ausgaben == null || ausgaben.Count == 0))
        {
            label.text = "0 €";
            return;
        }

        DateTime firstDate = DateTime.MaxValue;

        void CheckDate(string date)
        {
            if (DateTime.TryParse(date, out DateTime d))
            {
                if (d < firstDate)
                    firstDate = d;
            }
        }

        foreach (var e in einkommen)
            CheckDate(e.Datum);

        foreach (var a in ausgaben)
            CheckDate(a.Datum);

        float einnahmen = 0f;
        float kosten = 0f;

        foreach (var e in einkommen)
        {
            if (DateTime.TryParse(e.Datum, out DateTime d) && d == firstDate)
                einnahmen += (float)e.Amount;
        }

        foreach (var a in ausgaben)
        {
            if (DateTime.TryParse(a.Datum, out DateTime d) && d == firstDate)
                kosten += (float)a.Amount;
        }

        float liquiditaetAnfang = einnahmen - kosten;

        label.text = liquiditaetAnfang.ToString("N2") + " €";

        Debug.Log("[Finanzen1] Liquidität Anfang: " + liquiditaetAnfang);
    }

   private void LadeGruendungsUebersicht()
    {
        var container = _root.Q<VisualElement>("GruendungTabelle");

        if (container == null)
        {
            Debug.LogError("GruendungTabelle nicht gefunden");
            return;
        }

        container.Clear();

        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null)
        {
            Debug.LogWarning("Keine aktive Datenbank");
            return;
        }

        float geldeinlagen = 0f;
        float kredite = 0f;

        float investitionen = 0f;
        float gruendungskosten = 0f;
        float mwstInvest = 0f;
        float mwstGruendung = 0f;

        // =========================
        // EINNAHMEN AUSWERTEN
        // =========================
        var einkommen = db.getAllEinkommenEntries();

        if (einkommen != null)
        {
            foreach (var e in einkommen)
            {
                string desc = e.Description.ToLower();

                if (desc.Contains("einlage"))
                    geldeinlagen += (float)e.Amount;

                if (desc.Contains("kredit"))
                    kredite += (float)e.Amount;
            }
        }

        // =========================
        // AUSGABEN AUSWERTEN
        // =========================
        var ausgaben = db.getAllAusgabenEntries();

        if (ausgaben != null)
        {
            foreach (var a in ausgaben)
            {
                string desc = a.Description.ToLower();

                if (desc.Contains("invest"))
                    investitionen += (float)a.Amount;

                else if (desc.Contains("gründung") || desc.Contains("gruendung"))
                    gruendungskosten += (float)a.Amount;

                else if (desc.Contains("mwst invest"))
                    mwstInvest += (float)a.Amount;

                else if (desc.Contains("mwst gründung"))
                    mwstGruendung += (float)a.Amount;
            }
        }

        float anfangsbestand =
            (geldeinlagen + kredite + mwstInvest + mwstGruendung)
            - (investitionen + gruendungskosten);

        // =========================
        // UI AUSGABE
        // =========================
        container.Add(CreateRow("Geldeinlagen", geldeinlagen));
        container.Add(CreateRow("Kredite", kredite));
        container.Add(CreateRow("Investitionen", investitionen));
        container.Add(CreateRow("Gründungskosten", gruendungskosten));
        container.Add(CreateRow("Rückerstattung MwSt Investitionen", mwstInvest));
        container.Add(CreateRow("Rückerstattung MwSt Gründungskosten", mwstGruendung));

        container.Add(CreateSeparator());
        container.Add(CreateRowBold("Anfangsbestand zu Geschäftsbeginn", anfangsbestand));
    }

    private VisualElement CreateRow(string label, float value)
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;

        Label l = new Label(label);
        Label v = new Label(value.ToString("N2") + " €");

        l.style.color = Color.white;
        v.style.color = Color.white;

        row.Add(l);
        row.Add(v);

        return row;
    }

    private VisualElement CreateRowBold(string label, float value)
    {
        VisualElement row = CreateRow(label, value);

        foreach (var child in row.Children())
        {
            if (child is Label lbl)
            {
                lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
        }

        return row;
    }

    private VisualElement CreateSeparator()
    {
        VisualElement line = new VisualElement();
        line.style.height = 2;
        line.style.marginTop = 10;
        line.style.marginBottom = 10;
        line.style.backgroundColor = new Color(1, 1, 1, 0.2f);

        return line;
    }

    private void LadeAktuelleLiquiditaet()
    {
        var label = _root.Q<Label>("LiquAktuell");

        if (label == null)
        {
            Debug.LogWarning("LiquAktuell Label nicht gefunden");
            return;
        }

        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            label.text = "0,00 €";
            return;
        }

        float einnahmen = 0f;
        float ausgaben = 0f;

        // =========================
        // EINNAHMEN
        // =========================
        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
        {
            foreach (var e in einkommen)
            {
                einnahmen += (float)e.Amount;
            }
        }

        // =========================
        // AUSGABEN
        // =========================
        var ausgabenListe = db.getAllAusgabenEntries();
        if (ausgabenListe != null)
        {
            foreach (var a in ausgabenListe)
            {
                ausgaben += (float)a.Amount;
            }
        }

        float liquiditaet = einnahmen - ausgaben;

        label.text = liquiditaet.ToString("N2") + " €";

        Debug.Log("[Finanzen1] Liquidität aktuell: " + liquiditaet);
    }
}