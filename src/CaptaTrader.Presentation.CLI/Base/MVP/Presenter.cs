using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP;

internal abstract class Presenter
{
    internal Navigator Navigator { get; set; }

    internal Presenter(Navigator navigator)
    {
        Navigator = navigator;
    }

    internal abstract void Display(View view);

    internal virtual void Dismiss() { }
}
