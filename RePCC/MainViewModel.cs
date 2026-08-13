using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using RePCC.Requests;

namespace RePCC;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    public MainViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }
    public ObservableCollection<Computer> Computers { get; private set; } = [];

    [RelayCommand]
    private async Task WakeUpAsync(Computer? computer)
    {
        if (computer != null)
            await _mediator.Send(new TurnOnRequest(computer));
    }

    [RelayCommand]
    private async Task ScanNetwork()
    {
        var computers = await _mediator.Send(new ScanNetworkRequest());
        Computers.Clear();
        foreach (var computer in computers)
            Computers.Add(computer);
    }

    [RelayCommand]
    private async Task ShutdownComputerAsync(Computer? computer)
    {
        if (computer is null)
            return;

        try
        {
            await _mediator.Send(new TurnOffRequest(computer));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Не удалось выключить ПК", ex.Message, "OK");
        }
    }
}
