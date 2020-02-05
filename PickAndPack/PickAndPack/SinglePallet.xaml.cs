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
        List<string> allSo = new List<string>();
        bool isNewPallet = true;
        int currentPallet=0;
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
        private async Task<bool> GetItems(string code)
        {
            if (await RemoveAllOld(code))
            {
                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "GetDocument";
                    client.BaseUrl = new Uri("http://192.168.0.108/FDBAPI/api/" + path);
                    {
                        string str = $"GET?qrystr=ACCHISTL|6|{code}|102|"+GoodsRecieveingApp.MainPage.UserCode;
                        var Request = new RestSharp.RestRequest();
                        Request.Resource = str;
                        Request.Method = RestSharp.Method.GET;
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                        if (res.Content.ToString().Contains("DocNum"))
                        {
                            await GoodsRecieveingApp.App.Database.DeleteSpecificDocs(code);
                            DataSet myds = new DataSet();
                            myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                            foreach (DataRow row in myds.Tables[0].Rows)
                            {
                                try
                                {
                                    var Doc = new DocLine();
                                    Doc.DocNum = row["DocNum"].ToString();
                                    Doc.SupplierCode = row["SupplierCode"].ToString();
                                    Doc.SupplierName = row["SupplierName"].ToString();
                                    Doc.ItemBarcode = row["ItemBarcode"].ToString();
                                    Doc.ItemCode = row["ItemCode"].ToString();
                                    Doc.ItemDesc = row["ItemDesc"].ToString();
                                    Doc.ScanAccQty = Convert.ToInt32(row["ScanAccQty"].ToString().Trim());
                                    Doc.ScanRejQty = 0;
                                    Doc.PalletNum = Convert.ToInt32(row["PalletNumber"].ToString().Trim());
                                    if (Doc.PalletNum>0)
                                    isNewPallet = false;
                                    Doc.ItemQty = Convert.ToInt32(row["ItemQty"].ToString().Trim());
                                    lblCode.Text = Doc.DocNum;
                                    lblCusName.Text = Doc.SupplierName;
                                    await GoodsRecieveingApp.App.Database.Insert(Doc);
                                }
                                catch (Exception )
                                {
                                    LodingIndiactor.IsVisible = false;
                                    Vibration.Vibrate();
                                    message.DisplayMessage("Error In Server!!", true);
                                    return false;
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
        private async void txfItemCode_Completed(object sender, EventArgs e)
        {
            if (txfItemCode.Text.Length != 0)
            {
                if (txfItemCode.Text.Length == 8)
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
                        txfItemCode.Focus();
                        return;
                    }
                    if (await CheckOrderItemCode(bi.ItemCode))
                    {
                        List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemCode == bi.ItemCode).ToList();
                        int i = docs.Sum(x => x.ScanAccQty);
                        if (i + bi.Qty <= docs.First().ItemQty)
                        {
                            DocLine docline = new DocLine { Balacnce = 0, Complete = "No", DocNum = txfSOCode.Text, isRejected = false, ItemBarcode = docs.First().ItemBarcode, ItemDesc = docs.First().ItemDesc, ItemCode = docs.First().ItemCode, ItemQty = docs.First().ItemQty, PalletNum = currentPallet, ScanAccQty = bi.Qty };
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
                        if (i + 1 <= docs.First().ItemQty)
                        {
                            DocLine docline = new DocLine { Balacnce = 0, Complete = "No", DocNum = txfSOCode.Text, isRejected = false, ItemBarcode = txfItemCode.Text, ItemDesc = docs.First().ItemDesc, ItemCode = docs.First().ItemCode, ItemQty = docs.First().ItemQty, PalletNum = currentPallet, ScanAccQty = 1};
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
                txfItemCode.Text = "";
                txfItemCode.Focus();
            }
        }
        private async void btnAddSoNumber_Clicked(object sender, EventArgs e)
        {
            string res = await DisplayActionSheet("Open a new Sales Order for this pallet?", "YES", "NO");
            if (res == "YES")
            {
                SOCodeLayout.IsVisible = true;
                AddSoLayout.IsVisible = false;
                txfSOCode.IsVisible = true;
                lblSOCode.IsVisible = true;
                ItemCodeLayout.IsVisible = false;
                GridLayout.IsVisible = false;
                txfSOCode.Text = "";
                txfSOCode.Focus();
            }
        }
        private async void btnViewSO_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Loading...", false);
            try
            {
                //await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(sender.ToString());
                await Navigation.PushAsync(new ViewItems((sender as ToolbarItem).Text));
            }
            catch
            {
                Vibration.Vibrate();
                message.DisplayMessage("Error!Could not load SO", false);
            }
        }
        private async void lstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            DocLine dl = e.SelectedItem as DocLine;
            string output = await DisplayActionSheet("Reset Qty to zero for this item on this pallet?", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    if(await ResetItem(dl)){
                        await GoodsRecieveingApp.App.Database.DeleteAllWithItemWithFilter(dl);
                        await RefreshList();
                    }
                 break;           
            }
        }
        private async Task<bool> ResetItem(DocLine doc)
        {
            var ds = new DataSet();
            try
            {
                var t1 = new DataTable();
                DataRow row = null;
                t1.Columns.Add("DocNum");
                t1.Columns.Add("ItemBarcode");
                t1.Columns.Add("ScanAccQty");
                t1.Columns.Add("Balance");
                t1.Columns.Add("ScanRejQty");
                t1.Columns.Add("PalletNumber");
                row = t1.NewRow();
                row["DocNum"] = doc.DocNum;
                row["ItemBarcode"] = doc.ItemBarcode;
                row["ScanAccQty"] = 0;
                row["Balance"] = 0;
                row["ScanRejQty"] = 0;
                row["PalletNumber"] = doc.PalletNum;
                t1.Rows.Add(row);
                ds.Tables.Add(t1);
                string myds = Newtonsoft.Json.JsonConvert.SerializeObject(ds);
                RestSharp.RestClient client = new RestSharp.RestClient();
                client.BaseUrl = new Uri("http://192.168.0.108/FDBAPI/api/");
                {
                    var Request = new RestSharp.RestRequest("SaveDocLine", RestSharp.Method.POST);
                    Request.RequestFormat = RestSharp.DataFormat.Json;
                    Request.AddJsonBody(myds);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("COMPLETE"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {

            }
            return false;
        }
        private async void txfSOCode_Completed(object sender, EventArgs e)
        {
            if (txfSOCode.Text.Length != 0)
            {
                LodingIndiactor.IsVisible = true;
                if (await GetItems(txfSOCode.Text.ToUpper()))
                {
                    List<DocLine> docsd = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text));
                    foreach(DocLine ds in docsd)
                    {
                        foreach(DocLine docz in docsd){
                            if (docz.PalletNum != ds.PalletNum && ds.PalletNum != 0 && docz.PalletNum != 0)
                            {
                                LodingIndiactor.IsVisible = false;
                                Vibration.Vibrate();
                                message.DisplayMessage("This Sales Order is being scanned onto multiple pallets already!", true);
                                txfSOCode.Text = "";
                                txfSOCode.Focus();
                                return;
                            }
                        }                      
                    }
                    if (isNewPallet)
                        await PalletCreate();
                    DocLine d = docsd.First();
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
                    ToolbarItem item = new ToolbarItem
                    {
                        Text=txfSOCode.Text,
                        Order = ToolbarItemOrder.Secondary
                    };
                    item.Clicked += btnViewSO_Clicked;
                    this.ToolbarItems.Add(item);
                    allSo.Add(txfSOCode.Text);
                    await RefreshList();
                    txfItemCode.Focus();
                }
                else
                {
                    txfSOCode.Text = "";
                    txfSOCode.Focus();
                }
            }
            
        }
        async Task RefreshList()
        {
            lstItems.ItemsSource = null;
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).ToList();
            if (docs == null)
                return;
            List<DocLine> displayDocs = new List<DocLine>();
            foreach (string s in docs.Select(x => x.ItemDesc).Distinct())
            {
                try
                {
                    DocLine TempDoc = docs.Where(x => x.ItemDesc == s).First();
                    TempDoc.ScanAccQty = docs.Where(x => x.ItemDesc == s).Sum(x => x.ScanAccQty);
                    TempDoc.ItemQty = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x =>x.PalletNum==0&& x.ItemDesc == s).First().ItemQty;
                    TempDoc.Balacnce = TempDoc.ItemQty - (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemDesc == s).Sum(x => x.ScanAccQty);
                    displayDocs.Add(TempDoc);
                    if(TempDoc.Balacnce == 0)
                    {
                        TempDoc.Complete = "Yes";
                    }
                    else if (TempDoc.Balacnce!=TempDoc.ItemQty)
                    {
                        TempDoc.Complete = "No";
                    }
                    else
                    {
                        TempDoc.Complete = "NotStarted";
                    }
                }
                catch (Exception exes)
                {
                   await DisplayAlert(exes+"","","OK");
                }
            }
            lstItems.ItemsSource = displayDocs;
        }
        async Task<bool> CheckOrderBarcode(string Code)
        {
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemBarcode == Code).ToList();
            if (docs.Count == 0)
                return false;

            return true;
        }
        async Task<bool> CheckOrderItemCode(string Code)
        {
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemCode == Code).ToList();
            if (docs.Count == 0)
                return false;

            return true;
        }  
        private async Task<int> PalletCreate()
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            string path = "DocumentSQLConnection";
            client.BaseUrl = new Uri("http://192.168.0.108/FDBAPI/api/" + path);
            {
                string qry = "INSERT INTO PalletTransaction(SONum,PalletID) VALUES('" + txfSOCode.Text + "',((SELECT MAX(PalletID) FROM PalletTransaction)+1))";                
                string str = $"Post?qry={qry}";
                var Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("Complete"))
                {
                    var RequestNum = new RestSharp.RestRequest("Get?qry=SELECT MAX(PalletID)As PalletID FROM PalletTransaction", RestSharp.Method.GET);
                    var cancellationToken = new CancellationTokenSource();
                    var Res2 = await client.ExecuteAsync(RequestNum, cancellationToken.Token);
                    if (Res2.IsSuccessful && Res2.Content.Contains("PalletID"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(Res2.Content);
                        return Convert.ToInt32(myds.Tables[0].Rows[0]["PalletID"].ToString());
                    }
                }
                else if (res.Content.Contains("ErrorSystem.Data.SqlClient.SqlException"))
                {
                    string qrys = "Post?qry=INSERT INTO PalletTransaction(SONum,PalletID) VALUES('" + txfSOCode.Text + "',1)";
                    var RequestNum = new RestSharp.RestRequest(qrys, RestSharp.Method.POST);
                    var cancellationToken = new CancellationTokenSource();
                    var Res2 = await client.ExecuteAsync(RequestNum, cancellationToken.Token);
                    if (Res2.IsSuccessful && Res2.Content.Contains("Complete"))
                    {
                        var RequestNumber = new RestSharp.RestRequest("Get?qry=SELECT MAX(PalletID)As PalletID FROM PalletTransaction", RestSharp.Method.GET);
                        var cancellationTokens = new CancellationTokenSource();
                        var Result = await client.ExecuteAsync(RequestNumber, cancellationTokens.Token);
                        if (Result.IsSuccessful && Result.Content.Contains("PalletID"))
                        {
                            DataSet myds = new DataSet();
                            myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(Result.Content);
                            return Convert.ToInt32(myds.Tables[0].Rows[0]["PalletID"].ToString());
                        }
                    }
                }
            }
            return -1;
        }      
        private async void btnSave_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Saving....", true);
            if (await SaveData())
            {
                Navigation.RemovePage(Navigation.NavigationStack[2]);
                await Navigation.PopAsync();
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Error!!! Could Not Save!", true);
            }               
        }
        private async Task<bool> SaveData() 
        {
            var ds = new DataSet();
            try
            {
                var t1 = new DataTable();
                DataRow row = null;
                t1.Columns.Add("DocNum");
                t1.Columns.Add("ItemBarcode");
                t1.Columns.Add("ScanAccQty");
                t1.Columns.Add("Balance");
                t1.Columns.Add("ScanRejQty");
                t1.Columns.Add("PalletNumber");
                foreach (string s in allSo)
                {                                                         
                    List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(s)).ToList();
                    foreach (string str in docs.Select(x => x.ItemDesc).Distinct())
                    {
                        foreach (int ints in docs.Select(x => x.PalletNum).Distinct())
                        {
                            row = t1.NewRow();
                            row["DocNum"] = docs.Select(x=>x.DocNum).FirstOrDefault();
                            row["ScanAccQty"] = docs.Where(x => x.PalletNum == ints && x.ItemDesc == str).Sum(x => x.ScanAccQty); 
                            row["ScanRejQty"] = 0;
                            row["ItemBarcode"] = docs.Where(x => x.PalletNum == ints && x.ItemDesc == str).Select(x => x.ItemBarcode).FirstOrDefault();
                            row["Balance"] = 0;
                            row["PalletNumber"] = ints;
                            t1.Rows.Add(row);
                        }
                    }
                }
                ds.Tables.Add(t1);
            }
            catch(Exception)
            {
                return false;
            }
            string myds = Newtonsoft.Json.JsonConvert.SerializeObject(ds);
            RestSharp.RestClient client = new RestSharp.RestClient();
            client.BaseUrl = new Uri("http://192.168.0.108/FDBAPI/api/");
            {
                var Request = new RestSharp.RestRequest("SaveDocLine", RestSharp.Method.POST);
                Request.RequestFormat = RestSharp.DataFormat.Json;
                Request.AddJsonBody(myds);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("COMPLETE"))
                {
                    return true;
                }
            }
            return false;
        }                       
    }
}