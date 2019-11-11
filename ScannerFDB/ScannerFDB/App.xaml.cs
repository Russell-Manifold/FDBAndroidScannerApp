using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Data.DataAccess;
using Data.Model;
using System.IO;
using System.Threading;

namespace ScannerFDB
{
    public partial class App : Application
    {
        static AccessLayer database;
        public App()
        {
            InitializeComponent();
            MainPage mp = new MainPage();
            var nav = new NavigationPage(mp);
            nav.BarBackgroundColor = Color.Blue;
            nav.BarTextColor = Color.White;
            MainPage = nav;
        }
        public static AccessLayer Database
        {
            get
            {
                if (database == null)
                {
                    database = new AccessLayer(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SqliteDB.db3"));
                }
                return database;
            }
        }
        protected async override void OnStart()
        {
            //try
            //{
            //    RestSharp.RestClient client = new RestSharp.RestClient();
            //    string path = "GetSDKAuth";
            //    string API = "http://localhost:7721//api/";
            //    client.BaseUrl = new Uri(API + path);
            //    {
            //        var Request = new RestSharp.RestRequest();
            //        string str = "GET?auth=1154";
            //        Request.Method = RestSharp.Method.GET;
            //        Request.Resource = str;
            //        var cancellationTokenSource = new CancellationTokenSource();                   
            //        var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
            //        if (res.IsSuccessful)
            //        {
            //            await database.DeleteOldInfoAsync();
            //            await database.SaveAuthAsync(new DBInfo {DBPath="",APIPath="",SerNum="",Auth=""});
            //        }
            //    }
            //}
            //catch
            //{

            //}
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
