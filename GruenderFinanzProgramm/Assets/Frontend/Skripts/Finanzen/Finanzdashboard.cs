using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Globalization;

public class Finanzdashboard : MonoBehaviour
{
    private VisualElement _root;
    private FinanzChartElement _chart;

    private DataBase db;

    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        db = UserDatabaseAccess.getCurrentUserDatabase();

        SetupChart();
    }

    void SetupChart()
    {
        var canvas = _root.Q<VisualElement>("Diagramm");

        if (canvas == null)
        {
            Debug.LogError("Diagramm nicht gefunden");
            return;
        }

        if (db == null)
        {
            Debug.LogError("Datenbank nicht verfügbar");
            return;
        }

        int aktuellesJahr = DateTime.Now.Year;

        float[] einnahmen = new float[12];
        float[] ausgaben = new float[12];

        // =========================
        // EINNAHMEN
        // =========================

        var einkommenListe = db.getAllEinkommenEntries();

        if (einkommenListe != null)
        {
            foreach (var e in einkommenListe)
            {
                if (!TryParseDate(e.getDatum(), out DateTime datum))
                    continue;

                if (datum.Year != aktuellesJahr)
                    continue;

                float wert = ParseAmount(e.getAmount());

                einnahmen[datum.Month - 1] += wert;
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
                if (!TryParseDate(a.getDatum(), out DateTime datum))
                    continue;

                if (datum.Year != aktuellesJahr)
                    continue;

                float wert = ParseAmount(a.getAmount());

                ausgaben[datum.Month - 1] += wert;
            }
        }

        canvas.Clear();

        _chart = new FinanzChartElement();
        _chart.SetData(einnahmen, ausgaben);

        canvas.Add(_chart);

        _chart.style.flexGrow = 1;
    }

    private float ParseAmount(string value)
    {
        value = value.Replace("€", "").Trim();

        float.TryParse(
            value.Replace(",", "."),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out float result);

        return result;
    }

    private bool TryParseDate(string text, out DateTime result)
    {
        string[] formats =
        {
            "dd.MM.yyyy",
            "d.M.yyyy",
            "dd.M.yyyy",
            "d.MM.yyyy",
            "yyyy-MM-dd",
            "yyyy/MM/dd"
        };

        return DateTime.TryParseExact(
            text,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result)
            || DateTime.TryParse(text, out result);
    }
}