using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP;

internal abstract class Presenter
{
    internal Navigator Navigator { get; set; }

    internal Presenter(Navigator navigator)
    {
        Navigator = navigator;
    }

    internal virtual void Display(View view) { throw new NotImplementedException(); }

    internal virtual void Dismiss() { }
}
