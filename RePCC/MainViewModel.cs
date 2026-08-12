using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using RePCC.Requests;

namespace RePCC;

public sealed partial class MainViewModel(IMediator mediator) : ObservableObject
{
    public ObservableCollection<Computer> Computers { get; private set; } = [];

    [RelayCommand]
    private async Task WakeUpAsync(Computer? computer)
    {
        if (computer != null)
            await mediator.Send(new TurnOnRequest(computer));
    }

    [RelayCommand]
    private async Task ScanNetwork()
    {
        var computers = await mediator.Send(new ScanNetworkRequest());
        Computers = new ObservableCollection<Computer>(computers); //TODO испавить на добавление
    }

    [RelayCommand]
    private async Task ShutdownComputerAsync(Computer? computer)
    {
        if (computer != null)
            await mediator.Send(new TurnOffRequest(computer));
    }
}
