using Data.KeyboardContol;
using Data.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Data;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WHTransfer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InPage : ContentPage
    {
        private List<IBTHeader> headers = new List<IBTHeader>();
        private List<IBTItem> lines = new List<IBTItem>();
        private List<string> PickerItems = new List<string>();
        private List<string> DoneItems = new List<string>();
        private IBTHeader CurrentHeader;
        private ExtendedEntry _currententry;
        public InPage()
        {
            InitializeComponent();
            txfScannedItems.Focused += Entry_Focused;
            txfScannedItems.Focus();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            pickerHeaders.IsEnabled = false;
            isLoading.IsVisible = true;
            await FetchHeaders();
            pickerHeaders.IsEnabled = true;
            isLoading.IsVisible = false;
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
        private async void TxfScannedItem_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(100);
            if (txfScannedItems.Text.Length > 1)
            {
                if (!CheckItem(txfScannedItems.Text))
                {
                    await DisplayAlert("Error!", "Please make sure this item is on this order and hasnt been scanned yet", "Okay");
                }
                else
                {
                    _=IsDone();
                    await Task.Delay(100);
                    ListViewItems.ItemsSource = null;
                    ListViewItems.ItemsSource = lines;
                }
                txfScannedItems.Text = "";
                txfScannedItems.Focus();
            }
        }
        private bool CheckItem(string barcode)
        {
            try
            {
                lines.Where(x => x.ItemBarcode == barcode && x.ItemQtyIn == 0).First().ItemQtyIn = -1;
                return true;
            }
            catch
            {

            }         
            return false;
        }
        private async Task FetchHeaders()
        {
            try
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "IBTHeader";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"GET?";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content != null)
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            try
                            {
                                headers.Add(new IBTHeader{ID=Convert.ToInt32(row["TrfId"]),TrfDate=row["TrfDate"].ToString(),FromWH=row["FromWH"].ToString(),ToWH=row["ToWH"].ToString(),FromDate=row["FromDate"].ToString(),RecDate=row["RecDate"].ToString(),PickerUser = Convert.ToInt32(row["PickerUser"].ToString()), AuthUser=Convert.ToInt32(row["AuthUser"].ToString()), Active=Convert.ToBoolean(row["Active"]) });
                                PickerItems.Add(row["TrfId"].ToString());
                            }
                            catch
                            {

                            }
                        }
                        pickerHeaders.ItemsSource = PickerItems;
                    }
                }
            }
            catch
            {

            }
        }
        private async void PickerHeaders_SelectedIndexChanged(object sender, EventArgs e)
        {
            isLoading.IsVisible = true;
            CurrentHeader =headers.Where(x=>x.ID==Convert.ToInt32(pickerHeaders.SelectedItem.ToString())).FirstOrDefault();
            await GetLines(CurrentHeader.ID);
            pickerHeaders.IsVisible = false;
            lblTop.IsVisible = false;
            LayoutMain.IsVisible = true;
            lblInfo.Text = "Transfer number :"+CurrentHeader.ID+" started on date :"+CurrentHeader.TrfDate;
            ListViewItems.ItemsSource = lines;
            isLoading.IsVisible = false;
            //txfScannedItem.Focus();
        }
        private async Task<bool> GetLines(int trf)
        {
            try
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "IBTLines";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"Get?qry=SELECT* FROM tblIBTLines WHERE iTrfId={trf}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content != null)
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            try
                            {
                                lines.Add(new IBTItem {ScanBarcode=row["ScanBarcode"].ToString(),ItemBarcode = row["ItemBarcode"].ToString(), ItemCode = row["ItemCode"].ToString(), ItemDesc = row["ItemDesc"].ToString(), ItemQtyOut = Convert.ToInt32(row["ItemQtyOut"]), ItemQtyIn=Convert.ToInt32(row["ItemQtyIn"]), PickerUser=Convert.ToInt32(row["PickerUser"]),AuthUser=Convert.ToInt32(row["AuthUser"]),PickDateTime=Convert.ToDateTime(row["PickDateTime"]),WH=row["WH"].ToString(),iTrfID=Convert.ToInt32(row["iTrfId"])});
                            }
                            catch
                            {

                            }
                        }
                        return true;
                    }
                }
            }
            catch
            {

            }
            return false;
        }
        private async Task IsDone()
        {
            var changes = new IBTItem() ;
            try
            {
                changes = lines.Where(x => x.ItemQtyIn == 0).First();
            }
            catch (InvalidOperationException)
            {
                 await DisplayAlert("Complete!", "All items have been scanned in", "Okay");
                 await Complete();
            }         
                    
        }
        private async Task Complete()
        {
            try
            {              
                foreach (IBTItem i in lines)
                {
                    if (!DoneItems.Contains(i.ItemCode)) {
                        DoneItems.Add(i.ItemCode);
                        int k = lines.Where(x => x.ItemCode == i.ItemCode).Sum(c=>c.ItemQtyOut);
                        RestSharp.RestClient client2 = new RestSharp.RestClient();
                        string path2 = "WHTransfer";
                        client2.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path2);
                        {
                            string str2 = $"POST?itemCode={i.ItemCode}&InOrOut=true&JnlDate={DateTime.Now.ToString("dd MMM yyyy")}&JobCode={i.iTrfID}&Desc={i.iTrfID}&Ref={DateTime.Now.ToString("ddMMMyyyy")+"-"+i.iTrfID}&Qty={k}&Store={i.WH}";
                            var Request2 = new RestSharp.RestRequest();
                            Request2.Resource = str2;
                            Request2.Method = RestSharp.Method.POST;
                            var cancellationTokenSource2 = new CancellationTokenSource();
                            var res2 = await client2.ExecuteAsync(Request2, cancellationTokenSource2.Token);
                            if (!(res2.IsSuccessful && res2.Content != null))
                            {
                                await DisplayAlert("Error!", "Could not delete record", "Okay");
                                return;
                            }
                        }
                    }
                }
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "IBTHeader";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"DELETE?coid={CurrentHeader.ID}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.DELETE;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (!(res.IsSuccessful && res.Content != null))
                    {
                        await DisplayAlert("Error!", "Could not delete record", "Okay");
                        return;
                    }
                }
            }
            catch
            {
            }
            await DisplayAlert("Complete!","All items have been saved","Okay");
            await Navigation.PopToRootAsync();
        }
        private async void Button_Clicked(object sender, EventArgs e)
        {
            btnComplete.BackgroundColor = Color.Red;
            await IsDone();                                
            btnComplete.BackgroundColor = Color.Green;
        }
    }
}