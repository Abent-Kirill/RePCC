using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using RePCC.Models;
using RePCC.Requests;

namespace RePCC.ViewModels;

public sealed partial class ComputerItemViewModel(Computer Model, IMediator mediator) : ObservableObject
{
    public string Name => Model.Name;
    public string MacAddress => Model.MACAddress.ToString();
    public string? IpAddress => Model.IPAddress?.ToString();

    [ObservableProperty]
    public partial bool IsOnline { get; private set; } = Model.IsOnline;

    [ObservableProperty]
    public partial bool IsSavedInDb { get; set; } = Model.IPAddress is null;

    [RelayCommand]
    private async Task TogglePowerAsync()
    {
        try
        {
            if (IsOnline)
            {
                await mediator.Send(new TurnOffRequest(Model));
                IsOnline = false;
                await mediator.Send(new SaveToDatabaseRequest(new ComputerRecord(Name, MacAddress, IsOnline)));
            }
            else
            {
                await mediator.Send(new TurnOnRequest(Model));
                IsOnline = true;
                await mediator.Send(new SaveToDatabaseRequest(new ComputerRecord(Name, MacAddress, IsOnline)));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка питания", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await mediator.Send(new SaveToDatabaseRequest(new ComputerRecord(Name, MacAddress, IsOnline)));
            IsSavedInDb = true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Не удалось сохранить данные", ex.Message, "OK");
        }
    }
}
