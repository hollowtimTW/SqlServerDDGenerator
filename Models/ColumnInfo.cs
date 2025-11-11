namespace SqlServerDDGenerator.Models;

public class ColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string MaxLength { get; set; } = string.Empty;
    public string IsNullable { get; set; } = string.Empty;
    public string IsPrimaryKey { get; set; } = string.Empty;
    public string IsIdentity { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
