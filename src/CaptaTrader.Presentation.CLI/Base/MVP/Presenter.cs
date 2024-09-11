using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP;

internal abstract class Presenter
{
    internal Navigator Navigator { get; set; }// = null!; // Set on first navigation and reference is passed to next presenter on each subsequent navigation.

    internal Presenter(Navigator navigator)
    {
        Navigator = navigator;
    }

    internal virtual void Display(View view) { throw new NotImplementedException(); }

    internal virtual void Dismiss() { }
}
