namespace SqlServerDDGenerator.Models;

public class TableInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string FullName => $"{SchemaName}.{TableName}";
    public bool IsSelected { get; set; }
}
