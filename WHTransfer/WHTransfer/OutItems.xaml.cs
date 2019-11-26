using Data.KeyboardContol;
using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WHTransfer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OutItems : ContentPage
    {
        private ExtendedEntry _currententry;
        private readonly string auth = "DK198110007|5635796|C:/FDBManifoldData/FDB2020";
        public static List<IBTItem> items=new List<IBTItem>();
        public OutItems()
        {
            InitializeComponent();
            txfScannedItem.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfScannedItem.Focus();
        }
        private async void TxfScannedItem_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(100);
            if (txfScannedItem.Text.Length>1&&await CheckItem())
            {
                ListViewItems.ItemsSource = null;
                await Task.Delay(100);
                lblLastItem.Text ="Previous Barcode: "+ txfScannedItem.Text;
                txfScannedItem.Text = "";
                txfScannedItem.Focus();
                ListViewItems.ItemsSource = items;
                Loading.IsVisible = false;
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
        private async Task<bool> CheckItem()
        {
            Loading.IsVisible = true;
            try
            {
                BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfScannedItem.Text);
                items.Add(new IBTItem {ScanBarcode=txfScannedItem.Text,ItemBarcode=bi.PackBarcode, ItemDesc=bi.ItemDesc,ItemCode=bi.ItemCode,ItemQtyOut=bi.Qty,ItemQtyIn=0,PickDateTime=DateTime.Now});
                return true;
            }
            catch
            {
                try
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "FindDescAndCode";
                    client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                    {
                        string qry = $"ACCPRD|4|{txfScannedItem.Text}";
                        string str = $"GET?authDetails={auth}&qrystr={qry}";
                        var Request = new RestSharp.RestRequest(str,RestSharp.Method.GET);
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
                        cancellationTokenSource.Dispose();
                        if (res.IsSuccessful && res.Content != null)
                        {
                            //"0|002|DOMEDPICKET|Domed Picket Fence Set of 3|6006146009796|3||\u0001|\u0001|\u0001|PCE|15|15||\u0001||\u0001||||||0|0|0||0|17/09/2019 16:36:13|||1BF668944E4C16AF4C5FBD802A04FDB8|"
                            items.Add(new IBTItem { ScanBarcode = txfScannedItem.Text, ItemBarcode = res.Content.Split('|')[4], ItemCode = res.Content.Split('|')[2], ItemDesc= res.Content.Split('|')[3], ItemQtyOut = 1, ItemQtyIn = 0,PickDateTime = DateTime.Now });
                            return true;
                        }
                    }
                }
                catch
                {

                }
            }
            return false;
        }
        private void BtnComplete_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new AuthOut());
        }
    }
}