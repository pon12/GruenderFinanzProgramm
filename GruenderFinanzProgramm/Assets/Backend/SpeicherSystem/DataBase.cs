using System.Collections.Generic;
using UnityEngine;
public class DataBase : DatabaseManager
{

    public void setupDatabase()
    {
        //createTable<UserDB>();
        createTable<Company>();
        createTable<Customer>();
        createTable<Service>();
        createTable<UserDocument>();
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

    public int createEinkommen(float amount, string description)
    {
        return insert(new Einkommen { Amount = amount, Description = description });
    }

    public int createAusgaben(float amount, string description)
    {
        return insert(new Ausgaben { Amount = amount, Description = description });
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

}
