using SQLite;

namespace RePCC.Models;

public sealed record ComputerRecord
{
    [PrimaryKey]
    public string MacAddress { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public ComputerRecord(string name, string macAddress)
    {
        Name = name;
        MacAddress = macAddress;
    }
    public ComputerRecord() { }
}
