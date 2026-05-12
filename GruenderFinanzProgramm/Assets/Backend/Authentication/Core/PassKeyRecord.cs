public class PassKeyRecord
{
    public string userId;
    public string username;
    public string passKeyHash;
    public string recoveryKeyHash;

    public PassKeyRecord(string userId, string username, string passKeyHash, string recoveryKeyHash)
    {
        this.userId = userId;
        this.username = username;
        this.passKeyHash = passKeyHash;
        this.recoveryKeyHash = recoveryKeyHash;
    }
}