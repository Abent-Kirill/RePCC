using LinqToDB.Mapping;

namespace RePCC.Models;

[Table(Name = "Computers")]
public sealed record ComputerRecord
{
    [PrimaryKey]
    [Column(Name = "MacAddress"), NotNull]
    public string MacAddress { get; init; } = string.Empty;

    [Column(Name = "Name"), NotNull]
    public string Name { get; init; } = string.Empty;

    [Column(Name = "IsOnline"), NotNull]
    public bool IsOnline { get; set; } = true;

    public ComputerRecord(string name, string macAddress, bool isOnline)
    {
        Name = name;
        MacAddress = macAddress;
        IsOnline = isOnline;
    }
    public ComputerRecord() { }
}
