using Drishya.Helpers;
using System.Windows;

namespace Drishya
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            FontRegistration.RegisterFonts();
        }
    }

}
