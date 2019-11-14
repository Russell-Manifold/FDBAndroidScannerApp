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
        private string auth = "DK198110007|5635796|C:/FDBManifoldData/FDB2020";
        public Custom()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfBarcode.Focus();
        }
        private async void TxfBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfBarcode.Text);
                lblItemDesc.Text =bi.ItemDesc;
                MainPage.docLines.Add(new DocLine { ItemBarcode = txfBarcode.Text, ItemDesc = lblItemDesc.Text, isRejected = true, ItemQty = bi.Qty });
            }
            catch
            {
                try
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "FindDescAndCode";
                    client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                    {
                        string str = $"GET?authDetails={auth}&qrystr=ACCPRD|4|" + txfBarcode.Text;
                        var Request = new RestSharp.RestRequest();
                        Request.Resource = str;
                        Request.Method = RestSharp.Method.GET;
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
                        if (res.IsSuccessful && res.Content.Split('|')[0].Contains("0"))
                        {                         
                            lblItemDesc.Text = res.Content.Split('|')[3];
                        }
                    }
                }
                catch
                {
                    lblItemDesc.Text = "No Item With This Code";
                    await DisplayAlert("Error!","There was no item or pack found with this code","Okay");
                    txfBarcode.Text = "";
                    txfBarcode.Focus();
                    return;
                }
            }
            txfQTY.Focus();
        }
        private void BtnAdd_Clicked(object sender, EventArgs e)
        {
            if(lblItemDesc.Text!=null&&lblItemDesc.Text!=""&&lblItemDesc.Text!="No Item With This Code"&&txfQTY.Text!=null&&txfQTY.Text!="")
            MainPage.docLines.Add(new DocLine { ItemBarcode = txfBarcode.Text,ItemDesc=lblItemDesc.Text, isRejected = false, ItemQty = Convert.ToInt32(txfQTY.Text)});

             DisplayAlert("Done!","Items have been saved","Okay");
            Navigation.PopAsync();
        }
    }
}