namespace SqlServerDDGenerator.Models;

public class ConnectionInfo
{
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string AuthType { get; set; } = "Windows"; // Windows or SA
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool TrustServerCertificate { get; set; } = true;
}
