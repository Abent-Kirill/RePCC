using System.Net.NetworkInformation;

namespace ShareLib;
public record Computer(bool Status, PhysicalAddress MACAddress);
