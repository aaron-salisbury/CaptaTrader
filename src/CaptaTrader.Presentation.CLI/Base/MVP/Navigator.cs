using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP;

// Inspired by https://stackoverflow.com/a/13030590
public class Navigator
{
    private readonly IServiceProvider _serviceProvider;
    private Presenter? _current;

    public Navigator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    internal void NavigateTo(Type nextPresenterType)
    {
        if (typeof(Presenter).IsAssignableFrom(nextPresenterType) &&
            _serviceProvider.GetService(nextPresenterType) is Presenter nextPresenter)
        {
            NavigateTo(nextPresenter);
        }
    }

    private void NavigateTo(Presenter nextPresenter)
    {
        _current?.Dismiss();

        _current = nextPresenter;
        _current.Display(FindView(nextPresenter));
    }

    private static View FindView(Presenter presenter)
    {
        string? name = presenter.GetType().FullName!.Replace("Presenter", "View");
        Type? type = Type.GetType(name);

        if (type != null && Activator.CreateInstance(type) is View view)
        {
            return view;
        }

        throw new Exception("View not found for presenter.");
    }
}
