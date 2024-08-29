using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP
{
    internal abstract class View
    {
        internal const string PROMPT_FORMAT = "{0}: ";

        internal virtual void Show() { throw new NotImplementedException(); }
    }
}
