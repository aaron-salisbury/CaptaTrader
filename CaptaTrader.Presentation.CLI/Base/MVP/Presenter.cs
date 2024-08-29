using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP
{
    internal class Presenter
    {
        internal Navigator? Navigator { get; set; }

        internal virtual void Display() { throw new NotImplementedException(); }

        internal virtual void Dismiss() { }
    }
}
