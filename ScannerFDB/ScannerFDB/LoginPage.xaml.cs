using Data.KeyboardContol;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoodsRecieveingApp;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ScannerFDB
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        private ExtendedEntry _currententry;
        public LoginPage()
        {
            InitializeComponent();
            txfUserBarcode.Focused += Entry_Focused;
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            txfUserBarcode.Text = "";
            await Task.Delay(200);
            txfUserBarcode.Focus();
        }
        private async void Entry_Focused(object sender, FocusEventArgs e)
        {
            await Task.Delay(110);
            _currententry = sender as ExtendedEntry;
            if (_currententry != null)
            {
                try
                {
                    _currententry.HideKeyboard();
                }
                catch
                {

                }

            }
        }
        private async void TxfUserBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(200);
            if (!(txfUserBarcode.Text.Length<2))
            {
                if (!await CheckUser())
                {
                    txfUserBarcode.Text = "";
                    txfUserBarcode.Focus();
                }
            }
            
        }
        private async Task<bool> CheckUser()
        {
            AccessLoading.IsVisible = true;
            try
            {
                RestClient client = new RestClient();
                string path = "GetUser";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"GET?UserName={txfUserBarcode.Text}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    cancellationTokenSource.Dispose();
                    if (res.IsSuccessful && res.Content.Contains("UserName"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            GoodsRecieveingApp.MainPage.UserName = row["UserName"].ToString();
                            GoodsRecieveingApp.MainPage.UserCode = Convert.ToInt32(row["Id"].ToString());

                            MainPage.UserName = row["UserName"].ToString();
                            MainPage.UserCode = Convert.ToInt32(row["Id"].ToString());

                            MainPage.fReceive = Convert.ToBoolean(row["fReceive"]);
                            MainPage.fRepack = Convert.ToBoolean(row["fRepack"]);
                            MainPage.fInvCount = Convert.ToBoolean(row["fInvCount"]);
                            MainPage.fWhTrf = Convert.ToBoolean(row["fWhTrf"]);
                            MainPage.fPickPack = Convert.ToBoolean(row["fPickPack"]);

                            MainPage.AuthWHTrf = Convert.ToBoolean(row["AuthWHTrf"]);
                            MainPage.AuthReceive = Convert.ToBoolean(row["AuthReceive"]);
                            MainPage.AuthDispatch = Convert.ToBoolean(row["AuthDispatch"]);
                            MainPage.PickChecker = Convert.ToBoolean(row["PickChecker"]);
                            MainPage.SystAdmin = Convert.ToBoolean(row["SystAdmin"]);
                            MainPage.CreateInvCount = Convert.ToBoolean(row["CreateInvCount"]);
                            MainPage.CloseInvCount = Convert.ToBoolean(row["CloseInvCount"]);
                            MainPage.PSCollect = Convert.ToBoolean(row["PSCollect"]);
                        }
                            await Navigation.PushAsync(new MainPage());
                            AccessLoading.IsVisible = false;
                            return true;
                        
                    }
                    else
                    {
                        AccessLoading.IsVisible = false;
                        await DisplayAlert("Error!", "Invalid User Access", "OK");
                    }
                }
            }
            catch
            {
                AccessLoading.IsVisible = false;
                await DisplayAlert("Error!", "Invalid User Access", "OK");
            }
            AccessLoading.IsVisible = false;
            await DisplayAlert("Error!", "Invalid User Access", "OK");
            return false;
        }
    }
}