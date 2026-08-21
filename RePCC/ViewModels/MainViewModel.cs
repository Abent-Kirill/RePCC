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

            // Гарантируем, что изменение коллекции происходит в UI-потоке
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Computers.Clear();
                foreach (var computer in computers)
                {
                    Computers.Add(new ComputerItemViewModel(computer, mediator));
                }
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }
}
