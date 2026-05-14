using UnityEngine;
using System;
using System.Collections.Generic;

public class CustomerServiceTest : MonoBehaviour
{
    private DataBase db;
    void Start()
    {
        db = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("customer_service_db");
        db.setupDatabase();
        db.setupCustomerTable();
        db.setupServiceTable();
        // Beispielkunde
        Customer c = new Customer
        {
            name = "Max Mustermann",
            street = "Musterstraße 12",
            postalCode = "12345",
            city = "Mittweida",
            email = "max@mustermann.de",
            phone = "1234 123456"
        };
        db.createCustomer(c);
        // Beispieldienstleistung
        Service s = new Service
        {
            name = "Kundendatenbank",
            description = "Juhu, eine Kundendatenbank! Hier können Sie alle Ihre Kunden verwalten, inklusive Kontaktinformationen, Kaufhistorie und mehr. Perfekt für kleine Unternehmen, die den Überblick behalten wollen.", 
            price = 800.0
        };
        db.createService(s);
        Debug.Log($"✅ Kunde '{c.name}' & Dienstleistung '{s.name}' wurden angelegt.");
    List<Customer> customers = db.getAllCustomers();

foreach (Customer customer in customers)
{
    Debug.Log($"Kunde: {customer.name} | {customer.email}");
}

List<Service> services = db.getAllServices();

foreach (Service service in services)
{
  Debug.Log(
    $"Service: {service.name}\n" +
    $"Beschreibung: {service.description}\n" +
    $"Preis: {service.price}€");   
}
    }
}
    
