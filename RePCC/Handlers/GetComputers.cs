using System.Collections.Immutable;
using System.Net.NetworkInformation;
using MediatR;
using RePCC.Models;
using RePCC.Requests;

namespace RePCC.Handlers;

internal sealed record GetComputersHandler(ComputersRepository ComputersRepository) : IRequestHandler<GetComputersRequest, IReadOnlyCollection<Computer>>
{
    public async Task<IReadOnlyCollection<Computer>> Handle(GetComputersRequest request, CancellationToken cancellationToken)
    {
        var computersInLocalNetwork = await NetworkService.ScanLocalNetwork();
        var computersFromDatabase = await ComputersRepository.GetComputerRecordsAsync();

        var dbLookup = computersFromDatabase
            .ToLookup(db => db.MacAddress, StringComparer.OrdinalIgnoreCase);
        var netLookup = computersInLocalNetwork
            .ToLookup(net => net.MACAddress.ToString(), StringComparer.OrdinalIgnoreCase);

        var allMacs = computersFromDatabase.Select(db => db.MacAddress)
            .Union(computersInLocalNetwork.Select(net => net.MACAddress.ToString()))
            .Distinct();

        var allComputers = allMacs.Select(mac =>
        {
            var dbRecord = dbLookup[mac].SingleOrDefault();
            var netDevice = netLookup[mac].SingleOrDefault();

            if (netDevice is null && dbRecord is null)
            {
                return null;
            }
            if (dbRecord is null)
            {
                return new Computer(netDevice.Name, netDevice.MACAddress, netDevice.IPAddress);
            }
            if (netDevice is null)
            {
                if (PhysicalAddress.TryParse(dbRecord.MacAddress, out var macAdr))
                {
                    return new Computer(dbRecord.Name, macAdr, null);
                }
                throw new FormatException("Не смог распарсить MAC у dbRecord");
            }
            else
            {
                return new Computer(dbRecord.Name, netDevice.MACAddress, netDevice.IPAddress);
            }
        }).ToImmutableList() ?? throw new Exception("Ни один из компьютеров не был найден");
        return allComputers;
    }
}
