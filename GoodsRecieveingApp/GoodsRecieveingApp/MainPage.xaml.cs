using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Data.Model;
using Xamarin.Essentials;
using System.Data;
using ZXing.Net.Mobile.Forms;

namespace GoodsRecieveingApp
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private string auth = "DK198110007|5635796|C:/FDBManifoldData/FDB2020";
        private string CurrentUser="";
        public MainPage()
        {
            InitializeComponent();
        }      
        private void TxfUserCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            Thread.Sleep(100);
            CurrentUser = txfUserCode.Text;
            txfPOCode.Focus();
        }
        private async void TxfPOCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            Thread.Sleep(100);            
            if (txfPOCode.Text.Length == 8)
            {            
                    LodingIndiactor.IsVisible = true;
                    if (await GetItems(txfPOCode.Text.ToUpper()))
                    {
                        DocLine d = await App.Database.GetOneSpecificDocAsync(txfPOCode.Text.ToUpper());
                        lblCompany.Text = d.SupplierName + " - " + d.SupplierCode;
                        lblCompany.IsVisible = true;
                        lblCompanyPre.IsVisible = true;
                    try
                    {
                        await App.Database.Delete(await App.Database.GetHeader(d.DocNum));
                    }
                    catch
                    {

                    }                      
                        await App.Database.Insert(new DocHeader {DocNum= txfPOCode.Text,User=txfUserCode.Text,AccName=d.SupplierName,AcctCode=d.SupplierCode});
                        LodingIndiactor.IsVisible = false;
                        await DisplayAlert("Done", "All the data has been loaded for this order", "Okay");                       
                    }
                            
            }
        }     
        private async void ButtonAccepted_Clicked(object sender, EventArgs e)
        {

            try
            {
                DocLine dl = await App.Database.GetOneSpecificDocAsync(txfPOCode.Text.ToUpper());
                await Navigation.PushAsync(new ScanAcc(dl));
            }
            catch
            {
                await DisplayAlert("Error", "Could not load info of the entered PO", "Try Again");
            }
        }
        private async void ButtonRejected_Clicked(object sender, EventArgs e)
        {
            try
            {
                DocLine dl= await App.Database.GetOneSpecificDocAsync(txfPOCode.Text.ToUpper());
                await Navigation.PushAsync(new ScanRej(dl));
            }
            catch
            {
                await DisplayAlert("Error", "Could not load info of the entered PO", "Try Again");
            }
        }
        private async Task<bool> GetItems(string code)
        {
            if (await RemoveAllOld(code))
            {
                var current = Connectivity.NetworkAccess;
                var profiles = Connectivity.ConnectionProfiles;
                if (current == NetworkAccess.Internet)
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "GetDocument";
                    client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                    {
                        string str = $"GET?authDetails={auth}&qrystr=ACCHISTL|6|{code}";
                        var Request = new RestSharp.RestRequest();
                        Request.Resource = str;
                        Request.Method = RestSharp.Method.GET;
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
                        if (res.Content.ToString().Contains("OrderNumber"))
                        {
                            DataSet myds = new DataSet();
                            myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                            foreach (DataRow row in myds.Tables[0].Rows)
                            {
                                try
                                {
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
                                    await App.Database.Insert(Doc);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }
                            }
                            return true;
                        }
                        else
                        {
                            LodingIndiactor.IsVisible = false;
                            await DisplayAlert("Error", "There is no data in this PO", "Okay");
                        }
                    }
                }
                else
                {
                    LodingIndiactor.IsVisible = false;
                    await DisplayAlert("Error!!", "Please Reconnect to the internet", "Okay");
                }
            }
            return false;
        }
 
        private async void ButtonViewS_Clicked(object sender, EventArgs e)
        {
            try
            {
                await App.Database.GetOneSpecificDocAsync(txfPOCode.Text.ToUpper());
                await Navigation.PushAsync(new ViewStock(txfPOCode.Text.ToUpper()));
            }
            catch
            {
                await DisplayAlert("Error","Could not load info of the entered PO","Try Again");
            }           
        }
        private async void TxfPOCode_Focused(object sender, FocusEventArgs e)
        {
            if (txfUserCode.Text==""|| txfUserCode.Text == null)
            {
                await DisplayAlert("Error!","Please Scan a user barcode","Okay");
                txfUserCode.Focus();
            }                   
        }       
        private async Task<bool> RemoveAllOld(string docNum)
        {
            try
            {
                foreach (DocLine dl in (await App.Database.GetSpecificDocsAsync(docNum)).Where(x => x.ItemQty != 0))
                {
                    await App.Database.Delete(dl);
                }
            }
            catch
            {

            }
            return true;
        }
    }
}
