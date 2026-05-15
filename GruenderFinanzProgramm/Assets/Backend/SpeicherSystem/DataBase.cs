using System.Collections.Generic;
using UnityEngine;

public class DataBase : DatabaseManager
{

    //Diese funktion ist hauptsächlich für testzwecke ihr könnt wenn ihr tables habt die nicht zur runtime erstellt werden hier einbauen.
    // setupDatabase bleibt für alte Tests unverändert.
    // AuthUserDB wird separat über setupAuthDatabase() erstellt.
    public void setupDatabase()
    {
        createTable<UserDB>();
        createTable<Company>();

    }

    public void setupUserDatabase()
    {
        createTable<UserDB>();
        createTable<Company>();
    }

    public void setupAuthDatabase()
    {
        createTable<AuthUserDB>();
    }


    //User
    public UserDB getUserById(int userId)
    {
        return getById<UserDB>(userId);
    }


    public List<UserDB> getAllUsers()
    {
        return getAll<UserDB>();
    }


    public int createUser(string name)
    {
        UserDB newUser = new UserDB
        {
            name = name,
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


    // Erweiterung durch Alex: 

    public int createAuthUser(string userId, string username, string passKeyHash, string recoveryKeyHash, string databaseName)
    {
        AuthUserDB newAuthUser = new AuthUserDB
        {
            userId = userId,
            username = username,
            passKeyHash = passKeyHash,
            recoveryKeyHash = recoveryKeyHash,
            databaseName = databaseName
        };

        return insert(newAuthUser);
    }

    public List<AuthUserDB> getAllAuthUsers()
    {
        return getAll<AuthUserDB>();
    }

    public bool authUsernameExistsExact(string username)
    {
        List<AuthUserDB> users = getAllAuthUsers();

        foreach (AuthUserDB user in users)
        {
            if (user.username == username)
            {
                return true;
            }
        }

        return false;
    }

    public AuthUserDB getAuthUserByPassKeyHash(string passKeyHash)
    {
        List<AuthUserDB> users = getAllAuthUsers();

        foreach (AuthUserDB user in users)
        {
            if (user.passKeyHash == passKeyHash)
            {
                return user;
            }
        }

        return null;
    }

    public AuthUserDB getAuthUserByRecoveryKeyHash(string recoveryKeyHash)
    {
        List<AuthUserDB> users = getAllAuthUsers();

        foreach (AuthUserDB user in users)
        {
            if (user.recoveryKeyHash == recoveryKeyHash)
            {
                return user;
            }
        }

        return null;
    }

    public bool authPassKeyHashExists(string passKeyHash)
    {
        return getAuthUserByPassKeyHash(passKeyHash) != null;
    }

    public bool authRecoveryKeyHashExists(string recoveryKeyHash)
    {
        return getAuthUserByRecoveryKeyHash(recoveryKeyHash) != null;
    }

    public int updateAuthUserPassKeyHash(AuthUserDB user, string newPassKeyHash)
    {
        user.passKeyHash = newPassKeyHash;
        return update(user);
    }

}
