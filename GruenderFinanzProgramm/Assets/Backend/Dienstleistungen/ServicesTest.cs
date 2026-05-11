using UnityEngine;
using System;
using System.Collections.Generic;
public class ServiceTest : MonoBehaviour
{
    private DataBase db;
    void Start()
    {
        
        db = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("service_test_db");
        db.setupDatabase();
        db.setupServiceTable();
     
        Service s = new Service
        {
            companyId = 1,
            name = "Erstellung von Dienstleisungstabellen",
            description = "knochenharte Arbeit",
            unitPrice = 50000,     // Preis pro Stunde
            quantity = 2,         // Anzahl Stunden
            unitType = "Stunden",
            category = "Entwicklung"
        };
        db.createService(s);
        
        // Ausgabe
        Debug.Log($"Dienstleistung: {s.name}");
        Debug.Log($"Beschreibung: {s.description}");
        Debug.Log($"Einheiten: {s.quantity} {s.unitType}");
        Debug.Log($"Einzelpreis: {s.unitPrice} €");
        Debug.Log($"Gesamtpreis (berechnet): {s.totalPrice} €");
        // Alle Services anzeigen
        List<Service> all = db.getAllServices();
        Debug.Log($"Gesamt gespeicherte Services: {all.Count}");
    }
}