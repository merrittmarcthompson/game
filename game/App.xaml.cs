#nullable enable
using System;
using System.Windows;

namespace Gamebook
{
   public partial class App: Application
   {
      private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs @event)
      {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
         MessageBox.Show(String.Format("{0}", @event.Exception.InnerException.Message), "Exception");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
         Environment.Exit(1);
      }
   }
}
