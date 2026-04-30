using System.Collections.Generic;
using UnityEngine;

public class DataBase : DatabaseManager
{

    //Diese funktion ist hauptsächlich für testzwecke ihr könnt wenn ihr tables habt die nicht zur runtime erstellt werden hier einbauen.

    public void setupDatabase()
    {
        createTable<User>();
        createTable<Company>();
    }


//User
    public User getUserById(int userId)
    {
        return getById<User>(userId);
    }


    public List<User> getAllUsers()
    {
        return getAll<User>();
    }


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


    public int updateUser(User user)
    {
        return update(user);
    }


    public int deleteUser(int userId)
    {
        return delete<User>(userId);
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


    public int createCompany(string name, int legalForm)
    {
        Company newCompany = new Company
        {
            name = name,
            legalForm = legalForm
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

    public List<User> findUsersByName(string name)
    {
        return query<User>($"SELECT * FROM User WHERE name LIKE '%{name}%'");
    }


    public List<Company> findCompaniesByName(string name)
    {
        return query<Company>($"SELECT * FROM Company WHERE name LIKE '%{name}%'");
    }


    public List<User> getLoggedInUsers()
    {
        return query<User>("SELECT * FROM User WHERE isLoggedIn = 1");
    }


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


    public List<Company> getCompaniesByLegalForm(int legalForm)
    {
        return query<Company>($"SELECT * FROM Company WHERE legalForm = {legalForm}");
    }
}
