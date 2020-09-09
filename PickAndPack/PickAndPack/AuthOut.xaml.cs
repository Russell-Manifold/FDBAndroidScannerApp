using Data.Message;
using Data.Model;
using RestSharp;
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
	public partial class AuthOut : ContentPage
	{
        IMessage message = DependencyService.Get<IMessage>();
        private List<string> CompleteNums = new List<string>();
        private List<string> ErrorDocs = new List<string>();
        private List<string> CompleteCodes = new List<string>();
        public AuthOut()
		{
			InitializeComponent();
            _=GetItems();
			if (!GoodsRecieveingApp.MainPage.AuthDispatch)
			{
                btnComplete.IsEnabled = false;
			}
        }
        public async void PopData()
        {
            lstItems.ItemsSource = null;
            List<DocLine> list = new List<DocLine>();
            foreach (string docItem in CompleteNums)
			{
                List<DocLine> lines = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docItem);               
                foreach (string s in lines.Select(x => x.ItemCode).Distinct())
                {
                    foreach (int i in lines.Where(x => x.ItemCode == s).Select(x => x.PalletNum).Distinct())
                    {
                        int Pall = lines.Where(x => x.ItemCode == s && x.PalletNum == i).Select(x => x.PalletNum).FirstOrDefault();
                        string itemdesc = lines.Where(x => x.ItemCode == s && x.PalletNum == i).Select(x => x.ItemDesc).FirstOrDefault();
                        DocLine TempDoc = new DocLine() { PalletNum = Pall, ItemDesc = itemdesc };
                        TempDoc.ScanAccQty = (lines.Where(x => x.ItemCode == s && x.PalletNum == i).Sum(x => x.ScanAccQty));
                        TempDoc.ItemQty = (lines.Where(x => x.ItemCode == s).First().ItemQty);
                        TempDoc.Balacnce = TempDoc.ItemQty - TempDoc.ScanAccQty;
                        if (i == 0)
                        {
                            TempDoc.Complete = "Orig";
                        }
                        else
                        {
                            if (TempDoc.Balacnce == 0)
                            {
                                TempDoc.Complete = "Yes";
                            }
                            else if (TempDoc.ScanAccQty == 0)
                            {
                                TempDoc.Complete = "NotStarted";
                            }
                            else
                            {
                                TempDoc.Complete = "No";
                            }
                        }

                        list.Add(TempDoc);
                    }
                }
            }           
            LodingIndiactor.IsVisible = false;
            lstItems.ItemsSource = list;
        }
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {
            if (GoodsRecieveingApp.MainPage.AuthDispatch)
            {
                await SendToPastel();
				if (ErrorDocs.Count>0)
				{
                    await DisplayAlert("Error!", "The following documents could not be uploaded", "View");
					foreach (string s in ErrorDocs)
					{
                        await DisplayAlert("Error!", s, "Next");
                    }
                }
                if (CompleteCodes.Count > 0)
                {
                    await DisplayAlert("Complete!", "The following documents have been uploaded here are the INV numbers", "View");
                    foreach (string s in CompleteCodes)
                    {
                        await DisplayAlert("Complete!", s, "Next");
                    }
                }
            }
			else
			{
                await DisplayAlert("Error!", "You do not have access to send these", "OK");
            }
        }
        private async Task<bool> SendToPastel()
        {
            ErrorDocs.Clear();
			foreach (string docCode in CompleteNums)
			{
                List<DocLine> docs = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
                DataTable det = await GetDocDetails(docCode);
                if (det == null)
                {
                    ErrorDocs.Add(docCode);
                    continue;
                }
                string docL = CreateDocLines(docs, det);
                string docH = CreateDocHeader(det);
                if (docL == "" || docH == "")
                {
                    ErrorDocs.Add(docCode);
                    continue;
                }
                RestClient client = new RestClient();
                string path = "AddDocument";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?DocHead={docH}&Docline={docL}&DocType=103";
                    var Request = new RestRequest(str, Method.POST);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("0"))
                    {                       
                        CompleteCodes.Add(docCode+" - "+res.Content.Split('|')[1]);                       
                    }
                    else if (res.IsSuccessful && res.Content.Contains("99"))
                    {
                        ErrorDocs.Add(docCode+" - "+res.Content);
					}else
					{
                        ErrorDocs.Add(docCode);
                    }
                }
            }
			foreach (string s in CompleteCodes)
			{
                CompleteNums.Remove(s.Trim().Split(' ')[0]);
                await SQLRemove(s.Trim().Split(' ')[0]);
			}
            return true;
        }
        string CreateDocHeader(DataTable det)
        {
            DataRow CurrentRow = det.Rows[0];
            string ret = $"||Y|{CurrentRow["CustomerCode"].ToString()}|{DateTime.Now.ToString("dd/MM/yyyy")}|{CurrentRow["OrderNumber"].ToString()}|N|0|{CurrentRow["Message_1"].ToString()}|{CurrentRow["Message_2"].ToString()}|{CurrentRow["Message_3"].ToString()}|{CurrentRow["Address1"].ToString()}|{CurrentRow["Address2"].ToString()}|{CurrentRow["Address3"].ToString()}|{CurrentRow["Address4"].ToString()}|||{CurrentRow["SalesmanCode"].ToString()}||{Convert.ToDateTime(CurrentRow["Due_Date"]).ToString("dd/MM/yyyy")}||||1";
            return ret.Replace('&', '+').Replace('\'', ' ');
            //||Y|ACK001                                 |05/03/1999                           |                                      |N|0|Message no.1                        |Message no.2                        |Message no.3                        |Delivery no.1                      |Delivery no.2                      |Delivery no.3                      |Delivery no.4                      |||00                                     ||05/03/1999                                                         |011-7402156|Johnny|011-7402157|1
        }
        string CreateDocLines(List<DocLine> d, DataTable det)
        {
            string s = "";
            foreach (string CurrItem in d.Where(x => x.PalletNum == 0).Select(x => x.ItemCode).Distinct())
            {
                DataRow CurrentRow = det.Select($"ItemCode='{CurrItem}'").FirstOrDefault();
                if (CurrentRow != null)
                    s += $"{CurrentRow["CostPrice"].ToString()}|{CurrentRow["ItemQty"].ToString()}|{CurrentRow["ExVat"].ToString()}|{CurrentRow["InclVat"].ToString()}|{CurrentRow["Unit"].ToString()}|{CurrentRow["TaxType"].ToString()}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString()}|{CurrentRow["ItemCode"].ToString()}|{CurrentRow["ItemDesc"].ToString()}|4|001%23";
                //                                 285 | 1                                | 350.88                         | 400.00                           | EACH                          | 01                               |                                   |                                   | ACC /                             |                       Description |4|001             
            }
            return s;
        }
        private async Task SQLRemove(string doc)
		{
            RestClient client = new RestClient();
            string path = "DocumentSQLConnection";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?qry=DELETE * FROM tblTempDocLines,tblTempDocHeader WHERE DocNum='" + doc + "'";
                var Request = new RestRequest(str, Method.GET);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("0"))
                {
                }
            }
        }
        private async Task<bool> Check()
        {
            foreach(string docCode in CompleteNums)
			{
                List<DocLine> lines = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
                foreach (DocLine dc in lines.Where(x => x.PalletNum == 0))
                {
                    foreach (DocLine dl in lines.Where(x => x.PalletNum != 0))
                    {
                        if (dc.ItemCode == dl.ItemCode)
                        {
                            dc.Balacnce += dl.ScanAccQty;
                        }
                    }
                    if (dc.Balacnce != dc.ItemQty)
                    {
                        return false;
                    }
                }              
            }
            return true;
        }
        async Task<DataTable> GetDocDetails(string DocNum)
        {//https://manifoldsa.co.za/FDBAPI/api/GetFullDocDetails/GET?qrystr=ACCHISTL|6|IO170852|102
            RestClient client = new RestClient();
            string path = "GetFullDocDetails";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?qrystr=ACCHISTL|6|{DocNum}|102";
                var Request = new RestRequest(str, Method.GET);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("0"))
                {
                    DataSet myds = new DataSet();
                    myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                    return myds.Tables[0];
                }
            }
            return null;
        }
        private async Task GetAllHeaders()
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            string path = "DocumentSQLConnection";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?qry=SELECT DocNum FROM tblTempDocHeader WHERE Complete=True";
                var Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.GET;
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.Content.ToString().Contains("DocNum"))
                {
                    DataSet myds = new DataSet();
                    myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                    CompleteNums.Clear();
                    foreach (DataRow row in myds.Tables[0].Rows)
                    {
                        CompleteNums.Add(row["DocNum"].ToString());
                        await RemoveAllOld(row["DocNum"].ToString());
                    }
                }
            }
        }
        private async Task<bool> GetItems()
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {               
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    await GetAllHeaders();
                    foreach (string strNum in CompleteNums)
					{
                        string str = $"GET?qry=SELECT * FROM tblTempDocLines WHERE DocNum='"+strNum+"'";
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
                                    await RemoveAllOld(row["DocNum"].ToString());
                                    Doc.DocNum = row["DocNum"].ToString();
                                    Doc.SupplierCode = row["SupplierCode"].ToString();
                                    Doc.SupplierName = row["SupplierName"].ToString();
                                    Doc.ItemBarcode = row["ItemBarcode"].ToString();
                                    Doc.ItemCode = row["ItemCode"].ToString();
                                    Doc.ItemDesc = row["ItemDesc"].ToString();
                                    Doc.Bin = row["Bin"].ToString();
                                    try
                                    {
                                        Doc.ScanAccQty = Convert.ToInt32(row["ScanAccQty"].ToString().Trim());
                                    }
                                    catch
                                    {
                                        Doc.ScanAccQty = 0;
                                    }
                                    Doc.ScanRejQty = 0;
                                    try
                                    {
                                        Doc.PalletNum = Convert.ToInt32(row["PalletNumber"].ToString().Trim());
                                    }
                                    catch
                                    {
                                        Doc.PalletNum = 0;
                                    }
                                    try
                                    {
                                        Doc.Balacnce = Convert.ToInt32(row["Balacnce"].ToString().Trim());
                                    }
                                    catch
                                    {
                                        Doc.Balacnce = 0;
                                    }
                                    if (Convert.ToInt32(Doc.Balacnce) == -1)
                                    {
                                        Doc.Balacnce = 0;
                                    }
                                    Doc.ItemQty = Convert.ToInt32(row["ItemQty"].ToString().Trim());
                                    await GoodsRecieveingApp.App.Database.Insert(Doc);
                                }
                                catch (Exception ex)
                                {
                                    LodingIndiactor.IsVisible = false;
                                    Vibration.Vibrate();
                                    message.DisplayMessage("Error In Server!!" + ex, true);
                                    return false;
                                }
                            }
                        }
                    }
                }
                PopData();
            }
            return false;
        }
        private async Task<bool> RemoveAllOld(string docNum)
        {
            try
            {               
                foreach (DocLine dl in (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docNum)))
                {
                    await GoodsRecieveingApp.App.Database.Delete(dl);
                }
                await GoodsRecieveingApp.App.Database.Delete(await GoodsRecieveingApp.App.Database.GetHeader(docNum));
            }
            catch
            {
            }
            return true;
        }
    }
}