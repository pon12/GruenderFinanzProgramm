using UnityEngine;
public static class LookupSeeder
{
    public static void Seed(DataBase db)
    {
        // Nur befüllen wenn noch keine Einträge da sind
        string[] existingLegalForms = db.getLookupValues("LegalForm");
        if (existingLegalForms.Length == 0)
        {
            db.createLookupEntry("LegalForm", "GmbH");
            db.createLookupEntry("LegalForm", "KG");
            db.createLookupEntry("LegalForm", "AG");
            db.createLookupEntry("LegalForm", "OHG");
            db.createLookupEntry("LegalForm", "GbR");
            db.createLookupEntry("LegalForm", "UG (haftungsbeschränkt)");
            db.createLookupEntry("LegalForm", "Einzelunternehmen");
            db.createLookupEntry("LegalForm", "GmbH & Co. KG");
            db.createLookupEntry("LegalForm", "eG (Genossenschaft)");
            Debug.Log("✅ Rechtsformen wurden angelegt.");
        }
        string[] existingIndustries = db.getLookupValues("Industry");
        if (existingIndustries.Length == 0)
        {
            db.createLookupEntry("Industry", "IT & Software");
            db.createLookupEntry("Industry", "Handel");
            db.createLookupEntry("Industry", "Produktion");
            db.createLookupEntry("Industry", "Dienstleistung");
            db.createLookupEntry("Industry", "Gesundheitswesen");
            db.createLookupEntry("Industry", "Bauwesen");
            db.createLookupEntry("Industry", "Gastronomie");
            db.createLookupEntry("Industry", "Finanzwesen");
            db.createLookupEntry("Industry", "Logistik");
            Debug.Log("✅ Branchen wurden angelegt.");
        }
    }
}