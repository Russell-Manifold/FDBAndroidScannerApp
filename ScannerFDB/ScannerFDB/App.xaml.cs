using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ScannerFDB
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage mp = new MainPage();
            MainPage = new NavigationPage(mp);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
