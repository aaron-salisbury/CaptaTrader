using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP;

// Inspired by https://stackoverflow.com/a/13030590

internal class Navigator
{
    private Presenter? _current;

    internal void NavigateTo(Type nextPresenterType)
    {
        bool isPresenter = typeof(Presenter).IsAssignableFrom(nextPresenterType);

        if (isPresenter && Application.ServiceProvider!.GetService(nextPresenterType) is Presenter nextPresenter)
        {
            NavigateTo(nextPresenter);
        }
    }

    private void NavigateTo(Presenter nextPresenter)
    {
        _current?.Dismiss();

        _current = nextPresenter;
        _current.Navigator = this;
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
