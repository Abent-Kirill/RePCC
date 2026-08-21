using System.Net.NetworkInformation;
using MediatR;
using RePCC.Models;
using RePCC.Requests;

namespace RePCC.Handlers;

internal sealed record GetComputersHandler(ComputersRepository ComputersRepository) : IRequestHandler<GetComputersRequest, IReadOnlyCollection<Computer>>
{
    public async Task<IReadOnlyCollection<Computer>> Handle(GetComputersRequest request, CancellationToken cancellationToken)
    {
        var computersInLocalNetworkTask = NetworkService.ScanLocalNetworkAsync(cancellationToken);
        var computersFromDatabaseTask = ComputersRepository.GetComputerRecordsAsync(cancellationToken);

        await Task.WhenAll(computersFromDatabaseTask, computersInLocalNetworkTask);

        var computersInLocalNetwork = await computersInLocalNetworkTask;
        var computersFromDatabase = await computersFromDatabaseTask;

        cancellationToken.ThrowIfCancellationRequested();

        var mergedComputers = new Dictionary<string, (ComputerRecord? Db, Computer? Net)>(
            computersFromDatabase.Count,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var db in computersFromDatabase)
        {
            if (db.MacAddress != null)
            {
                mergedComputers[db.MacAddress] = (db, null);
            }
        }

        foreach (var net in computersInLocalNetwork)
        {
            var macStr = net.MACAddress.ToString();

            if (mergedComputers.TryGetValue(macStr, out var existing))
            {
                mergedComputers[macStr] = (existing.Db, net);
                continue;
            }
            mergedComputers[macStr] = (null, net);
        }

        var allComputers = mergedComputers.Select(pair =>
        {
            var (dbRecord, netDevice) = pair.Value;

            if (dbRecord is null)
            {
                var computer = new Computer(netDevice!.Name, netDevice.MACAddress, netDevice.IPAddress)
                {
                    IsOnline = true
                };
                return computer;
            }

            if (netDevice is null)
            {
                if (PhysicalAddress.TryParse(dbRecord.MacAddress, out var macAdr))
                {
                    var computer = new Computer(dbRecord.Name, macAdr, null) { IsOnline = false };
                    return computer;
                }
                throw new FormatException($"Не смог распарсить MAC у dbRecord: {dbRecord.MacAddress}");
            }

            return new Computer(dbRecord.Name, netDevice.MACAddress, netDevice.IPAddress) { IsOnline = true };
        }).ToList();

        return allComputers;
    }
}
