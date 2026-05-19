using System.Collections.Generic;
using UnityEngine;
public class DataBase : DatabaseManager
{

    public void setupDatabase()
    {
        //createTable<UserDB>();
        createTable<Company>();
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
        return insert(customer);
    }

    public List<Customer> getAllCustomers()
    {
        return getAll<Customer>();
    }

    // ---Services---

    public void setupServiceTable()
    {
        createTable<Service>();
    }

    public int createService(Service service)
    {
        return insert(service);
    }

    public List<Service> getAllServices()
    {
        return getAll<Service>();
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


}
