using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using RePCC.Models;
using RePCC.Requests;

namespace RePCC.ViewModels;

public sealed partial class ComputerItemViewModel(Computer Model, IMediator mediator, bool isSavedInDb = false) : ObservableObject
{
    public string Name => Model.Name;
    public string MacAddress => Model.MACAddress.ToString();
    public string? IpAddress => Model.IPAddress?.ToString();

    [ObservableProperty]
    public partial bool IsOnline { get; private set; } = Model.IsOnline;

    [ObservableProperty]
    public partial bool IsSavedInDb { get; set; } = isSavedInDb;

    [RelayCommand]
    private async Task TogglePowerAsync()
    {
        try
        {
            if (IsOnline)
            {
                await mediator.Send(new TurnOffRequest(Model));
                IsOnline = false;
            }
            else
            {
                await mediator.Send(new TurnOnRequest(Model));
                IsOnline = true;
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
            await mediator.Send(new SaveToDatabaseRequest(Model.ToRecord()));
            IsSavedInDb = true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Не удалось сохранить данные", ex.Message, "OK");
        }
    }
}
