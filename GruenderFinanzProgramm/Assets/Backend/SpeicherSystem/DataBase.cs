using System.Collections.Generic;
using UnityEngine;

public class DataBase : DatabaseManager
{
    /// <summary>
    /// Setup the database and create all necessary tables
    /// </summary>
    public void setupDatabase()
    {
        createTable<User>();
        createTable<Company>();
    }

    /// <summary>
    /// Get a user by ID
    /// </summary>
    public User getUserById(int userId)
    {
        return getById<User>(userId);
    }

    /// <summary>
    /// Get all users from the database
    /// </summary>
    public List<User> getAllUsers()
    {
        return getAll<User>();
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    public int createUser(string name, int passKey, int recoveryKey, bool isLoggedIn = false)
    {
        User newUser = new User
        {
            name = name,
            passKey = passKey,
            recoveryKey = recoveryKey,
            isLoggedIn = isLoggedIn
        };
        return insert(newUser);
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    public int updateUser(User user)
    {
        return update(user);
    }

    /// <summary>
    /// Delete a user by ID
    /// </summary>
    public int deleteUser(int userId)
    {
        return delete<User>(userId);
    }

    /// <summary>
    /// Get a company by ID
    /// </summary>
    public Company getCompanyById(int companyId)
    {
        return getById<Company>(companyId);
    }

    /// <summary>
    /// Get all companies from the database
    /// </summary>
    public List<Company> getAllCompanies()
    {
        return getAll<Company>();
    }

    /// <summary>
    /// Create a new company
    /// </summary>
    public int createCompany(string name, int legalForm)
    {
        Company newCompany = new Company
        {
            name = name,
            legalForm = legalForm
        };
        return insert(newCompany);
    }

    /// <summary>
    /// Update an existing company
    /// </summary>
    public int updateCompany(Company company)
    {
        return update(company);
    }

    /// <summary>
    /// Delete a company by ID
    /// </summary>
    public int deleteCompany(int companyId)
    {
        return delete<Company>(companyId);
    }

    /// <summary>
    /// Find users by name (case-insensitive)
    /// </summary>
    public List<User> findUsersByName(string name)
    {
        return query<User>($"SELECT * FROM User WHERE name LIKE '%{name}%'");
    }

    /// <summary>
    /// Find companies by name (case-insensitive)
    /// </summary>
    public List<Company> findCompaniesByName(string name)
    {
        return query<Company>($"SELECT * FROM Company WHERE name LIKE '%{name}%'");
    }

    /// <summary>
    /// Get all logged-in users
    /// </summary>
    public List<User> getLoggedInUsers()
    {
        return query<User>("SELECT * FROM User WHERE isLoggedIn = 1");
    }

    /// <summary>
    /// Update user login status
    /// </summary>
    public int updateUserLoginStatus(int userId, bool isLoggedIn)
    {
        User user = getUserById(userId);
        if (user != null)
        {
            user.isLoggedIn = isLoggedIn;
            return updateUser(user);
        }
        return 0;
    }

    /// <summary>
    /// Get companies by legal form
    /// </summary>
    public List<Company> getCompaniesByLegalForm(int legalForm)
    {
        return query<Company>($"SELECT * FROM Company WHERE legalForm = {legalForm}");
    }
}
