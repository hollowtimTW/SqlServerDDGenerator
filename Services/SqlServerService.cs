using Microsoft.Data.SqlClient;
using SqlServerDDGenerator.Models;
using System.Data;
using System.Text;

namespace SqlServerDDGenerator.Services;

public class SqlServerService
{
    private string? _connectionString;

    public string BuildConnectionString(Models.ConnectionInfo info)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = info.ServerName,
            InitialCatalog = "master",
            TrustServerCertificate = info.TrustServerCertificate
        };

        if (info.AuthType == "Windows")
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = info.Username;
            builder.Password = info.Password;
        }

        _connectionString = builder.ConnectionString;
        return _connectionString;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetDatabasesAsync(string connectionString)
    {
        var databases = new List<string>();
        
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var command = new SqlCommand(@"
            SELECT d.name 
            FROM sys.databases d
            WHERE d.name NOT IN ('master', 'tempdb', 'model', 'msdb')
            AND HAS_PERMS_BY_NAME(d.name, 'DATABASE', 'CONNECT') = 1
            AND d.state_desc = 'ONLINE'
            ORDER BY d.name", connection);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            databases.Add(reader.GetString(0));
        }

        return databases;
    }

    public async Task<List<TableInfo>> GetTablesAsync(string serverName, string databaseName, string authType, string username, string password, bool trustServerCertificate = true)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = serverName,
            InitialCatalog = databaseName,
            TrustServerCertificate = trustServerCertificate
        };

        if (authType == "Windows")
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = username;
            builder.Password = password;
        }

        var tables = new List<TableInfo>();
        
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var command = new SqlCommand(@"
            SELECT 
                s.name AS SchemaName,
                t.name AS TableName
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            ORDER BY s.name, t.name", connection);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(new TableInfo
            {
                SchemaName = reader.GetString(0),
                TableName = reader.GetString(1)
            });
        }

        return tables;
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(string serverName, string databaseName, string schemaName, string tableName, string authType, string username, string password, bool trustServerCertificate = true)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = serverName,
            InitialCatalog = databaseName,
            TrustServerCertificate = trustServerCertificate
        };

        if (authType == "Windows")
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = username;
            builder.Password = password;
        }

        var columns = new List<ColumnInfo>();
        
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var command = new SqlCommand(@"
            SELECT 
                c.name AS ColumnName,
                t.name AS DataType,
                CASE 
                    WHEN t.name IN ('varchar', 'nvarchar', 'char', 'nchar') THEN 
                        CASE WHEN c.max_length = -1 THEN 'MAX' 
                             WHEN t.name IN ('nvarchar', 'nchar') THEN CAST(c.max_length/2 AS VARCHAR(10))
                             ELSE CAST(c.max_length AS VARCHAR(10)) 
                        END
                    WHEN t.name IN ('decimal', 'numeric') THEN CAST(c.precision AS VARCHAR(10)) + ',' + CAST(c.scale AS VARCHAR(10))
                    ELSE ''
                END AS MaxLength,
                CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable,
                CASE WHEN pk.column_id IS NOT NULL THEN 'YES' ELSE 'NO' END AS IsPrimaryKey,
                CASE WHEN c.is_identity = 1 THEN 'YES' ELSE 'NO' END AS IsIdentity,
                ISNULL(dc.definition, '') AS DefaultValue,
                ISNULL(ep.value, '') AS Description
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            LEFT JOIN (
                SELECT ic.object_id, ic.column_id
                FROM sys.index_columns ic
                INNER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                WHERE i.is_primary_key = 1
            ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
            LEFT JOIN sys.default_constraints dc ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            LEFT JOIN sys.extended_properties ep ON c.object_id = ep.major_id AND c.column_id = ep.minor_id AND ep.name = 'MS_Description'
            WHERE c.object_id = OBJECT_ID(@TableFullName)
            ORDER BY c.column_id", connection);

        command.Parameters.AddWithValue("@TableFullName", $"{schemaName}.{tableName}");

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnInfo
            {
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1),
                MaxLength = reader.GetString(2),
                IsNullable = reader.GetString(3),
                IsPrimaryKey = reader.GetString(4),
                IsIdentity = reader.GetString(5),
                DefaultValue = reader.GetString(6),
                Description = reader.GetString(7)
            });
        }

        return columns;
    }

    public async Task<List<ColumnInfo>> GetTableSchemaAsync(string serverName, string databaseName, string tableFullName, string authType, string username, string password, bool trustServerCertificate = true)
    {
        // Parse schema and table name
        string schemaName, tableName;
        if (tableFullName.Contains('.'))
        {
            var parts = tableFullName.Split('.');
            schemaName = parts[0];
            tableName = parts[1];
        }
        else
        {
            schemaName = "dbo";
            tableName = tableFullName;
        }

        return await GetColumnsAsync(serverName, databaseName, schemaName, tableName, authType, username, password, trustServerCertificate);
    }

    public async Task<string> GenerateMarkdownAsync(string serverName, string databaseName, List<TableInfo> selectedTables, string authType, string username, string password, bool trustServerCertificate = true)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Database: {databaseName}");
        sb.AppendLine($"**Server:** {serverName}");
        sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var table in selectedTables.Where(t => t.IsSelected))
        {
            sb.AppendLine($"## Table: {table.FullName}");
            sb.AppendLine();

            var columns = await GetColumnsAsync(serverName, databaseName, table.SchemaName, table.TableName, authType, username, password, trustServerCertificate);

            sb.AppendLine("| Column Name | Data Type | Length | Nullable | Primary Key | Identity | Default | Description |");
            sb.AppendLine("|-------------|-----------|--------|----------|-------------|----------|---------|-------------|");

            foreach (var col in columns)
            {
                var dataTypeDisplay = string.IsNullOrEmpty(col.MaxLength) 
                    ? col.DataType 
                    : $"{col.DataType}({col.MaxLength})";

                sb.AppendLine($"| {col.ColumnName} | {dataTypeDisplay} | {col.MaxLength} | {col.IsNullable} | {col.IsPrimaryKey} | {col.IsIdentity} | {col.DefaultValue} | {col.Description} |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // 已不再需要儲存到伺服器
    // public async Task SaveMarkdownFileAsync(string content, string databaseName)
    // {
    //     var fileName = $"{databaseName}_DD_{DateTime.Now:yyyyMMdd_HHmmss}.md";
    //     var outputDir = Path.Combine("wwwroot", "output");
    //     
    //     // 確保輸出資料夾存在
    //     if (!Directory.Exists(outputDir))
    //     {
    //         Directory.CreateDirectory(outputDir);
    //     }
    //     
    //     var filePath = Path.Combine(outputDir, fileName);
    //     await File.WriteAllTextAsync(filePath, content);
    // }
}
