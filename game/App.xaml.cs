using System;
using System.Windows;

namespace Gamebook
{
   public partial class App: Application
   {
      private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs @event)
      {
         MessageBox.Show(String.Format("{0}", @event.Exception.InnerException.Message), "Exception");
         Environment.Exit(1);
      }
   }
}
