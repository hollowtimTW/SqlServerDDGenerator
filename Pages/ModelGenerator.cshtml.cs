using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlServerDDGenerator.Services;
using System.IO.Compression;
using System.Text;

namespace SqlServerDDGenerator.Pages;

public class ModelGeneratorModel : PageModel
{
    private readonly SqlServerService _sqlService;

    public ModelGeneratorModel(SqlServerService sqlService)
    {
        _sqlService = sqlService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostGetDatabasesAsync()
    {
        var connectionInfo = GetConnectionFromSession();
        if (connectionInfo == null)
        {
            return new JsonResult(new { success = false, message = "No connection info found" });
        }

        try
        {
            var connectionString = _sqlService.BuildConnectionString(connectionInfo);
            var databases = await _sqlService.GetDatabasesAsync(connectionString);
            return new JsonResult(new { success = true, databases });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostGetTablesAsync([FromBody] DatabaseRequest request)
    {
        var connectionInfo = GetConnectionFromSession();
        if (connectionInfo == null)
        {
            return new JsonResult(new { success = false, message = "No connection info found" });
        }

        try
        {
            var tables = await _sqlService.GetTablesAsync(
                connectionInfo.ServerName,
                request.DatabaseName,
                connectionInfo.AuthType,
                connectionInfo.Username,
                connectionInfo.Password,
                connectionInfo.TrustServerCertificate
            );
            return new JsonResult(new { success = true, tables });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync([FromBody] GenerateModelRequest request)
    {
        var connectionInfo = GetConnectionFromSession();
        if (connectionInfo == null)
        {
            return new JsonResult(new { success = false, message = "No connection info found" });
        }

        try
        {
            // Create ZIP file in memory
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var tableName in request.SelectedTables)
                {
                    var columns = await _sqlService.GetTableSchemaAsync(
                        connectionInfo.ServerName,
                        request.DatabaseName,
                        tableName,
                        connectionInfo.AuthType,
                        connectionInfo.Username,
                        connectionInfo.Password,
                        connectionInfo.TrustServerCertificate
                    );
                    var modelCode = GenerateModelClass(tableName, columns, request.Namespace);
                    
                    // Get clean table name (remove schema)
                    var cleanTableName = tableName.Contains('.') ? tableName.Split('.')[1] : tableName;
                    var fileName = $"{cleanTableName}.cs";
                    
                    var entry = archive.CreateEntry(fileName);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream);
                    await writer.WriteAsync(modelCode);
                }
            }

            memoryStream.Position = 0;
            var zipData = Convert.ToBase64String(memoryStream.ToArray());
            var zipFileName = $"{request.DatabaseName}_Models_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

            return new JsonResult(new
            {
                success = true,
                message = $"Successfully generated {request.SelectedTables.Count} model class files",
                zipData,
                fileName = zipFileName
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    private string GenerateModelClass(string tableName, List<Models.ColumnInfo> columns, string namespaceName)
    {
        var sb = new StringBuilder();
        var cleanTableName = tableName.Contains('.') ? tableName.Split('.')[1] : tableName;

        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {cleanTableName}");
        sb.AppendLine("    {");

        for (int i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var csharpType = MapSqlTypeToCSharp(column.DataType, column.IsNullable == "YES");
            sb.AppendLine($"        public {csharpType} {column.ColumnName} {{ get; set; }}");
            
            // Add blank line between properties (except for the last one)
            if (i < columns.Count - 1)
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string MapSqlTypeToCSharp(string sqlType, bool isNullable)
    {
        var baseType = sqlType.ToLower() switch
        {
            "int" => "int",
            "bigint" => "long",
            "smallint" => "short",
            "tinyint" => "byte",
            "bit" => "bool",
            "decimal" or "numeric" or "money" or "smallmoney" => "decimal",
            "float" => "double",
            "real" => "float",
            "datetime" or "datetime2" or "smalldatetime" => "DateTime",
            "date" => "DateTime",
            "time" => "TimeSpan",
            "datetimeoffset" => "DateTimeOffset",
            "uniqueidentifier" => "Guid",
            "char" or "nchar" or "varchar" or "nvarchar" or "text" or "ntext" => "string",
            "binary" or "varbinary" or "image" => "byte[]",
            _ => "object"
        };

        // String and byte[] are reference types, don't need nullable modifier
        if (baseType == "string" || baseType == "byte[]" || baseType == "object")
        {
            return baseType;
        }

        return isNullable ? $"{baseType}?" : baseType;
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

public class GenerateModelRequest
{
    public string DatabaseName { get; set; } = string.Empty;
    public List<string> SelectedTables { get; set; } = new();
    public string Namespace { get; set; } = string.Empty;
}
