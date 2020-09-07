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
        private static string docCode = "";
        IMessage message = DependencyService.Get<IMessage>();
        public AuthOut()
		{
			InitializeComponent();
		}
        public async void PopData()
        {
            lstItems.ItemsSource = null;
            List<DocLine> lines = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
            List<DocLine> list = new List<DocLine>();
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
            //list.RemoveAll(x => x.ItemCode.Length < 2);
            //lstItems.ItemsSource = list.OrderBy(x => new { x.ItemDesc , x.PalletNum } );
            lstItems.ItemsSource = list;
        }
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {

            if (GoodsRecieveingApp.MainPage.AuthDispatch)
            { 
                if (!await SendToPastel())
                    await DisplayAlert("Error!", "Could not send data to pastel", "OK");
			}
			else
			{
                await DisplayAlert("Error!", "You do not have access to send these", "OK");
            }


        }
        private async Task<bool> SendToPastel()
        {
            //Do a foreach for every docline
            List<DocLine> docs = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
            DataTable det = await GetDocDetails(docCode);
            if (det == null)
            {
                return false;
            }
            string docL = CreateDocLines(docs, det);//33.5|6|60|69|PCE|15|3|0|1215|8 Lt clear locked storage box|4|001%2342.5|6|77.6|89.24|PCE|15|3|0|1216|13 Lt clear locked storage box|4|001%2358|12|108.1|124.32|PCE|15|3|0|1217|20 Lt clear locked storage box|4|001%23101|8|170|195.5|PCE|15|3|0|6716000084401|Filo Laundry Hamper Romantic Ivory|4|001%23
            string docH = CreateDocHeader(det);//||Y|TAO01|29/06/2020|IO170852|N|0||||Take a Lot  JHB Distrubution|Cnr Riverfields Boulevard &|First Road, Witfontein Ext 54|Kempton Park,Johannesburg 1619|||||27/09/2019||||1
            if (docL == "" || docH == "")
                return false;
            RestClient client = new RestClient();
            string path = "AddDocument";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?DocHead={docH}&Docline={docL}&DocType=103";
                var Request = new RestRequest(str, Method.POST);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                //System.IndexOutOfRangeException: Subscript out of range
                //at PasSDK._PastelPartnerSDK.DefineDocumentHeader(String Data, Boolean& AdditionalCostInvoice)
                //at FDBWebAPI.Controllers.AddDocumentController.AddDocument(String DocHead, String Docline, String DocType) in E:\\GithubRepos\\FDB\\FirstDutchWebServiceAPI\\FDBWebAPI\\Controllers\\AddDocumentController.cs:line 38"
                if (res.IsSuccessful && res.Content.Contains("0"))
                {
                    await DisplayAlert("Complete!", "Invoice " + res.Content.Split('|')[1] + " successfully generated in Pastel", "OK");
                    return true;
                }
                else if (res.IsSuccessful && res.Content.Contains("99"))
                {
                    await DisplayAlert("Error!", "Your document could not be sent due to" + Environment.NewLine + res.Content, "OK");
                    return true;
                }
            }
            return false;
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
        private async Task<bool> Check()
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
        private async Task<bool> GetItems()
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?qry=";
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
                            var Doc = new DocLine();
                            await RemoveAllOld(row["DocNum"].ToString());
                        }
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
            }
            catch
            {
            }
            return true;
        }
    }
}