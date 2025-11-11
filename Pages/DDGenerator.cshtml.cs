using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlServerDDGenerator.Models;
using SqlServerDDGenerator.Services;

namespace SqlServerDDGenerator.Pages;

public class DDGeneratorModel : PageModel
{
    private readonly SqlServerService _sqlService;

    public DDGeneratorModel(SqlServerService sqlService)
    {
        _sqlService = sqlService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostGetTablesAsync([FromBody] DatabaseRequest request)
    {
        try
        {
            var connectionInfo = GetConnectionFromSession();
            if (connectionInfo == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "No active connection. Please connect on the home page first."
                });
            }

            var tables = await _sqlService.GetTablesAsync(
                connectionInfo.ServerName,
                request.DatabaseName,
                connectionInfo.AuthType,
                connectionInfo.Username ?? "",
                connectionInfo.Password ?? "",
                connectionInfo.TrustServerCertificate
            );

            return new JsonResult(new
            {
                success = true,
                message = $"Loaded {tables.Count} tables",
                tables = tables.Select(t => new { t.SchemaName, t.TableName, t.FullName }).ToList()
            });
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

    public async Task<IActionResult> OnPostGenerateAsync([FromBody] GenerateRequest request)
    {
        try
        {
            var connectionInfo = GetConnectionFromSession();
            if (connectionInfo == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "No active connection. Please connect on the home page first."
                });
            }

            var allTables = await _sqlService.GetTablesAsync(
                connectionInfo.ServerName,
                request.DatabaseName,
                connectionInfo.AuthType,
                connectionInfo.Username ?? "",
                connectionInfo.Password ?? "",
                connectionInfo.TrustServerCertificate
            );

            foreach (var table in allTables)
            {
                if (request.SelectedTables.Contains(table.FullName))
                {
                    table.IsSelected = true;
                }
            }

            var selectedCount = allTables.Count(t => t.IsSelected);
            if (selectedCount == 0)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Please select at least one table."
                });
            }

            var markdown = await _sqlService.GenerateMarkdownAsync(
                connectionInfo.ServerName,
                request.DatabaseName,
                allTables,
                connectionInfo.AuthType,
                connectionInfo.Username ?? "",
                connectionInfo.Password ?? "",
                connectionInfo.TrustServerCertificate
            );

            return new JsonResult(new
            {
                success = true,
                message = $"Successfully generated DD for {selectedCount} tables!",
                markdown = markdown,
                databaseName = request.DatabaseName,
                fileName = $"{request.DatabaseName}_DD_{DateTime.Now:yyyyMMdd_HHmmss}.md"
            });
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

    public async Task<IActionResult> OnPostGetDatabasesAsync()
    {
        try
        {
            var connectionInfo = GetConnectionFromSession();
            if (connectionInfo == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "No active connection. Please connect on the home page first."
                });
            }

            var connectionString = _sqlService.BuildConnectionString(connectionInfo);
            var databases = await _sqlService.GetDatabasesAsync(connectionString);

            return new JsonResult(new
            {
                success = true,
                databases = databases
            });
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

    private Models.ConnectionInfo? GetConnectionFromSession()
    {
        var connectionJson = HttpContext.Session.GetString("ConnectionInfo");
        if (string.IsNullOrEmpty(connectionJson))
            return null;

        return System.Text.Json.JsonSerializer.Deserialize<Models.ConnectionInfo>(connectionJson);
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

public class DatabaseRequest
{
    public string DatabaseName { get; set; } = string.Empty;
}

public class GenerateRequest
{
    public string DatabaseName { get; set; } = string.Empty;
    public List<string> SelectedTables { get; set; } = new();
}
