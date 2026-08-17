using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using RePCC.Requests;

namespace RePCC.ViewModels;

public sealed partial class MainViewModel(IMediator mediator) : ObservableObject
{
    public ObservableCollection<ComputerItemViewModel> Computers { get; } = [];

    [RelayCommand]
    private async Task GetComputersAsync()
    {
        try
        {
            var computers = await mediator.Send(new GetComputersRequest());
            Computers.Clear();

            foreach (var computer in computers)
            {
                // Оборачиваем доменную модель в UI-обертку
                var itemViewModel = new ComputerItemViewModel(computer, mediator);
                Computers.Add(itemViewModel);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Не удалось получить данные о компьютерах", ex.Message, "OK");
        }
    }
}
