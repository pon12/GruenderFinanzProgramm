// HI :) UIUIUIUIUIUIUIUIUI SPAAAßßßßßßß

using System;
using System.Collections.Generic;

[Serializable]
public class FinanzMonatswert
{
    public int monthIndex;
    public string monthName;

    public float einnahmen;
    public float ausgaben;
    public float gewinn;

    public FinanzMonatswert()
    {
    }

    public FinanzMonatswert(int monthIndex, string monthName)
    {
        this.monthIndex = monthIndex;
        this.monthName = monthName;

        einnahmen = 0f;
        ausgaben = 0f;
        gewinn = 0f;
    }
}

[Serializable]
public class DienstleistungMonatsUmsatz
{
    public string dienstleistungName;

    public List<float> monatsUmsaetze = new List<float>();
    public float gesamtUmsatz;

    public DienstleistungMonatsUmsatz()
    {
        InitializeMonths();
    }

    public DienstleistungMonatsUmsatz(string dienstleistungName)
    {
        this.dienstleistungName = dienstleistungName;
        InitializeMonths();
    }

    private void InitializeMonths()
    {
        monatsUmsaetze.Clear();

        for (int i = 0; i < 12; i++)
        {
            monatsUmsaetze.Add(0f);
        }

        gesamtUmsatz = 0f;
    }

    public void AddUmsatz(int monthIndex, float amount)
    {
        if (monthIndex < 0 || monthIndex > 11)
        {
            return;
        }

        monatsUmsaetze[monthIndex] += amount;
        gesamtUmsatz += amount;
    }
}