using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class FinanzChartElement : VisualElement
{
    private float[] _einnahmen;
    private float[] _ausgaben;

    private static readonly Color EinnahmenColor     = new Color(0.502f, 0.812f, 0.584f, 1f);
    private static readonly Color EinnahmenFill      = new Color(0.502f, 0.812f, 0.584f, 0.15f);
    private static readonly Color AusgabenColor      = new Color(0.902f, 0.224f, 0.275f, 1f);
    private static readonly Color AusgabenFill       = new Color(0.902f, 0.224f, 0.275f, 0.15f);
    private static readonly Color GridColor          = new Color(0.25f,  0.25f,  0.25f,  1f);
    private static readonly Color AxisColor          = new Color(0.4f,   0.4f,   0.4f,   1f);
    private static readonly Color LabelColor         = new Color(0.627f, 0.627f, 0.627f, 1f);

    private readonly List<Label> _labels = new List<Label>();

    private readonly string[] _monate = {
        "Jan","Feb","Mär","Apr","Mai","Jun",
        "Jul","Aug","Sep","Okt","Nov","Dez"
    };

    public FinanzChartElement()
    {
        generateVisualContent += Draw;
    }

    public void SetData(float[] einnahmen, float[] ausgaben)
    {
        _einnahmen = einnahmen;
        _ausgaben  = ausgaben;
        MarkDirtyRepaint();
    }

    private void ClearLabels()
    {
        foreach (var l in _labels) l.RemoveFromHierarchy();
        _labels.Clear();
    }

    private void Draw(MeshGenerationContext ctx)
    {
        ClearLabels();

        if (_einnahmen == null || _ausgaben == null) return;

        float w        = contentRect.width;
        float h        = contentRect.height;
        float padLeft  = 55f;
        float padRight = 12f;
        float padTop   = 20f;
        float padBot   = 30f;

        float chartW = w - padLeft - padRight;
        float chartH = h - padTop  - padBot;

        var p = ctx.painter2D;

        // Max-Wert bestimmen
        float maxVal = 1f;
        for (int i = 0; i < 12; i++)
        {
            if (_einnahmen[i] > maxVal) maxVal = _einnahmen[i];
            if (_ausgaben[i]  > maxVal) maxVal = _ausgaben[i];
        }
        maxVal *= 1.1f;

        // Grid + Y-Achse Labels
        for (int g = 0; g <= 4; g++)
        {
            float t  = g / 4f;
            float yg = padTop + chartH * t;
            float val = maxVal * (1f - t);

            p.strokeColor = GridColor;
            p.lineWidth   = 0.5f;
            p.BeginPath();
            p.MoveTo(new Vector2(padLeft, yg));
            p.LineTo(new Vector2(w - padRight, yg));
            p.Stroke();

            string valText = val >= 1000 ? $"€{val/1000:0.#}k" : $"€{val:0}";
            var lbl = MakeChartLabel(valText, padLeft - 4, yg - 9, LabelColor, 10, TextAnchor.MiddleRight);
            Add(lbl); _labels.Add(lbl);
        }

        // X-Achse Monatsbeschriftung + Punkte berechnen
        var ePts = new Vector2[12];
        var aPts = new Vector2[12];

        for (int i = 0; i < 12; i++)
        {
            float x = padLeft + chartW * i / 11f;
            ePts[i] = new Vector2(x, padTop + chartH * (1f - _einnahmen[i] / maxVal));
            aPts[i] = new Vector2(x, padTop + chartH * (1f - _ausgaben[i]  / maxVal));

            var mLbl = MakeChartLabel(_monate[i], x - 12, h - padBot + 4, LabelColor, 10, TextAnchor.UpperCenter);
            Add(mLbl); _labels.Add(mLbl);
        }

        // Füllfläche Einnahmen
        p.fillColor = EinnahmenFill;
        p.BeginPath();
        p.MoveTo(new Vector2(ePts[0].x, padTop + chartH));
        foreach (var pt in ePts) p.LineTo(pt);
        p.LineTo(new Vector2(ePts[11].x, padTop + chartH));
        p.ClosePath(); p.Fill();

        // Füllfläche Ausgaben
        p.fillColor = AusgabenFill;
        p.BeginPath();
        p.MoveTo(new Vector2(aPts[0].x, padTop + chartH));
        foreach (var pt in aPts) p.LineTo(pt);
        p.LineTo(new Vector2(aPts[11].x, padTop + chartH));
        p.ClosePath(); p.Fill();

        // Linie Einnahmen
        p.strokeColor = EinnahmenColor; p.lineWidth = 2.5f;
        p.BeginPath(); p.MoveTo(ePts[0]);
        for (int i = 1; i < 12; i++) p.LineTo(ePts[i]);
        p.Stroke();

        // Linie Ausgaben
        p.strokeColor = AusgabenColor; p.lineWidth = 2.5f;
        p.BeginPath(); p.MoveTo(aPts[0]);
        for (int i = 1; i < 12; i++) p.LineTo(aPts[i]);
        p.Stroke();

        // Punkte Einnahmen
        p.fillColor = EinnahmenColor;
        foreach (var pt in ePts) { p.BeginPath(); p.Arc(pt, 3.5f, 0, 360); p.Fill(); }

        // Punkte Ausgaben
        p.fillColor = AusgabenColor;
        foreach (var pt in aPts) { p.BeginPath(); p.Arc(pt, 3.5f, 0, 360); p.Fill(); }
    }

    private Label MakeChartLabel(string text, float x, float y, Color farbe, int size, TextAnchor anchor)
    {
        var l = new Label(text);
        l.style.position  = Position.Absolute;
        l.style.left      = x;
        l.style.top       = y;
        l.style.color     = farbe;
        l.style.fontSize  = size;
        return l;
    }
}
