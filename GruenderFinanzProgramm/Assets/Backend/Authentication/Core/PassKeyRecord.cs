public class PassKeyRecord
{
    public string userId;
    public string username;
    public string passKeyHash;
    public string recoveryKeyHash;
    public string databaseName;

    public PassKeyRecord(string userId, string username, string passKeyHash, string recoveryKeyHash, string databaseName)
    {
        this.userId = userId;
        this.username = username;
        this.passKeyHash = passKeyHash;
        this.recoveryKeyHash = recoveryKeyHash;
        this.databaseName = databaseName;
    }
}