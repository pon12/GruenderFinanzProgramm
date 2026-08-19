// ================================================================
// EinfachesBalkendiagrammElement.cs
//
// Generisches Balkendiagramm für frei benannte Wertepaare (Label + Zahl).
// Nutzt denselben Zeichenansatz (Painter2D über generateVisualContent) wie
// FinanzChartElement im Finanzdashboard, aber für beliebige Nutzer-Daten
// statt der fest verdrahteten 12-Monats-Einnahmen/Ausgaben-Struktur dort.
// Wird von den "Diagramm"-Dokumenten im Dokumente-Screen genutzt.
// ================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EinfachesBalkendiagrammElement : VisualElement
{
    private List<(string label, float wert)> _daten = new List<(string, float)>();

    private static readonly Color BalkenFarbe = new Color(0.502f, 0.812f, 0.584f, 1f);
    private static readonly Color GridFarbe   = new Color(0.25f, 0.25f, 0.25f, 1f);
    private static readonly Color AchsenFarbe = new Color(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Color LabelFarbe  = new Color(0.75f, 0.75f, 0.75f, 1f);

    private readonly List<Label> _labels = new List<Label>();

    public EinfachesBalkendiagrammElement()
    {
        generateVisualContent += Zeichnen;
        // Neu zeichnen, sobald sich die Größe des Elements ändert (z.B.
        // beim ersten Layout-Durchlauf, wenn contentRect vorher noch 0 war).
        RegisterCallback<GeometryChangedEvent>(_ => { MarkDirtyRepaint(); AktualisiereBeschriftungen(); });
    }

    public void SetzeDaten(List<(string label, float wert)> daten)
    {
        _daten = daten ?? new List<(string, float)>();
        MarkDirtyRepaint();
        // FIX: Labels wurden bisher INNERHALB von Zeichnen() (dem
        // generateVisualContent-Callback) hinzugefügt/entfernt - das
        // Verändern des Visual Trees während der Mesh-Generierung ist ein
        // bekanntes Unity-Antipattern (kann Layout-Schleifen und
        // flackerndes/instabiles Rendering auslösen - genau das "spinnt
        // komplett"-Verhalten). Jetzt sauber getrennt: Zeichnen() zeichnet
        // NUR die Balken/Achsen per Painter2D, Labels werden hier separat
        // verwaltet, außerhalb des Zeichen-Callbacks.
        AktualisiereBeschriftungen();
    }

    private void EntferneLabels()
    {
        foreach (var l in _labels) l.RemoveFromHierarchy();
        _labels.Clear();
    }

    private void AktualisiereBeschriftungen()
    {
        EntferneLabels();
        if (_daten == null || _daten.Count == 0) return;

        float w = contentRect.width;
        float h = contentRect.height;
        if (w <= 0 || h <= 0) return; // noch kein gültiges Layout vorhanden

        float padLeft = 45f, padRight = 12f, padTop = 16f, padBot = 26f;
        float chartW = w - padLeft - padRight;
        float chartH = h - padTop - padBot;
        if (chartW <= 0 || chartH <= 0) return;

        float maxWert = 0f;
        foreach (var (_, wert) in _daten) if (wert > maxWert) maxWert = wert;
        if (maxWert <= 0f) maxWert = 1f;

        int anzahl = _daten.Count;
        float slotBreite = chartW / anzahl;

        for (int i = 0; i < anzahl; i++)
        {
            var (label, wert) = _daten[i];
            float balkenH = chartH * Mathf.Clamp01(wert / maxWert);
            float slotMitte = padLeft + slotBreite * i + slotBreite / 2f;
            float yTop = padTop + chartH - balkenH;

            var beschriftung = new Label(string.IsNullOrEmpty(label) ? "-" : label);
            beschriftung.style.position = Position.Absolute;
            beschriftung.style.left = slotMitte - slotBreite / 2f;
            beschriftung.style.width = slotBreite;
            beschriftung.style.top = padTop + chartH + 4f;
            beschriftung.style.fontSize = 10;
            beschriftung.style.color = LabelFarbe;
            beschriftung.style.unityTextAlign = TextAnchor.UpperCenter;
            beschriftung.pickingMode = PickingMode.Ignore;
            Add(beschriftung);
            _labels.Add(beschriftung);

            var wertLabel = new Label(wert.ToString("0.##"));
            wertLabel.style.position = Position.Absolute;
            wertLabel.style.left = slotMitte - slotBreite / 2f;
            wertLabel.style.width = slotBreite;
            wertLabel.style.top = yTop - 16f;
            wertLabel.style.fontSize = 10;
            wertLabel.style.color = LabelFarbe;
            wertLabel.style.unityTextAlign = TextAnchor.LowerCenter;
            wertLabel.pickingMode = PickingMode.Ignore;
            Add(wertLabel);
            _labels.Add(wertLabel);
        }
    }

    private void Zeichnen(MeshGenerationContext ctx)
    {
        if (_daten == null || _daten.Count == 0) return;

        float w = contentRect.width;
        float h = contentRect.height;
        float padLeft = 45f, padRight = 12f, padTop = 16f, padBot = 26f;
        float chartW = w - padLeft - padRight;
        float chartH = h - padTop - padBot;
        if (chartW <= 0 || chartH <= 0) return;

        float maxWert = 0f;
        foreach (var (_, wert) in _daten) if (wert > maxWert) maxWert = wert;
        if (maxWert <= 0f) maxWert = 1f;

        var p = ctx.painter2D;

        // Achsen
        p.strokeColor = AchsenFarbe;
        p.lineWidth = 1f;
        p.BeginPath();
        p.MoveTo(new Vector2(padLeft, padTop));
        p.LineTo(new Vector2(padLeft, padTop + chartH));
        p.LineTo(new Vector2(padLeft + chartW, padTop + chartH));
        p.Stroke();

        // Grid-Linien (4 horizontale Hilfslinien)
        p.strokeColor = GridFarbe;
        for (int i = 1; i <= 4; i++)
        {
            float y = padTop + chartH - (chartH * i / 4f);
            p.BeginPath();
            p.MoveTo(new Vector2(padLeft, y));
            p.LineTo(new Vector2(padLeft + chartW, y));
            p.Stroke();
        }

        // Balken
        int anzahl = _daten.Count;
        float slotBreite = chartW / anzahl;
        float balkenBreite = Mathf.Min(48f, slotBreite * 0.6f);

        for (int i = 0; i < anzahl; i++)
        {
            var (label, wert) = _daten[i];
            float balkenH = chartH * Mathf.Clamp01(wert / maxWert);
            float slotMitte = padLeft + slotBreite * i + slotBreite / 2f;
            float x = slotMitte - balkenBreite / 2f;
            float yTop = padTop + chartH - balkenH;

            p.fillColor = BalkenFarbe;
            p.BeginPath();
            p.MoveTo(new Vector2(x, padTop + chartH));
            p.LineTo(new Vector2(x, yTop));
            p.LineTo(new Vector2(x + balkenBreite, yTop));
            p.LineTo(new Vector2(x + balkenBreite, padTop + chartH));
            p.ClosePath();
            p.Fill();
        }
    }
}
