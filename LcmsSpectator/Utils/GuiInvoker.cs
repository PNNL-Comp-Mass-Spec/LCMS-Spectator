using System;

namespace LcmsSpectator.Utils
{
    public class GuiInvoker
    {
        /// <summary>
        /// Invoke action on GUI thread
        /// </summary>
        /// <param name="action">Action to invoke</param>
        public static void Invoke(Action action)
        {
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Invoke action asynchonously on GUI thread
        /// </summary>
        /// <param name="action">Action to invoke</param>
        public static void BeginInvoke(Action action)
        {
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Invoke Action with a paramter on GUI thread
        /// </summary>
        /// <typeparam name="T">Datatype of parameter</typeparam>
        /// <param name="action">Action to invoke</param>
        /// <param name="argument">Paramter of action</param>
        public static void Invoke<T>(Action<T> action, T argument)
        {
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(action, argument);
            }
            else
            {
                action(argument);
            }
        }
    }
}
