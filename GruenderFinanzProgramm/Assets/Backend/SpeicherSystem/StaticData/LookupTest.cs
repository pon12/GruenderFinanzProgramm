using UnityEngine;
public class LookupTest : MonoBehaviour
{
    private DataBase db;
    void Start()
    {
        db = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("lookup_test_db");
        db.setupDatabase();
        db.setupLookupTable();
        // Standardwerte befüllen
        LookupSeeder.Seed(db);
        // Ausgabe prüfen
        string[] legalForms = db.getLookupValues("LegalForm");
        Debug.Log($"Rechtsformen ({legalForms.Length}):");
        foreach (var f in legalForms)
            Debug.Log($"  → {f}");
        string[] industries = db.getLookupValues("Industry");
        Debug.Log($"Branchen ({industries.Length}):");
        foreach (var i in industries)
            Debug.Log($"  → {i}");
    }
}