using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using GoodsRecieveingApp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace PickAndPack
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        public MainPage()
        {
            InitializeComponent();
            txfSOCode.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfSOCode.Focus();
        }
        //private async void TxfUserCode_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    await Task.Delay(200);
        //    CurrentUser = txfUserCode.Text;
        //    if (CurrentUser.Length > 1)
        //    {
        //        LodingIndiactor.IsVisible = true;
        //        if (await userCheck())
        //        {
        //            LodingIndiactor.IsVisible = false;
        //            txfUserCode.IsVisible = false;
        //            lblUserCode.IsVisible = false;
        //            txfSOCode.IsVisible = true;
        //            lblSOCode.IsVisible = true;
        //            txfSOCode.Text = "";
        //            await Task.Delay(100);
        //            txfSOCode.Focus();
        //        }
        //        else
        //        {
        //            LodingIndiactor.IsVisible = false;
        //            txfUserCode.Text = "";
        //            await DisplayAlert("Error!", "Invalid User", "OK");
        //            txfUserCode.Focus();
        //        }
        //    }
        //}
        //private async Task<bool> userCheck()
        //{
        //    try
        //    {
        //        RestSharp.RestClient client = new RestSharp.RestClient();
        //        string path = "GetUser";
        //        client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
        //        {
        //            string str = $"GET?UserName={CurrentUser}";
        //            var Request = new RestSharp.RestRequest();
        //            Request.Resource = str;
        //            Request.Method = RestSharp.Method.GET;
        //            var cancellationTokenSource = new CancellationTokenSource();
        //            var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
        //            if (res.IsSuccessful && res.Content != null)
        //            {
        //                DataSet myds = new DataSet();
        //                myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
        //                foreach (DataRow row in myds.Tables[0].Rows)
        //                {
        //                    try
        //                    {
        //                        if (Convert.ToInt32(row["AccessLevel"]) > 0)
        //                        {
        //                            return true;
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Console.WriteLine(ex);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch
        //    {
        //    }
        //    return false;
        //}
        private async void TxfSOCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txfSOCode.Text.Length == 8)
            {
                LodingIndiactor.IsVisible = true;
                if (await GetItems(txfSOCode.Text.ToUpper()))
                {
                    DocLine d = await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(txfSOCode.Text.ToUpper());                   
                    txfSOCode.IsVisible = false;
                    lblSOCode.IsVisible = false;
                    try
                    {
                        await GoodsRecieveingApp.App.Database.Delete(await GoodsRecieveingApp.App.Database.GetHeader(d.DocNum));
                    }
                    catch
                    {

                    }
                    await GoodsRecieveingApp.App.Database.Insert(new DocHeader { DocNum = txfSOCode.Text, User = GoodsRecieveingApp.MainPage.UserName, AccName = d.SupplierName, AcctCode = d.SupplierCode });
                    LodingIndiactor.IsVisible = false;
                    lblCusName.IsVisible = true;
                    lblCode.IsVisible = true;
                    //await DisplayAlert("Done", "All the data has been loaded for this order", "OK");                       
                }
                else
                {
                    txfSOCode.Text = "";
                    txfSOCode.Focus();
                }
            }
        }
        private async void btnScanItems_Clicked(object sender, EventArgs e)
        {

            try
            {
                DocLine dl = await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(txfSOCode.Text.ToUpper());
                await Navigation.PushAsync(new ScannItems(dl));
            }
            catch
            {
                await DisplayAlert("Error", "Could not load info of the entered SO", "Try Again");
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
                                    lblCode.Text = Doc.DocNum;
                                    lblCusName.Text = Doc.SupplierName;
                                    await GoodsRecieveingApp.App.Database.Insert(Doc);
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
                            await DisplayAlert("Error", "There is no data in this PO", "OK");
                        }
                    }
                }
                else
                {
                    LodingIndiactor.IsVisible = false;
                    await DisplayAlert("Error!!", "Please Reconnect to the internet", "OK");
                }
            }
            return false;
        }
        private async void ButtonViewS_Clicked(object sender, EventArgs e)
        {
            try
            {
                message.DisplayMessage("Loading...", false);
                await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(txfSOCode.Text.ToUpper());
                await Navigation.PushAsync(new ViewItems(txfSOCode.Text.ToUpper()));
            }
            catch
            {
                await DisplayAlert("Error", "Could not load info of the entered PO", "Try Again");
            }
        }      
        private async Task<bool> RemoveAllOld(string docNum)
        {
            try
            {
                foreach (DocLine dl in (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docNum)).Where(x => x.ItemQty != 0))
                {
                    await GoodsRecieveingApp.App.Database.Delete(dl);
                }
            }
            catch
            {

            }
            return true;
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
