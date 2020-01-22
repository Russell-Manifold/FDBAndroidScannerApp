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
    public partial class SingleOrder : ContentPage
    {
        IMessage message = DependencyService.Get<IMessage>();        
        private ExtendedEntry _currententry;
        public SingleOrder()
        {
            InitializeComponent();
            txfSOCode.Focused += Entry_Focused;
            txfItemCode.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Task.Delay(100);
            txfSOCode.Focus();
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
        private async void txfSOCode_Completed(object sender, EventArgs e)
        {
            LodingIndiactor.IsVisible = true;
            if (await GetItems(txfSOCode.Text.ToUpper()))
            {
                DocLine d = new DocLine();
                try
                {
                    d = await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(txfSOCode.Text);
                    txfSOCode.IsVisible = false;
                    await GoodsRecieveingApp.App.Database.Delete(await GoodsRecieveingApp.App.Database.GetHeader(d.DocNum));
                }
                catch
                {

                }
                await GoodsRecieveingApp.App.Database.Insert(new DocHeader { DocNum = txfSOCode.Text, PackerUser = GoodsRecieveingApp.MainPage.UserCode, AccName = d.SupplierName, AcctCode = d.SupplierCode });
                LodingIndiactor.IsVisible = false;
                lblPalletForOrder.Text = "1";
                PalletLayout.IsVisible = true;
                ItemCodeLayout.IsVisible = true;
                GridLayout.IsVisible = true;
                txfItemCode.Focus();
            }
            else
            {               
                txfSOCode.Text = "";
                txfSOCode.Focus();
            }
        }
        private async Task<bool> GetItems(string code)
        {
            if (await RemoveAllOld(code))
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
                                    lblSOCode.Text = Doc.DocNum+"-"+Doc.SupplierName;
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
                            Vibration.Vibrate();
                            message.DisplayMessage("Error Invalid SO Code!!", true);
                        }
                    }
                }
                else
                {
                    LodingIndiactor.IsVisible = false;
                    Vibration.Vibrate();
                    message.DisplayMessage("Internet Connection Problem!", true);
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
        private async void btnViewSO_Clicked(object sender, EventArgs e)
        {
                message.DisplayMessage("Loading...", false);
                try
                {
                    await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(txfSOCode.Text.ToUpper());
                    await Navigation.PushAsync(new ViewItems(txfSOCode.Text.ToUpper()));
                }
                catch
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("Error!Could not load SO", false);
                }                           
        }
        private async void btnAddPallet_Clicked(object sender, EventArgs e)
        {           
            int palletNum = Convert.ToInt32(lblPalletForOrder.Text.ToString());
            foreach (DocLine dl in await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text))
            {
                if (dl.PalletNum==palletNum)
                {
                    lblPalletForOrder.Text = (palletNum+1)+"";
                    return;
                }   
            }
            Vibration.Vibrate();
            message.DisplayMessage("Error!- You cant go to the next pallet if this one has no items",false);
        }
        private async void txfItemCode_Completed(object sender, EventArgs e)
        {
            if (txfItemCode.Text.Length==8)
            {
                BOMItem bi = new BOMItem();
                try
                {
                     bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfItemCode.Text);
                }
                catch
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("Error! No Item with this code", true);
                    txfItemCode.Text = "";
                    return;
                }
                if(await CheckOrderItemCode(bi.ItemCode))
                {
                    List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemCode == bi.ItemCode).ToList();
                    int i = docs.Sum(x=>x.ScanAccQty);
                    if (i + bi.Qty <= docs.Where(x => x.ScanAccQty == 0).First().ItemQty)
                    {
                        DocLine docline = new DocLine { Balacnce = 0, Complete = "Yes", DocNum = txfSOCode.Text, isRejected = false, ItemBarcode = docs.Where(x => x.ScanAccQty == 0).First().ItemBarcode, ItemDesc = docs.Where(x => x.ScanAccQty == 0).First().ItemDesc, ItemCode = docs.Where(x => x.ScanAccQty == 0).First().ItemCode, ItemQty = bi.Qty, PalletNum = Convert.ToInt32(lblPalletForOrder.Text), ScanAccQty = bi.Qty, WarehouseID = docs.Where(x => x.ScanAccQty == 0).First().WarehouseID, SupplierCode = docs.Where(x => x.ScanAccQty == 0).First().SupplierCode, SupplierName = docs.Where(x => x.ScanAccQty == 0).First().SupplierName };
                        await GoodsRecieveingApp.App.Database.Insert(docline);
                        await RefreshList();
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("All of this item have been scanned for this order", true);
                    }
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("This Item is not on this order", true);
                }
            }
            else
            {
                if (await CheckOrderBarcode(txfItemCode.Text))
                {
                    List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemBarcode == txfItemCode.Text).ToList();
                    int i = docs.Sum(x => x.ScanAccQty);
                    if (i+1<=docs.Where(x => x.ScanAccQty == 0).First().ItemQty)
                    {
                        DocLine docline = new DocLine { Balacnce = 0, Complete = "Yes", DocNum = txfSOCode.Text, isRejected = false,ItemBarcode=txfItemCode.Text,ItemDesc=docs.Where(x=>x.ScanAccQty==0).First().ItemDesc, ItemCode = docs.Where(x => x.ScanAccQty == 0).First().ItemCode,ItemQty=1,PalletNum=Convert.ToInt32(lblPalletForOrder.Text),ScanAccQty=1, WarehouseID=docs.Where(x => x.ScanAccQty == 0).First().WarehouseID,SupplierCode= docs.Where(x => x.ScanAccQty == 0).First().SupplierCode, SupplierName = docs.Where(x => x.ScanAccQty == 0).First().SupplierName };
                        await GoodsRecieveingApp.App.Database.Insert(docline);
                        await RefreshList();
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("All of this item have been scanned for this order", true);
                    }
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("This Item is not on this order", true);
                }
            }
        }
        async Task<bool> CheckOrderBarcode(string Code)
        {
            List<DocLine> docs =(await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x=>x.ItemBarcode==Code).ToList();
            if (docs == null)
                return false;

            return true;
        }
        async Task<bool> CheckOrderItemCode(string Code)
        {
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemCode == Code).ToList();
            if (docs.Count==0)
                return false;

            return true;
        }
        async Task RefreshList()
        {
            lstItems.ItemsSource = null;
            lstItems.ItemsSource = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x=>x.PalletNum==Convert.ToInt32(lblPalletForOrder.Text));
        }
        private async void lstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            DocLine dl = e.SelectedItem as DocLine;
            string output = await DisplayActionSheet("Confirm:-Reset QTY to zero?", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                        await GoodsRecieveingApp.App.Database.Delete(dl);
                        await RefreshList();
                    
                    break;
            }
        }

        private void btnPrevPallet_Clicked(object sender, EventArgs e)
        {

        }

        private void btnNextPallet_Clicked(object sender, EventArgs e)
        {

        }
    }
}