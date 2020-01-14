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
    public partial class Custom : ContentPage
    {
        private ExtendedEntry _currententry;
        private string ItemBarcode;
        public Custom()
        {
            InitializeComponent();
            txfBarcode.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfBarcode.Focus();
        }
        private async void TxfBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txfBarcode.Text != "")
            {
                Loader.IsVisible = true;
                try
                {
                    BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfBarcode.Text);
                    Loader.IsVisible = false;
                    await DisplayAlert("Error!", "You cannot add a BOM as a single item", "OK");
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
                                if (res.Content.Split('|')[2] == MainPage.docLines.Find(x => x.ItemDesc == "1ItemFromMain").ItemCode)
                                {
                                    lblItemDesc.Text = res.Content.Split('|')[3];
                                    ItemBarcode = res.Content.Split('|')[4];
                                    Loader.IsVisible = false;
                                }
                                else
                                {
                                    Loader.IsVisible = false;
                                    await DisplayAlert("Error!", "This is not the same product type from this BOM", "OK");
                                    txfBarcode.Text = "";
                                    txfBarcode.Focus();
                                    return;
                                }
                            }
                        }
                    }
                    catch
                    {
                        lblItemDesc.Text = "No Item With This Code";
                        Loader.IsVisible = false;
                        await DisplayAlert("Error!", "There was no item found with this code", "OK");
                        txfBarcode.Text = "";
                        txfBarcode.Focus();
                        return;
                    }
                }
                txfQTY.Focus();
            }           
        }
        private void BtnAdd_Clicked(object sender, EventArgs e)
        {
            if(lblItemDesc.Text!=null&&lblItemDesc.Text!=""&&lblItemDesc.Text!="No Item With This Code" && txfQTY.Text != null && txfQTY.Text != ""&&Convert.ToInt32(txfQTY.Text)>1 && Convert.ToInt32(txfQTY.Text) < 100)
            {
                MainPage.docLines.Add(new DocLine { ItemBarcode = txfBarcode.Text, ItemDesc = lblItemDesc.Text, isRejected = false, ItemQty = Convert.ToInt32(txfQTY.Text) });
                if (Convert.ToInt32(txfQTY.Text)>9)
                {
                    MainPage.PackCodes.Add("F" + txfQTY.Text + ItemBarcode.Substring(ItemBarcode.Length - 6, 5));
                }
                else
                {
                    MainPage.PackCodes.Add("F0" + txfQTY.Text + ItemBarcode.Substring(ItemBarcode.Length - 6, 5));
                }
                
                DisplayAlert("Done!", "Items have been saved", "OK");
                Navigation.PopAsync();
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
    }
}