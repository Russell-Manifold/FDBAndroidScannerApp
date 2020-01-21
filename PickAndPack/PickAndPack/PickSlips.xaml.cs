using Data.KeyboardContol;
using Data.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PickAndPack
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PickSlips : ContentPage
    {
        private ExtendedEntry _currententry;
        public PickSlips()
        {
            InitializeComponent();
            txfSOCodes.Focused += Entry_Focused;
        }
        private void Entry_Focused(object sender, FocusEventArgs e)
        {            
            _currententry = sender as ExtendedEntry;
            if (_currententry != null)
            {
                try
                {
                    _currententry.HideKeyboard();
                }
                catch
                {}
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Task.Delay(100);
            txfSOCodes.Focus();
        }
        private async void txfSOCodes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txfSOCodes.Text.Length == 8)
            {
                LoadingIndicator.IsVisible = true;
                if (await FetchSO(txfSOCodes.Text))
                {
                    LoadingIndicator.IsRunning = false;
                    await DisplayAlert("FDB Scanner","Got It","OK");
                    lblSoName.IsVisible = true;
                }
                else
                {
                    LoadingIndicator.IsRunning = false;
                    await DisplayAlert("FDB Scanner", "Error Somethings wrong", "OK");
                    lblSoName.IsVisible = false;
                }
                LoadingIndicator.IsVisible = false;
                txfSOCodes.Text = "";
                txfSOCodes.Focus();
            }
        }
        async Task<bool> FetchSO(string code)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "GetDocument";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"GET?qrystr=ACCHISTL|6|{code}|102";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.Content.ToString().Contains("OrderNumber"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        string smtpe=myds.Tables[0].Rows[0]["OrderNumber"].ToString();
                       // if (await GoodsRecieveingApp.App.Database.GetHeader(myds.Tables[0].Rows[0]["OrderNumber"].ToString())==null){
                            foreach (DataRow row in myds.Tables[0].Rows)
                            {
                                try
                                {
                                    string s = smtpe;
                                    s = "";
                                    var Doc = new DocLine();
                                    Doc.DocNum = row["OrderNumber"].ToString();
                                    Doc.SupplierCode = row["SupplierCode"].ToString();
                                    Doc.SupplierName = row["SupplierName"].ToString();
                                    Doc.ItemBarcode = row["Barcode"].ToString();
                                    Doc.ItemCode = row["ItemCode"].ToString();
                                    Doc.ItemDesc = row["ItemDesc"].ToString();
                                    Doc.ScanAccQty = 0;
                                    Doc.ScanRejQty = 0;
                                    Doc.ItemQty = Convert.ToInt32(row["ItemQty"].ToString().Trim());
                                    await GoodsRecieveingApp.App.Database.Insert(Doc);
                                    s = Doc.SupplierCode;
                                    s += "\n" + Doc.SupplierName;
                                    lblSoName.Text = s;                                   
                                }
                                catch
                                {
                                    return false;
                                }
                            }
                            return true;
                        //}
                    }
                }
            }
            return false;
        }
    }
}