using System.Configuration;
using System.Data;
using System.Windows;

namespace TexTed
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //todo: uncomment when release
            //SplashScreen splashScreen = new SplashScreen("/Resources/texted_logo12.png");
            //splashScreen.Show(autoClose: true, topMost: true);

            if (e.Args.Length > 0)
            {
                Application.Current.Properties["FileName"] = e.Args[0];
            }

        }
   
    }

}
