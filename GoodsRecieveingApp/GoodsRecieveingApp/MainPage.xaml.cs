using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Data.Model;
using Data.Message;
using Xamarin.Essentials;
using System.Data;
using Data.KeyboardContol;

namespace GoodsRecieveingApp
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        //public static string APIPath = "http://192.168.10.254/FDBAPI/api/";
        //public static string APIPathIN = "http://192.168.10.254/FDBAPI/api/";
        //public static string APIPathOUT = "http://firstdutchbrands.dvrdns.org:5555/FDBWebServiceAPI/api/";

        //public static string APIPath = "https://manifoldsa.co.za/FDBAPI/api/";
        //public static string APIPathIN = "https://manifoldsa.co.za/FDBAPI/api/";
        //public static string APIPathOUT = "https://manifoldsa.co.za/FDBAPI/api/";
        public static string APIPath = "http://192.168.0.111/FDBAPI/api/";
        public static string APIPathIN = "http://192.168.0.111/FDBAPI/api/";
        public static string APIPathOUT = "http://192.168.0.111/FDBAPI/api/";
        public static string UserName = "";
        public static string ACCWH = "";
        public static string REJWH = "";
        public static int AccessLevel = 0;
        public static int UserCode = 0;
        public static Boolean fReceive = false;
        public static Boolean fRepack = false;
        public static Boolean fInvCount = false;
        public static Boolean fWhTrf = false;
        public static Boolean fPickPack = false;
        public static Boolean AuthWHTrf = false;
        public static Boolean AuthReceive = false;
        public static Boolean AuthDispatch = false;
        public static Boolean PickChecker = false;
        public static Boolean SystAdmin = false;
        public static Boolean CreateInvCount = false;
        public static Boolean CloseInvCount = false;
        public static Boolean PSCollect = false;
        public static Boolean CanPartRec = false;
        public static bool InProccess = false;
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        public MainPage()
        {
            InitializeComponent();
            txfPOCode.Focused += Entry_Focused;
        }
        protected async override void OnAppearing()
        {
            DeviceConfig dev = await App.Database.GetConfig();
            if (dev==null||dev.DefaultAccWH==null||dev.DefaultRejWH==null)
            {
                Vibration.Vibrate();
                await DisplayAlert("Error!", "Please select both default WH in device configuration", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                ACCWH = dev.DefaultAccWH;
                REJWH = dev.DefaultRejWH;
            }
            base.OnAppearing();
            txfPOCode.Focus();          
        }              
        private async void TxfPOCode_TextChanged(object sender, TextChangedEventArgs e)
        {
           
        }
        private async Task GetDocStatus()
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?qry=SELECT DocStatus FROM tblTempDocHeader WHERE DocNum ='{txfPOCode.Text}'";
                    var Request = new RestSharp.RestRequest(str, RestSharp.Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.Content.ToString().Contains("DocStatus"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        if (myds.Tables[0].Rows[0]["DocStatus"] != null)
                        {
                            if (Convert.ToInt32(myds.Tables[0].Rows[0]["DocStatus"]) == 3)
                            {
                                InProccess = true;
                            }
                        }
                    }
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not find document status", false);
            }
        }
        private async void ButtonAccepted_Clicked(object sender, EventArgs e)
        {
            if (txfPOCode.Text.ToString().Length > 1)
            {
                DocLine dl = await App.Database.GetOneSpecificDocAsync(txfPOCode.Text.ToUpper());
                await Navigation.PushAsync(new ScanAcc(dl));
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
                Vibration.Vibrate();
                message.DisplayMessage("Could not Load PO", true);
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
                    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                    {
                        //https://manifoldsa.co.za/FDBAPI/api/GetDocument/GET?qrystr=ACCHISTL|6|PO100352|106|1
                        string str = $"GET?qrystr=ACCHISTL|6|{code}|106|"+MainPage.UserCode;
                        var Request = new RestSharp.RestRequest();
                        Request.Resource = str;
                        Request.Method = RestSharp.Method.GET;
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                        if (res.Content.ToString().Contains("DocNum"))
                        {
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
                                    Doc.Bin = row["Bin"].ToString();
                                    Doc.ItemQty = Convert.ToInt32(row["ItemQty"].ToString().Trim());
                                    Doc.PalletNum = 0;
                                    try
                                    {
                                        Doc.ScanAccQty = Convert.ToInt32(row["ScanAccQty"].ToString());
                                    }
                                    catch
                                    {
                                        Doc.ScanAccQty = 0;
                                    }
                                    try
                                    {
                                        Doc.ScanRejQty = Convert.ToInt32(row["ScanRejQty"].ToString());
                                    }
                                    catch
                                    {
                                        Doc.ScanRejQty = 0;
                                    }                                  
                                   await App.Database.Insert(Doc);
                                }
                                catch (Exception ex)
                                {
                                    Vibration.Vibrate();
                                    message.DisplayMessage("Error in storing items" + ex.Message, true);
                                }
                            }
                            return true;
                        }
                        else
                        {
                            LodingIndiactor.IsVisible = false;
                            Vibration.Vibrate();
                        }
                    }
                }
                else
                {
                    LodingIndiactor.IsVisible = false;
                    Vibration.Vibrate();
                    message.DisplayMessage("Please connect to the internet", true);
                }
            }
            return false;
        }
        private async void ButtonViewS_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Loading....", false);          
            try
            {              
                await App.Database.GetOneSpecificDocAsync(txfPOCode.Text.ToUpper());
                await Navigation.PushAsync(new ViewStock(txfPOCode.Text.ToUpper()));
            }
            catch
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not Load PO", true);
            }           
        }             
        private async Task<bool> RemoveAllOld(string docNum)
        {
            try
            {
                foreach (DocLine dl in (await App.Database.GetSpecificDocsAsync(docNum)))
                {
                    await App.Database.Delete(dl);
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
        private async void SaveClicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Saving....",false);
            if (await SaveData())
            {
                message.DisplayMessage("All data Saved", true);
                await Navigation.PopAsync();
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Error!!! Could Not Save!", true);
            }                
        }
        private async void HomeClicked(object sender, EventArgs e)
        {
            if (txfPOCode.Text!=null)
            {
                string res = await DisplayActionSheet("Are you sure you want to exit before saving", "No", "Yes");
                if (res == "Yes")
                {
                    await Navigation.PopAsync();
                    return;
                }
                else
                {
                    return;
                }
            }
            await Navigation.PopAsync();
        }
        private async Task<bool> SaveData()
        {
            List<DocLine> docs = new List<DocLine>();
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
                t1.Columns.Add("GRV");
                docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemQty==0||x.GRN).ToList();
                if (docs.Count == 0)
                    return true;
                //foreach (string str in docs.Select(x=>x.ItemCode).Distinct())
                //{
                //    int i = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemCode == str && x.ItemQty != 0).Sum(x => x.ScanAccQty);
                //    row = t1.NewRow();
                //    row["DocNum"] = docs.FirstOrDefault().DocNum;
                //    row["ItemBarcode"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemCode == str && x.ItemQty != 0).FirstOrDefault().ItemBarcode;
                //    row["ScanAccQty"] = docs.Where(x => x.ItemCode == str).Sum(x => x.ScanAccQty) + (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemCode == str && x.ItemQty != 0).Sum(x => x.ScanAccQty);
                //    row["Balance"] =0;
                //    row["ScanRejQty"] = docs.Where(x => x.ItemCode == str).Sum(x => x.ScanRejQty)+(await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemCode == str&&x.ItemQty!=0).Sum(x=>x.ScanRejQty);
                //    row["PalletNumber"] = 0;
                //    t1.Rows.Add(row);
                //}                
                foreach (string str in docs.Select(x => x.ItemCode).Distinct())
                {
                    DocLine currentGRV = (await App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.GRN && x.ItemCode == str).FirstOrDefault();
                    if (currentGRV != null)
                    {
                        row = t1.NewRow();
                        row["DocNum"] = txfPOCode.Text;
                        row["ItemBarcode"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemCode == str && x.ItemQty != 0).FirstOrDefault().ItemBarcode; ;
                        row["ScanAccQty"] = currentGRV.ScanAccQty;
                        row["Balance"] = 0;
                        row["ScanRejQty"] = currentGRV.ScanRejQty;
                        row["PalletNumber"] = 0;
                        row["GRV"] = true;
                        t1.Rows.Add(row);
                    }
                    int i = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemCode == str && x.ItemQty != 0).Sum(x => x.ScanAccQty);
                    row = t1.NewRow();
                    row["DocNum"] = docs.FirstOrDefault().DocNum;
                    row["ItemBarcode"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemCode == str && x.ItemQty != 0).FirstOrDefault().ItemBarcode;
                    row["ScanAccQty"] = docs.Where(x => x.ItemCode == str && !x.GRN).Sum(x => x.ScanAccQty) + (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemCode == str && x.ItemQty != 0).Sum(x => x.ScanAccQty);
                    row["Balance"] = 0;
                    row["ScanRejQty"] = docs.Where(x => x.ItemCode == str && !x.GRN).Sum(x => x.ScanRejQty) + (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfPOCode.Text)).Where(x => x.ItemCode == str && x.ItemQty != 0).Sum(x => x.ScanRejQty);
                    row["PalletNumber"] = 0;
                    row["GRV"] = false;
                    t1.Rows.Add(row);
                }
                ds.Tables.Add(t1);
            }
            catch (Exception)
            {
                return false;
            }
            string myds = Newtonsoft.Json.JsonConvert.SerializeObject(ds);
            RestSharp.RestClient client = new RestSharp.RestClient();
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath);
            {
                var Request = new RestSharp.RestRequest("SaveDocLine", RestSharp.Method.POST);
                Request.RequestFormat = RestSharp.DataFormat.Json;
                Request.AddJsonBody(myds);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("COMPLETE"))
                {
                    await RemoveAllOld(docs.FirstOrDefault().DocNum);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

		private async void txfPOCode_Completed(object sender, EventArgs e)
		{
            if (txfPOCode.Text.Length == 8)
            {
                LodingIndiactor.IsVisible = true;
                if (await GetItems(txfPOCode.Text.ToUpper()))
                {
                    _ = GetDocStatus();
                    DocLine d = await App.Database.GetOneSpecificDocAsync(txfPOCode.Text.ToUpper());
                    lblCompany.Text = d.SupplierName.ToUpper();
                    lblPONum.Text = txfPOCode.Text.ToUpper();
                    lblCompany.IsVisible = true;
                    lblPONum.IsVisible = true;
                    txfPOCode.IsEnabled = false;
                    txfPOCode.IsVisible = false;
                    lblPOCode.IsVisible = false;
                    btnAccept.IsVisible = true;
                    btnRej.IsVisible = true;
                    btnAll.IsVisible = true;

                    //ToolbarItem item = new ToolbarItem()
                    //{
                    //    Text = "Save"                    
                    //};
                    //item.Clicked += SaveClicked;
                    //this.ToolbarItems.Add(item);

                    try
                    {
                        await App.Database.Delete(await App.Database.GetHeader(d.DocNum));
                    }
                    catch
                    {

                    }
                    try
                    {
                        await App.Database.Insert(new DocHeader { DocNum = txfPOCode.Text, PackerUser = UserCode, AccName = d.SupplierName, AcctCode = d.SupplierCode });
                    }
                    catch
                    {

                    }
                    LodingIndiactor.IsVisible = false;
                    //await DisplayAlert("Done", "All the data has been loaded for this order", "OK");                       
                }
                else
                {
                    txfPOCode.Text = "";
                    txfPOCode.Focus();
                }

            }
            else
            {
                await DisplayAlert("Error!", "Invalid Document Number", "OK");
                Vibration.Vibrate();
            }
        }
	}
}
