using Data.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

namespace RepackagingMoblie
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public static List<DocLine> docLines = new List<DocLine>();
        private string auth = "DK198110007|5635796|C:/FDBManifoldData/FDB2020";
        public MainPage()
        {
            InitializeComponent();
            
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
                await DisplayAlert("Error!", "Please enter a BOM barcode", "Okay");
            }

        }

        private async void TxfPackbarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfPackbarcode.Text);
                lblDesc.Text = bi.ItemDesc;
                lblQTY.Text = "There are "+bi.Qty+" items on this BOM";
                docLines.Add(new DocLine {ItemBarcode=bi.PackBarcode,ItemDesc="#1ItemFromMain",ItemQty=bi.Qty});
            }
            catch
            {
                await DisplayAlert("Error!","No BOM with this code was found","Okay");
            }
        }
    }
}
