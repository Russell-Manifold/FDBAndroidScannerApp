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

namespace RepackagingMoblie
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Singles : ContentPage
    {
        private ExtendedEntry _currententry;
        public Singles()
        {
            InitializeComponent();
            txfBarcode.Focused +=Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfBarcode.Focus();
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
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); 
        }
        private async void TxfBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(200);
            if (txfBarcode.Text!="")
            {
                Loader.IsVisible = true;
                await FindItem();
                txfBarcode.Text = "";
                txfBarcode.Focus();
            }
            
        }
        void setQTY(string barcode)
        {
            int i = MainPage.docLines.FindAll(x => x.isRejected == false && x.ItemBarcode == barcode&&x.ItemQty==1).Count;
            lblItemQTY.Text = i + " items scanned as repacked";
        }
        public async Task<bool> FindItem()
        {
            lblBarcode.Text = txfBarcode.Text;
            try
            {
                BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfBarcode.Text);
                Loader.IsVisible = false;
                await DisplayAlert("Error!", "You cannot add a BOM as a single item", "OKay");
            }
            catch
            {
                try
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "FindDescAndCode";
                    client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                    {
                        string str = $"GET?qrystr=ACCPRD|4|" + txfBarcode.Text;
                        var Request = new RestSharp.RestRequest();
                        Request.Resource = str;
                        Request.Method = RestSharp.Method.GET;
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
                        if (res.IsSuccessful && res.Content.Split('|')[0].Contains("0"))
                        {
                            if(res.Content.Split('|')[2]==MainPage.docLines.Find(x=>x.ItemDesc== "1ItemFromMain").ItemCode)
                            {
                                lblItemDesc.Text = res.Content.Split('|')[3];
                                MainPage.docLines.Add(new DocLine { ItemBarcode = txfBarcode.Text, ItemDesc = lblItemDesc.Text, isRejected = false, ItemQty = 1 });
                                setQTY(txfBarcode.Text);
                                Loader.IsVisible = false;
                                return true;
                            }
                            else
                            {
                                Loader.IsVisible = false;
                                await DisplayAlert("Error!","This is not the same product type from this BOM","Okay");
                                lblItemDesc.Text = "";
                                lblItemQTY.Text = "";
                                return false;
                            }
                            
                        }
                    }
                }
                catch
                {
                    lblItemDesc.Text = "No Item With This Code";
                    Loader.IsVisible = false;
                    await DisplayAlert("Error!", "There was no item or pack found with this code", "Okay");
                }              
            }
            return false;
        }
    }
}