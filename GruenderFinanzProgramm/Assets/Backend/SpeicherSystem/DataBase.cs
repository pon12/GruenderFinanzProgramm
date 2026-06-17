using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DataBase : DatabaseManager
{
    public void setupDatabase()
    {
        createTable<Company>();
        createTable<Customer>();
        createTable<Service>();
        createTable<UserDocument>();
        createTable<UserPDFDocument>();
        createTable<Ausgaben>();
        createTable<Einkommen>();
        createTable<Dauerauftrag>();
        createTable<GruenderpfadEintrag>();
        createTable<Offer>();
        createTable<OfferItem>();
        createTable<Invoice>();
        createTable<InvoiceItem>();
    }
    public void setupAuthDB()

    {
        createTable<UserDB>();
    }


    //User Auth DB
    public UserDB getUserById(int userId)
    {
        return getById<UserDB>(userId);
    }


    public List<UserDB> getAllUsers()
    {
        return getAll<UserDB>();
    }


    public int createUser(string name, string passKeyHash, string recoveryPassKeyHash)
    {
        UserDB newUser = new UserDB
        {
            name = name,
            passKeyHash = PasswordHashing.hashPassword(passKeyHash),
            recoveryPassKeyHash = PasswordHashing.hashPassword(recoveryPassKeyHash)
        };
        return insert(newUser);
    }


    public int updateUser(UserDB user)
    {
        return update(user);
    }


    public int deleteUser(int userId)
    {
        return delete<UserDB>(userId);
    }
    //Company
    public Company getCompanyById(int companyId)
    {
        return getById<Company>(companyId);
    }
    public List<Company> getAllCompanies()
    {
        return getAll<Company>();
    }
    public int createCompany(string name, int legalForm, int industry, string location)
    {
        Company newCompany = new Company
        {
            name = name,
            legalForm = legalForm,
            industry = industry,
            location = location
        };
        return insert(newCompany);
    }
    public int updateCompany(Company company)
    {
        return update(company);
    }
    public int deleteCompany(int companyId)
    {
        return delete<Company>(companyId);
    }

    //Login/Auth

    public List<UserDB> findUsersByName(string name)
    {
        return query<UserDB>($"SELECT * FROM UserDB WHERE name LIKE '%{name}%'");
    }


    public List<Company> findCompaniesByName(string name)
    {
        return query<Company>($"SELECT * FROM Company WHERE name LIKE '%{name}%'");
    }


    public List<UserDB> getLoggedInUsers()
    {
        return query<UserDB>("SELECT * FROM UserDB WHERE isLoggedIn = 1");
    }

    public List<Company> getCompaniesByLegalForm(int legalForm)
    {
        return query<Company>($"SELECT * FROM Company WHERE legalForm = {legalForm}");
    }

    // Tabellen anlegen
    public void setupInvoiceAndOfferTables()
    {
        createTable<Invoice>();
        createTable<InvoiceItem>();
        createTable<Offer>();
        createTable<OfferItem>();
    }
    // --- Rechnungen ---

    public int createInvoice(Invoice invoice)
    {
        return insert(invoice);
    }
    public int updateInvoice(Invoice invoice)
    {
        return update(invoice);
    }
    public int deleteInvoice(int id)
    {
        return delete<Invoice>(id);
    }
    public List<Invoice> getAllInvoices()
    {
        return getAll<Invoice>();
    }
    public Invoice getInvoiceById(int id)
    {
        return getById<Invoice>(id);
    }
    public List<InvoiceItem> getItemsByInvoice(int invoiceId)
    {
        return query<InvoiceItem>(
            $"SELECT * FROM InvoiceItem WHERE invoiceId = {invoiceId}"
        );
    }
    public int createInvoiceItem(InvoiceItem item)
    {
        return insert(item);
    }
    public int updateInvoiceItem(InvoiceItem item)
    {
        return update(item);
    }
    public int deleteInvoiceItem(int id)
    {
        return delete<InvoiceItem>(id);
    }

    // --- Angebote ---

    public int createOffer(Offer offer)
    {
        return insert(offer);
    }
    public int updateOffer(Offer offer)
    {
        return update(offer);
    }
    public int deleteOffer(int id)
    {
        return delete<Offer>(id);
    }
    public List<Offer> getAllOffers()
    {
        return getAll<Offer>();
    }
    public Offer getOfferById(int id)
    {
        return getById<Offer>(id);
    }
    public List<OfferItem> getItemsByOffer(int offerId)
    {
        return query<OfferItem>(
            $"SELECT * FROM OfferItem WHERE offerId = {offerId}"
        );
    }
    public int createOfferItem(OfferItem item)
    {
        return insert(item);
    }
    public int updateOfferItem(OfferItem item)
    {
        return update(item);
    }
    public int deleteOfferItem(int id)
    {
        return delete<OfferItem>(id);
    }

    // ---Customer---
    public void setupCustomerTable()
    {
        createTable<Customer>();
    }

    public int createCustomer(Customer customer)
    {
        customer.lastUpdated = System.DateTime.Now;
        return insert(customer);
    }

    public List<Customer> getAllCustomers()
    {
        return getAll<Customer>();
    }

    public Customer getCustomerById(int customerId)
    {
        return getById<Customer>(customerId);
    }

    public int updateCustomer(Customer customer)
    {
        customer.lastUpdated = System.DateTime.Now;
        return update(customer);
    }

    public int deleteCustomer(int customerId)
    {
        return delete<Customer>(customerId);
    }


    // --- Service ---

    public void setupServiceTable()
    {
        createTable<Service>();
    }

    public int createService(Service service)
    {
        service.lastUpdated = System.DateTime.Now;
        return insert(service);
    }

    public List<Service> getAllServices()
    {
        return getAll<Service>();
    }

    public Service getServiceById(int serviceId)
    {
        return getById<Service>(serviceId);
    }

    public int updateService(Service service)
    {
        service.lastUpdated = System.DateTime.Now;
        return update(service);
    }

    public int deleteService(int serviceId)
    {
        return delete<Service>(serviceId);
    }



    // --- Lookup (Allgemeine Nachschlagetabelle) --- 

    public void setupLookupTable()
    {
        createTable<LookupEntry>();
    }
    public int createLookupEntry(string category, string value)
    {
        return insert(new LookupEntry { category = category, value = value });
    }
    public string[] getLookupValues(string category)
    {
        var results = query<LookupEntry>(
            $"SELECT * FROM LookupEntry WHERE category = '{category}'"
        );
        string[] output = new string[results.Count];
        for (int i = 0; i < results.Count; i++)
            output[i] = results[i].value;
        return output;
    }
    public int deleteLookupEntry(int id)
    {
        return delete<LookupEntry>(id);
    }


    //Kassenbuch

    public void setupKassenbuchTable()
    {
        createTable<Einkommen>();
        createTable<Ausgaben>();
        createTable<Dauerauftrag>();
    }

    public int createEinkommen(float amount, string description, string datum)
    {
        return insert(new Einkommen { Amount = amount, Description = description, Datum = datum });
    }

    public int createAusgaben(float amount, string description, string datum)
    {
        return insert(new Ausgaben { Amount = amount, Description = description, Datum = datum });
    }

    public List<Einkommen> getAllEinkommenEntries()
    {
        return getAll<Einkommen>();
    }
    public List<Ausgaben> getAllAusgabenEntries()
    {
        return getAll<Ausgaben>();
    }

    public int deleteEinkommen(int id)
    {
        return delete<Einkommen>(id);
    }
    public int deleteAusgaben(int id)
    {
        return delete<Ausgaben>(id);
    }

    public float getTotalEinkommen()
    {
        return getAllEinkommenEntries().Sum(e => e.Amount);
    }

    public float getTotalAusgaben()
    {
        return getAllAusgabenEntries().Sum(a => a.Amount);
    }
    public float getDifferenz()
    {
        return getTotalEinkommen() - getTotalAusgaben();
    }

    public List<Ausgaben> getAusgabebyDatum(string datum)
    {
        return query<Ausgaben>($"SELECT * FROM Ausgaben WHERE Datum = '{datum}'");
    }
    public List<Einkommen> getEinkommenbyDatum(string datum)
    {
        return query<Einkommen>($"SELECT * FROM Einkommen WHERE Datum = '{datum}'");
    }



    //Documents
    public int createUserDocument(int documentType, string title, string text)
    {
        UserDocument document = new UserDocument
        {
            documentType = documentType,
            title = title,
            text = text
        };

        return insert(document);
    }

    public List<UserDocument> getAllUserDocuments()
    {
        return getAll<UserDocument>();
    }


    // --- Buissnesplan ---

    public void setupBusinessPlanTable()
    {
        createTable<Antworten>();
    }

    public int createAntwort(int antwort)
    {
        Antworten newAntwort = new Antworten
        {
            Antwort = antwort
        };
        return insert(newAntwort);
    }

    public List<Antworten> getAllAntworten()
    {
        return getAll<Antworten>();
    }

    public int deleteAntwort(int id)
    {
        return delete<Antworten>(id);
    }

    public int deleteAllAntworten()
    {
        List<Antworten> allAntworten = getAllAntworten();
        int count = 0;
        foreach (var antwort in allAntworten)
        {
            count += deleteAntwort(antwort.Id);
        }
        return count;
    }

    // --- User PDFs ---

    public int createUserPDFDocument(UserPDFDocument document)
    {
        document.uploadedAt = System.DateTime.Now;
        return insert(document);
    }

    public UserPDFDocument getUserPDFDocumentById(int pdfId, int userId)
    {
        UserPDFDocument document = getById<UserPDFDocument>(pdfId);

        if (document == null || document.userId != userId)
            return null;

        return document;
    }

    public List<UserPDFDocument> getPDFDocumentsByUser(int userId)
    {
        return query<UserPDFDocument>(
            $"SELECT * FROM UserPDFDocument WHERE userId = {userId} ORDER BY uploadedAt DESC"
        );
    }

    public int deleteUserPDFDocument(int pdfId, int userId)
    {
        UserPDFDocument document = getUserPDFDocumentById(pdfId, userId);

        if (document == null)
            return 0;

        return delete<UserPDFDocument>(pdfId);
    }




    // Änderungen Alex
    // --- Daueraufträge ---

    public int createDauerauftrag(string typ, float amount, string description, string startDatum, string naechstesDatum, int intervallTyp)
    {
        Dauerauftrag dauerauftrag = new Dauerauftrag
        {
            typ = typ,
            amount = amount,
            description = description,
            startDatum = startDatum,
            naechstesDatum = naechstesDatum,
            intervallTyp = intervallTyp,
            isActive = true,
            lastUpdated = System.DateTime.Now
        };

        return insert(dauerauftrag);
    }

    public int createDauerauftrag(Dauerauftrag dauerauftrag)
    {
        dauerauftrag.lastUpdated = System.DateTime.Now;
        return insert(dauerauftrag);
    }

    public List<Dauerauftrag> getAllDauerauftraege()
    {
        return getAll<Dauerauftrag>();
    }

    public Dauerauftrag getDauerauftragById(int id)
    {
        return getById<Dauerauftrag>(id);
    }

    public int updateDauerauftrag(Dauerauftrag dauerauftrag)
    {
        dauerauftrag.lastUpdated = System.DateTime.Now;
        return update(dauerauftrag);
    }

    public int deleteDauerauftrag(int id)
    {
        return delete<Dauerauftrag>(id);
    }

    public List<Dauerauftrag> getActiveDauerauftraege()
    {
        return query<Dauerauftrag>("SELECT * FROM Dauerauftrag WHERE isActive = 1");
    }

    public int deactivateDauerauftrag(int id)
    {
        Dauerauftrag dauerauftrag = getDauerauftragById(id);

        if (dauerauftrag == null)
        {
            Debug.LogWarning("[Dauerauftrag] Kein Dauerauftrag mit ID " + id + " gefunden.");
            return 0;
        }

        dauerauftrag.isActive = false;
        dauerauftrag.lastUpdated = System.DateTime.Now;

        return update(dauerauftrag);
    }


    public void uebernehmeFaelligeDauerauftraegeInsKassenbuch()
    {
        List<Dauerauftrag> aktiveDauerauftraege = getActiveDauerauftraege();
        System.DateTime heute = System.DateTime.Today;

        foreach (Dauerauftrag dauerauftrag in aktiveDauerauftraege)
        {
            if (!tryParseKassenbuchDatum(dauerauftrag.naechstesDatum, out System.DateTime naechstesDatum))
            {
                Debug.LogWarning("[Dauerauftrag] Ungültiges nächstes Datum bei Dauerauftrag ID " + dauerauftrag.id);
                continue;
            }

            if (naechstesDatum > heute)
            {
                continue;
            }

            string buchungsDatum = naechstesDatum.ToString("dd.MM.yyyy");

            if (dauerauftrag.typ == "Einnahme")
            {
                createEinkommen(dauerauftrag.amount, dauerauftrag.description, buchungsDatum);
            }
            else if (dauerauftrag.typ == "Ausgabe")
            {
                createAusgaben(dauerauftrag.amount, dauerauftrag.description, buchungsDatum);
            }
            else
            {
                Debug.LogWarning("[Dauerauftrag] Unbekannter Typ bei Dauerauftrag ID " + dauerauftrag.id + ": " + dauerauftrag.typ);
                continue;
            }

            dauerauftrag.naechstesDatum = berechneNaechstesDauerauftragDatum(naechstesDatum, dauerauftrag.intervallTyp);
            dauerauftrag.lastUpdated = System.DateTime.Now;

            updateDauerauftrag(dauerauftrag);

            Debug.Log("[Dauerauftrag] Übernommen: " + dauerauftrag.description + " am " + buchungsDatum);
        }
    }

    private string berechneNaechstesDauerauftragDatum(System.DateTime aktuellesDatum, int intervallTyp)
    {
        System.DateTime neuesDatum;

        switch (intervallTyp)
        {
            case 1:
                neuesDatum = aktuellesDatum.AddMonths(1);
                break;

            case 2:
                neuesDatum = aktuellesDatum.AddYears(1);
                break;

            default:
                Debug.LogWarning("[Dauerauftrag] Unbekannter IntervallTyp: " + intervallTyp + ". Es wird monatlich verwendet.");
                neuesDatum = aktuellesDatum.AddMonths(1);
                break;
        }

        return neuesDatum.ToString("dd.MM.yyyy");
    }

    private bool tryParseKassenbuchDatum(string datum, out System.DateTime parsedDate)
    {
        return System.DateTime.TryParseExact(
            datum,
            "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out parsedDate
        );
    }

    // --- Gründerpfad ---

    public int createGruenderpfadEintrag(int meilenstein, string beschreibung, bool erledigt)
    {
        GruenderpfadEintrag eintrag = new GruenderpfadEintrag
        {
            meilenstein = meilenstein,
            beschreibung = beschreibung,
            erledigt = erledigt,
            lastUpdated = System.DateTime.Now
        };

        return insert(eintrag);
    }

    public List<GruenderpfadEintrag> getAllGruenderpfadEintraege()
    {
        return getAll<GruenderpfadEintrag>();
    }

    public GruenderpfadEintrag getGruenderpfadEintragById(int id)
    {
        return getById<GruenderpfadEintrag>(id);
    }

    public int updateGruenderpfadEintrag(GruenderpfadEintrag eintrag)
    {
        eintrag.lastUpdated = System.DateTime.Now;
        return update(eintrag);
    }

    public int deleteGruenderpfadEintrag(int id)
    {
        return delete<GruenderpfadEintrag>(id);
    }

    public List<GruenderpfadEintrag> getOffeneGruenderpfadEintraege()
    {
        return query<GruenderpfadEintrag>("SELECT * FROM GruenderpfadEintrag WHERE erledigt = 0");
    }

    public List<GruenderpfadEintrag> getErledigteGruenderpfadEintraege()
    {
        return query<GruenderpfadEintrag>("SELECT * FROM GruenderpfadEintrag WHERE erledigt = 1");
    }

    public int setGruenderpfadEintragErledigt(int id, bool erledigt)
    {
        GruenderpfadEintrag eintrag = getGruenderpfadEintragById(id);

        if (eintrag == null)
        {
            Debug.LogWarning("[Gruenderpfad] Kein Eintrag mit ID " + id + " gefunden.");
            return 0;
        }

        eintrag.erledigt = erledigt;
        eintrag.lastUpdated = System.DateTime.Now;

        return update(eintrag);
    }

}
