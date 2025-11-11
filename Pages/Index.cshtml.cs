using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlServerDDGenerator.Models;
using SqlServerDDGenerator.Services;

namespace SqlServerDDGenerator.Pages;

public class IndexModel : PageModel
{
    private readonly SqlServerService _sqlService;

    public IndexModel(SqlServerService sqlService)
    {
        _sqlService = sqlService;
    }

    public void OnGet()
    {
    }

    // AJAX API: 測試連線並取得資料庫列表
    public async Task<IActionResult> OnPostTestConnectionAsync([FromBody] Models.ConnectionInfo connectionInfo)
    {
        try
        {
            var connectionString = _sqlService.BuildConnectionString(connectionInfo);
            var isValid = await _sqlService.TestConnectionAsync(connectionString);

            if (isValid)
            {
                var databases = await _sqlService.GetDatabasesAsync(connectionString);
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
                    message = "連線失敗，請檢查連線資訊。"
                });
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new
            {
                success = false,
                message = $"錯誤: {ex.Message}"
            });
        }
    }

    // AJAX API: 取得資料表列表
    public async Task<IActionResult> OnPostGetTablesAsync([FromBody] DatabaseRequest request)
    {
        try
        {
            var tables = await _sqlService.GetTablesAsync(
                request.ServerName,
                request.DatabaseName,
                request.AuthType,
                request.Username ?? "",
                request.Password ?? "",
                request.TrustServerCertificate
            );

            return new JsonResult(new
            {
                success = true,
                message = $"已載入 {tables.Count} 個資料表",
                tables = tables.Select(t => new { t.SchemaName, t.TableName, t.FullName }).ToList()
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new
            {
                success = false,
                message = $"錯誤: {ex.Message}"
            });
        }
    }

    // AJAX API: 產生 DD 文件
    public async Task<IActionResult> OnPostGenerateAsync([FromBody] GenerateRequest request)
    {
        try
        {
            var allTables = await _sqlService.GetTablesAsync(
                request.ServerName,
                request.DatabaseName,
                request.AuthType,
                request.Username ?? "",
                request.Password ?? "",
                request.TrustServerCertificate
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
                    message = "請至少選擇一個資料表。"
                });
            }

            var markdown = await _sqlService.GenerateMarkdownAsync(
                request.ServerName,
                request.DatabaseName,
                allTables,
                request.AuthType,
                request.Username ?? "",
                request.Password ?? "",
                request.TrustServerCertificate
            );

            // 不再儲存到伺服器，只回傳給前端下載

            return new JsonResult(new
            {
                success = true,
                message = $"成功產生 {selectedCount} 個資料表的 DD 文件！",
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
                message = $"錯誤: {ex.Message}"
            });
        }
    }
}

public class DatabaseRequest
{
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string AuthType { get; set; } = "Windows";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool TrustServerCertificate { get; set; } = true;
}

public class GenerateRequest
{
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string AuthType { get; set; } = "Windows";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool TrustServerCertificate { get; set; } = true;
    public List<string> SelectedTables { get; set; } = new();
}
