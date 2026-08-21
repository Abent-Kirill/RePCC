using System.Net;
using System.Net.NetworkInformation;

namespace RePCC.Models;

public static class ComputerExtentions
{
    /// <summary>
    /// Преобразовние Computer в ComputerRecord
    /// </summary>
    /// <param name="computer"></param>
    /// <returns>ComputerRecord</returns>
    public static ComputerRecord ToRecord(this Computer computer) => new(computer.Name, computer.MACAddress.ToString(), computer.IsOnline);

    /// <summary>
    /// Преобразование ComputerRecord в Computer
    /// </summary>
    /// <param name="computerRecord"></param>
    /// <param name="iPAddress"></param>
    /// <returns>Computer</returns>
    /// <exception cref="InvalidCastException">Пустое поле Name</exception>
    /// <exception cref="ArgumentException">Не валидный MAC или IP</exception>
    public static Computer ToComputer(this ComputerRecord computerRecord, IPAddress iPAddress)
    {
        if (!PhysicalAddress.TryParse(computerRecord.MacAddress, out var macAddress))
            throw new InvalidCastException("MAC адресс не валидный");

        return new(computerRecord.Name, macAddress, iPAddress);
    }

    /// <summary>
    /// Преобразование ComputerRecord в Computer
    /// </summary>
    /// <param name="computerRecord"></param>
    /// <param name="iPAddress"></param>
    /// <returns>Computer</returns>
    /// <exception cref="InvalidCastException">Пустое поле Name</exception>
    /// <exception cref="ArgumentException">Не валидный MAC или IP</exception>
    public static Computer ToComputer(this ComputerRecord computerRecord, string iPAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(computerRecord.Name);

        if (!PhysicalAddress.TryParse(computerRecord.MacAddress, out var macAddress))
            throw new InvalidCastException("MAC адресс не валидный");

        if (!IPAddress.TryParse(iPAddress, out var ipAddress))
            throw new InvalidCastException("IP адрес не валидный");

        return new(computerRecord.Name, macAddress, ipAddress);
    }
}
