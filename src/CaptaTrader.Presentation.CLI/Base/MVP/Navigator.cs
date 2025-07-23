using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP;

// Inspired by https://stackoverflow.com/a/13030590
public class Navigator(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private Presenter? _current;

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
        string? name = presenter.GetType().FullName!.Replace(nameof(Presenter), nameof(View));
        Type? type = Type.GetType(name);

        if (type != null && Activator.CreateInstance(type) is View view)
        {
            return view;
        }

        throw new Exception("View not found for presenter. Presenters and Views must be named identically, except for their respective \"Presenter\" and \"View\" suffixes.");
    }
}
