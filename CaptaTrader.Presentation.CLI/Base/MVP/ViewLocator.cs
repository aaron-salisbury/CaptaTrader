using System;

namespace CaptaTrader.Presentation.CLI.Base.MVP
{
    internal static class ViewLocator
    {
        internal static T GetView<T>(Presenter presenter) where T : View
        {
            var name = presenter.GetType().FullName!.Replace("Presenter", "View");
            var type = Type.GetType(name);

            if (type != null && Activator.CreateInstance(type) is T view)
            {
                return view;
            }

            throw new Exception("View not for for presenter.");
        }
    }
}
