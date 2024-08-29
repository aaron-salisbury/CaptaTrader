using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP
{
    // https://stackoverflow.com/a/13030590

    internal class Navigator
    {
        private Presenter? _current;

        internal void NavigateTo(Type nextPresenterType)
        {
            bool isPresenter = typeof(Presenter).IsAssignableFrom(nextPresenterType);

            if (isPresenter && Activator.CreateInstance(nextPresenterType) is Presenter nextPresenter)
            {
                NavigateTo(nextPresenter);
            }
        }

        internal void NavigateTo(Presenter nextPresenter)
        {
            _current?.Dismiss();

            _current = nextPresenter;
            _current.Navigator = this;
            _current.Display();
        }
    }
}
