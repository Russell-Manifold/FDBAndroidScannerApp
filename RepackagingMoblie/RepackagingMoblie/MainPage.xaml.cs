using Data.KeyboardContol;
using Data.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RepackagingMoblie
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private ExtendedEntry _currententry;
        public static List<DocLine> docLines = new List<DocLine>();
        public static List<string> PackCodes = new List<string>();
        public MainPage()
        {
            InitializeComponent();
            txfPackbarcode.Focused += Entry_Focused;
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
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfPackbarcode.Focus();
        }
        private async void BtnGoToRepack_Clicked(object sender, EventArgs e)
        {
            if (docLines.Count>0)
            {
                await Navigation.PushAsync(new MenuPage());
            }
            else
            {
                await DisplayAlert("Error!", "Please scan a barcode of items unpacked", "OK");
            }

        }

        private async void TxfPackbarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(txfPackbarcode.Text!=""&&txfPackbarcode.Text!=null){
                try
                {
                    BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfPackbarcode.Text);
                    lblDesc.Text = "Item: " + bi.ItemDesc;
                    lblQTY.Text = "Qty: " + bi.Qty;
                    docLines.Add(new DocLine { ItemBarcode = bi.PackBarcode,ItemCode=bi.ItemCode, ItemDesc = "1ItemFromMain", ItemQty = bi.Qty });
                    btnGoToRepack.IsVisible = true;
                }
                catch
                {
                    await DisplayAlert("Error!", "No Packing Code Found", "OK");
                    txfPackbarcode.Text = "";
                    txfPackbarcode.Focus();
                    btnGoToRepack.IsVisible = false;
                }
            }
        }
    }
}
