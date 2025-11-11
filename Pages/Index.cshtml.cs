using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlServerDDGenerator.Services;

namespace SqlServerDDGenerator.Pages;

public class IndexModel : PageModel
{
    private readonly SqlServerService _sqlService;

    public IndexModel(SqlServerService sqlService)
    {
        _sqlService = sqlService;
    }

    public bool IsConnected { get; set; }
    public string? ConnectedServer { get; set; }
    public string? ConnectedAuthType { get; set; }

    public void OnGet()
    {
        // Check if there's an active connection in session
        var connectionJson = HttpContext.Session.GetString("ConnectionInfo");
        if (!string.IsNullOrEmpty(connectionJson))
        {
            try
            {
                var connectionInfo = System.Text.Json.JsonSerializer.Deserialize<Models.ConnectionInfo>(connectionJson);
                if (connectionInfo != null)
                {
                    IsConnected = true;
                    ConnectedServer = connectionInfo.ServerName;
                    ConnectedAuthType = connectionInfo.AuthType;
                }
            }
            catch
            {
                // If deserialization fails, just ignore
            }
        }
    }

    public async Task<IActionResult> OnPostTestConnectionAsync([FromBody] Models.ConnectionInfo connectionInfo)
    {
        try
        {
            var connectionString = _sqlService.BuildConnectionString(connectionInfo);
            var isValid = await _sqlService.TestConnectionAsync(connectionString);

            if (isValid)
            {
                var databases = await _sqlService.GetDatabasesAsync(connectionString);
                
                // Store connection info in session
                HttpContext.Session.SetString("ConnectionInfo", System.Text.Json.JsonSerializer.Serialize(connectionInfo));
                
                return new JsonResult(new
                {
                    success = true,
                    message = "連線成功！",
                    databases = databases
                });
            }
            else
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Connection failed. Please check your credentials."
                });
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new
            {
                success = false,
                message = $"Error: {ex.Message}"
            });
        }
    }

    public IActionResult OnPostDisconnectAsync()
    {
        HttpContext.Session.Remove("ConnectionInfo");
        return new JsonResult(new { success = true });
    }

    public IActionResult OnPostGetConnectionStatusAsync()
    {
        var connectionJson = HttpContext.Session.GetString("ConnectionInfo");
        if (string.IsNullOrEmpty(connectionJson))
        {
            return new JsonResult(new { isConnected = false });
        }

        try
        {
            var connectionInfo = System.Text.Json.JsonSerializer.Deserialize<Models.ConnectionInfo>(connectionJson);
            return new JsonResult(new
            {
                isConnected = true,
                serverName = connectionInfo?.ServerName ?? "",
                authType = connectionInfo?.AuthType ?? "Windows"
            });
        }
        catch
        {
            return new JsonResult(new { isConnected = false });
        }
    }
}
