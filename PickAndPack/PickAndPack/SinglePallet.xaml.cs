using Data.KeyboardContol;
using Data.Message;
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
    public partial class SinglePallet : ContentPage
    {
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        public SinglePallet()
        {
            InitializeComponent();
            txfSOCode.Focused += Entry_Focused;
            txfItemCode.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfSOCode.Focus();
        }       
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
                    await GoodsRecieveingApp.App.Database.Insert(new DocHeader { DocNum = txfSOCode.Text, PackerUser = GoodsRecieveingApp.MainPage.UserCode, AccName = d.SupplierName, AcctCode = d.SupplierCode });
                    LodingIndiactor.IsVisible = false;
                    SOCodeLayout.IsVisible = false;
                    ItemCodeLayout.IsVisible = true;
                    AddSoLayout.IsVisible = true;
                    GridLayout.IsVisible = true;
                    txfItemCode.Focus();
                }
                else
                {
                    txfSOCode.Text = "";
                    txfSOCode.Focus();
                }
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
        private void txfItemCode_Completed(object sender, EventArgs e)
        {

        }
        private async void btnAddSoNumber_Clicked(object sender, EventArgs e)
        {
            string res = await DisplayActionSheet("Open a new Sales Order", "YES", "NO");
            if (res == "YES")
            {
                AddSoLayout.IsVisible = false;
                txfSOCode.IsVisible = true;
                lblSOCode.IsVisible = true;
                GridLayout.IsVisible = false;
            }
        }
        private async void btnViewSO_Clicked(object sender, EventArgs e)
        {
            if (txfSOCode.Text != null || !(txfSOCode.Text.Length < 3))
            {
                message.DisplayMessage("Loading...", false);
                try
                {
                    await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(txfSOCode.Text.ToUpper());
                    await Navigation.PushAsync(new ViewItems(txfSOCode.Text.ToUpper()));
                }
                catch
                {
                    await DisplayAlert("Error", "Could not load info of the entered PO", "Try Again");
                }
            }
            else
            {
                await DisplayAlert("Error", "No SO Entered", "OK");
            }
        }
        private void btnPrevPallet_Clicked(object sender, EventArgs e)
        {

        }
        private void btnNextPallet_Clicked(object sender, EventArgs e)
        {
        
        }
        private void lstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {

        }
        private void btnComplete_Clicked(object sender, EventArgs e)
        {

        }
    }
}