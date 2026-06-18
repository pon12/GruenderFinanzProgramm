using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DataBase : DatabaseManager
{

    public void setupDatabase()
    {
        //createTable<UserDB>();
        createTable<Company>();
        createTable<Customer>();
        createTable<Service>();
        createTable<UserDocument>();
        createTable<UserPDFDocument>();
        createTable<Ausgaben>();
        createTable<Einkommen>();
        createTable<Settings>();
        createTable<Finanzdaten>();
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
public int createCompany(string name, int legalForm, int industry, string location, string steuerNr, string gruendungsJahr, string handelsReg, string strasseuHausNr, int plz, string ustIdNr, string email, string handyNr)
{
    Company newCompany = new Company
    {
        name = name,
        legalForm = legalForm,
        industry = industry,
        location = location,
        steuerNr = steuerNr,
        gruendungsJahr = gruendungsJahr,    
        handelsReg = handelsReg,
        strasseuHausNr = strasseuHausNr,
        plz = plz,
        ustIdNr = ustIdNr,
        email = email,
        handyNr = handyNr
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
    public int createInvoice(Invoice invoice) => insert(invoice);
    public int updateInvoice(Invoice invoice) => update(invoice);
    public int deleteInvoice(int id) => delete<Invoice>(id);
    public List<Invoice> getAllInvoices() => getAll<Invoice>();
    public List<InvoiceItem> getItemsByInvoice(int invoiceId) =>
        query<InvoiceItem>($"SELECT * FROM InvoiceItem WHERE invoiceId = {invoiceId}");
    public int createInvoiceItem(InvoiceItem item) => insert(item);

    // --- Angebote ---
    public int createOffer(Offer offer) => insert(offer);
    public int updateOffer(Offer offer) => update(offer);
    public int deleteOffer(int id) => delete<Offer>(id);
    public List<Offer> getAllOffers() => getAll<Offer>();
    public List<OfferItem> getItemsByOffer(int offerId) =>
        query<OfferItem>($"SELECT * FROM OfferItem WHERE offerId = {offerId}");
    public int createOfferItem(OfferItem item) => insert(item);




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


// Settings

public int createSettings(string rechnungsNrPräfix, string startNr, string zahlungsziel, int waehrung, int dtmFormat,  
bool ustRechnung, bool autoNummer, string zahlungshinweis, string kontoInhaber, string iban, string bic, string kreditinstitut, 
bool ibanRechnung, bool logo, bool seitenzahl ,bool exportpfad, int steuersatz, bool Begleiter )
{
    Settings newSettings = new Settings
    {
        rechnungsNrPräfix = rechnungsNrPräfix,
        startNr = startNr,
        zahlungsziel = zahlungsziel,
        waehrung = waehrung,
        dtmFormat = dtmFormat,
        ustRechnung = ustRechnung,
        autoNummer = autoNummer,
        zahlungshinweis = zahlungshinweis,
        kontoInhaber = kontoInhaber,
        iban = iban,
        bic = bic,
        kreditinstitut = kreditinstitut,
        ibanRechnung = ibanRechnung,
        logo = logo,
        seitenzahl = seitenzahl,
        exportpfad = exportpfad,
        steuersatz = steuersatz,
        Begleiter = Begleiter
    };
    return insert(newSettings);
}

public List<Settings> getAllSettings()
{
    return getAll<Settings>();
}

public int deleteSettings(int id)
{
    return delete<Settings>(id);
}

public int updateSettings(Settings settings)
{
    return update(settings);
}


//Finanzdaten 

public int createFinanzdaten(int monat,int ausgaben,int einahmenTotal,int erstellteAng ,int angenommenAng, int erstellteRech ,int angenommenRech)
{
    Finanzdaten newFinanzdaten = new Finanzdaten
    {
        monat = monat,
        ausgaben = ausgaben,
        einahmenTotal = einahmenTotal,
        erstellteAng = erstellteAng,
        angenommenAng = angenommenAng,
        erstellteRech = erstellteRech,
        angenommenRech = angenommenRech
    };
    return insert(newFinanzdaten);
}

public List<Finanzdaten> getAllFinanzdaten()
{
    return getAll<Finanzdaten>();
}

public int deleteFinanzdatenMonat(int monat)
{
    return delete<Finanzdaten>(monat);
}

public int updateFinanzdaten(Finanzdaten finanzdaten)
{
    return update(finanzdaten);
}

public Finanzdaten  getFinanzdatenMonat(int monat)
{
    return query<Finanzdaten>($"SELECT * FROM Finanzdaten WHERE monat = {monat}").FirstOrDefault();;

}

public List<Finanzdaten> orderFinanzdatenASC()
{
    return query<Finanzdaten>($"SELECT * FROM Finanzdaten ORDER BY monat ASC");
}

public List<Finanzdaten> orderFinanzdatenDESC()
{
    return query<Finanzdaten>($"SELECT * FROM Finanzdaten ORDER BY monat DESC");
}

public Finanzdaten getAusgabenbyMonat(int monat)
{
    return query<Finanzdaten>($"SELECT ausgaben FROM Finanzdaten WHERE monat = {monat}").FirstOrDefault();;
}

public Finanzdaten  getEinahmenTotalbyMonat(int monat)
{
    return query<Finanzdaten>($"SELECT einahmenTotal FROM Finanzdaten WHERE monat = {monat}").FirstOrDefault();;
}

public Finanzdaten  getErstellteAngbyMonat(int monat)
{
    return query<Finanzdaten>($"SELECT erstellteAng FROM Finanzdaten WHERE monat = {monat}").FirstOrDefault();;
}

public Finanzdaten  getAngenommenAngbyMonat(int monat)
{
    return query<Finanzdaten>($"SELECT angenommenAng FROM Finanzdaten WHERE monat = {monat}").FirstOrDefault();;
}

public Finanzdaten  getErstellteRechbyMonat(int monat)
{
    return query<Finanzdaten>($"SELECT erstellteRech FROM Finanzdaten WHERE monat = {monat}").FirstOrDefault();;
}

public Finanzdaten  getAngenommenRechbyMonat(int monat)
{
    return query<Finanzdaten>($"SELECT angenommenRech FROM Finanzdaten WHERE monat = {monat}").FirstOrDefault();;
}




}

