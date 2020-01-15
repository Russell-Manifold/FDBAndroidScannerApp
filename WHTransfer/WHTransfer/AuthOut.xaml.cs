using Data.KeyboardContol;
using Data.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WHTransfer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AuthOut : ContentPage
    {
        private ExtendedEntry _currententry;
        private string AdminName, WH;
        private int AdminCode,ColID;
        public AuthOut()
        {
            InitializeComponent();
            txfUserCode.Focused +=Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfUserCode.Focus();
        }
        private async void BtnDone_Clicked(object sender, EventArgs e)
        {
            //Complete order
            string Useroutput =await DisplayActionSheet("Are you sure you would like to complete the order","Yes","No");
            switch(Useroutput){
                case"Yes":
                    if (await completeOrder())
                    {
                        await GoodsRecieveingApp.App.Database.DeleteAllHeaders();
                        await DisplayAlert("Complete!","All the data has been saved","OK");
                        Navigation.RemovePage(Navigation.NavigationStack[2]);
                        Navigation.RemovePage(Navigation.NavigationStack[3]);
                        Navigation.RemovePage(Navigation.NavigationStack[4]);
                        await Navigation.PopAsync();
                        return;
                    }
                    await DisplayAlert("Error!", "There was a problem with sending the IBT", "OK");
                    break;
                case "No":
                    break;
                default:
                    break;
            }
        }
        private async Task<bool> completeOrder()
        {
            if(await InsertHeader())
            {
                UpdateRecords();
                if (await InsertLines())
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<bool> InsertHeader()
        {
            try
            {
                IBTHeader header = (await GoodsRecieveingApp.App.Database.GetIBTHeaders()).First();
                WH = header.FromWH;
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "IBTHeader";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"POST?TrfDate={DateTime.Now.ToString("dd MMM yyyy")}&FromWH={header.FromWH}&ToWH={header.ToWH}&FromDate={header.FromDate}&RecDate={header.RecDate}&PickerUser={GoodsRecieveingApp.MainPage.UserCode}&AuthUser={AdminCode}&Active=true";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.POST;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content != null)
                    {
                        ColID = Convert.ToInt32(res.Content.Replace('\\', ' ').Replace('"', ' '));
                        await DisplayAlert("Header Added!", "Your Header has been added your TRF code is " + ColID, "Continue");
                        return true;
                    }
                }
            }
            catch
            {

            }           
            return false;
        }
        private void UpdateRecords()
        {
            OutItems.items.Select(x=> { x.PickerUser = GoodsRecieveingApp.MainPage.UserCode;x.AuthUser = AdminCode; x.iTrfID=ColID; x.WH=WH; return x;}).ToList();
        }
        private async Task<bool> InsertLines()
        {
            foreach (IBTItem i in OutItems.items)
            {
                try
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "IBTLines";
                    client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                    {
                        string str = $"POST?ScanBarcode={i.ScanBarcode}&ItemBarcode={i.ItemBarcode}&ItemCode={i.ItemCode}&ItemDesc={i.ItemDesc}&ItemQtyOut={i.ItemQtyOut}&ItemQtyIn={i.ItemQtyIn}&PickerUser={i.PickerUser}&AuthUser={i.AuthUser}&PickDateTime={i.PickDateTime.ToString("dd MMM yyyy")}&WH={i.WH}&iTrfId={i.iTrfID}";
                        var Request = new RestRequest(str, Method.POST);
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                        cancellationTokenSource.Dispose();
                        if (!(res.IsSuccessful && res.Content.Contains("Complete")))
                        {
                            return false;
                        }
                    }
                }                
                catch
                {
                    await DisplayAlert("Error!","There was an error in inserting the lines","OK");
                    return false;
                }
            }
            return true;
        }
        private async void TxfUserCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(100);
            if (txfUserCode.Text.Length>1)
            {
                Loading.IsVisible = true;
                if (txfUserCode.Text!=GoodsRecieveingApp.MainPage.UserName&&await CheckUser(txfUserCode.Text) !=GoodsRecieveingApp.MainPage.UserCode) 
                { 
                     AdminName = txfUserCode.Text;
                     btnDone.IsEnabled = true;
                     Loading.IsVisible = false;
                     return;
                }
                    Loading.IsVisible = false;
                    await DisplayAlert("Error!", "Invalid access or user code", "OK");
                    txfUserCode.Text = "";
                    txfUserCode.Focus();               
            }
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
        private async Task<int> CheckUser(string usercode)
        {
            try
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "GetUser";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"GET?UserName={usercode}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    cancellationTokenSource.Dispose();
                    if (res.IsSuccessful && res.Content != null)
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            try
                            {
                                if (Convert.ToInt32(row["AccessLevel"]) > 3)
                                {
                                    AdminCode =Convert.ToInt32(row["Id"].ToString());
                                    myds.Dispose();
                                    return AdminCode;
                                }
                                else
                                {
                                    myds.Dispose();
                                    return GoodsRecieveingApp.MainPage.UserCode;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                        myds.Dispose();
                    }
                }
            }
            catch
            {
            }
            return GoodsRecieveingApp.MainPage.UserCode;
        }
    }
}