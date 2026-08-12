using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using RePCC.Requests;

namespace RePCC;

public partial class MainViewModel(IMediator mediator) : ObservableObject
{
    [RelayCommand]
    private async Task WakeUpAsync()
    {
        var request = new TurnOnRequest();
        await mediator.Send(request);
    }
}
