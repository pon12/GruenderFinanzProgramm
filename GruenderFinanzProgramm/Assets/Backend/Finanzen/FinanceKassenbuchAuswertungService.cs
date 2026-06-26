using System;
using System.Collections.Generic;
using UnityEngine;

public static class FinanceKassenbuchAuswertungService
{
    public static float GetSummeEinnahmenGesamt(DataBase db)
    {
        if (db == null)
        {
            Debug.LogError("[FinanceKassenbuchAuswertungService] Datenbank ist null.");
            return 0f;
        }

        float summe = 0f;
        List<Einkommen> einnahmen = db.getAllEinkommenEntries();

        if (einnahmen == null)
        {
            return 0f;
        }

        foreach (Einkommen einnahme in einnahmen)
        {
            if (einnahme == null)
            {
                continue;
            }

            summe += einnahme.Amount;
        }

        return summe;
    }

    public static float GetSummeAusgabenGesamt(DataBase db)
    {
        if (db == null)
        {
            Debug.LogError("[FinanceKassenbuchAuswertungService] Datenbank ist null.");
            return 0f;
        }

        float summe = 0f;
        List<Ausgaben> ausgaben = db.getAllAusgabenEntries();

        if (ausgaben == null)
        {
            return 0f;
        }

        foreach (Ausgaben ausgabe in ausgaben)
        {
            if (ausgabe == null)
            {
                continue;
            }

            summe += ausgabe.Amount;
        }

        return summe;
    }

    public static float GetSaldo(DataBase db)
    {
        return GetSummeEinnahmenGesamt(db) - GetSummeAusgabenGesamt(db);
    }

    public static float GetSummeAusgabenNachKategorie(DataBase db, string kategorie)
    {
        if (db == null)
        {
            Debug.LogError("[FinanceKassenbuchAuswertungService] Datenbank ist null.");
            return 0f;
        }

        if (string.IsNullOrWhiteSpace(kategorie))
        {
            return 0f;
        }

        float summe = 0f;
        List<Ausgaben> ausgaben = db.getAllAusgabenEntries();

        if (ausgaben == null)
        {
            return 0f;
        }

        foreach (Ausgaben ausgabe in ausgaben)
        {
            if (ausgabe == null)
            {
                continue;
            }

            if (MatchesKategorieOderArt(ausgabe.Kategorie, ausgabe.Art, kategorie))
            {
                summe += ausgabe.Amount;
            }
        }

        return summe;
    }

    public static float GetSummeEinnahmenNachKategorie(DataBase db, string kategorie)
    {
        if (db == null)
        {
            Debug.LogError("[FinanceKassenbuchAuswertungService] Datenbank ist null.");
            return 0f;
        }

        if (string.IsNullOrWhiteSpace(kategorie))
        {
            return 0f;
        }

        float summe = 0f;
        List<Einkommen> einnahmen = db.getAllEinkommenEntries();

        if (einnahmen == null)
        {
            return 0f;
        }

        foreach (Einkommen einnahme in einnahmen)
        {
            if (einnahme == null)
            {
                continue;
            }

            if (MatchesKategorieOderArt(einnahme.Kategorie, einnahme.Art, kategorie))
            {
                summe += einnahme.Amount;
            }
        }

        return summe;
    }

    public static List<Ausgaben> GetAusgabenNachKategorie(DataBase db, string kategorie)
    {
        List<Ausgaben> gefilterteAusgaben = new List<Ausgaben>();

        if (db == null)
        {
            Debug.LogError("[FinanceKassenbuchAuswertungService] Datenbank ist null.");
            return gefilterteAusgaben;
        }

        if (string.IsNullOrWhiteSpace(kategorie))
        {
            return gefilterteAusgaben;
        }

        List<Ausgaben> ausgaben = db.getAllAusgabenEntries();

        if (ausgaben == null)
        {
            return gefilterteAusgaben;
        }

        foreach (Ausgaben ausgabe in ausgaben)
        {
            if (ausgabe == null)
            {
                continue;
            }

            if (IsSameText(ausgabe.Kategorie, kategorie))
            {
                gefilterteAusgaben.Add(ausgabe);
            }
        }

        return gefilterteAusgaben;
    }

    public static List<Einkommen> GetEinnahmenNachKategorie(DataBase db, string kategorie)
    {
        List<Einkommen> gefilterteEinnahmen = new List<Einkommen>();

        if (db == null)
        {
            Debug.LogError("[FinanceKassenbuchAuswertungService] Datenbank ist null.");
            return gefilterteEinnahmen;
        }

        if (string.IsNullOrWhiteSpace(kategorie))
        {
            return gefilterteEinnahmen;
        }

        List<Einkommen> einnahmen = db.getAllEinkommenEntries();

        if (einnahmen == null)
        {
            return gefilterteEinnahmen;
        }

        foreach (Einkommen einnahme in einnahmen)
        {
            if (einnahme == null)
            {
                continue;
            }

            if (IsSameText(einnahme.Kategorie, kategorie))
            {
                gefilterteEinnahmen.Add(einnahme);
            }
        }

        return gefilterteEinnahmen;
    }

    public static float GetSummeAusgabenNachBeschreibung(DataBase db, string suchtext)
    {
        if (db == null)
        {
            Debug.LogError("[FinanceKassenbuchAuswertungService] Datenbank ist null.");
            return 0f;
        }

        if (string.IsNullOrWhiteSpace(suchtext))
        {
            return 0f;
        }

        float summe = 0f;
        List<Ausgaben> ausgaben = db.getAllAusgabenEntries();

        if (ausgaben == null)
        {
            return 0f;
        }

        foreach (Ausgaben ausgabe in ausgaben)
        {
            if (ausgabe == null || string.IsNullOrWhiteSpace(ausgabe.Description))
            {
                continue;
            }

            if (ausgabe.Description.ToLower().Contains(suchtext.Trim().ToLower()))
            {
                summe += ausgabe.Amount;
            }
        }

        return summe;
    }

    public static float GetSummeEinnahmenNachBeschreibung(DataBase db, string suchtext)
    {
        if (db == null)
        {
            Debug.LogError("[FinanceKassenbuchAuswertungService] Datenbank ist null.");
            return 0f;
        }

        if (string.IsNullOrWhiteSpace(suchtext))
        {
            return 0f;
        }

        float summe = 0f;
        List<Einkommen> einnahmen = db.getAllEinkommenEntries();

        if (einnahmen == null)
        {
            return 0f;
        }

        foreach (Einkommen einnahme in einnahmen)
        {
            if (einnahme == null || string.IsNullOrWhiteSpace(einnahme.Description))
            {
                continue;
            }

            if (einnahme.Description.ToLower().Contains(suchtext.Trim().ToLower()))
            {
                summe += einnahme.Amount;
            }
        }

        return summe;
    }

    private static bool IsSameText(string value, string compareValue)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(compareValue))
        {
            return false;
        }

        return value.Trim().ToLower() == compareValue.Trim().ToLower();
    }

    private static bool MatchesKategorieOderArt(
    string kategorie,
    string art,
    string suchwert
)
    {
        return IsSameText(kategorie, suchwert) || IsSameText(art, suchwert);
    }
}