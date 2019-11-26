using Data.KeyboardContol;
using Data.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WHTransfer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InPage : ContentPage
    {
        private List<IBTHeader> headers = new List<IBTHeader>();
        private List<string> PickerItems = new List<string>();
        private IBTHeader CurrentHeader;
        private ExtendedEntry _currententry;
        public InPage()
        {
            InitializeComponent();
            txfScannedItem.Focused += Entry_Focused;
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            await FetchHeaders();
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

        }
        private async Task<bool> CheckItem()
        {
            try
            {

            }catch{

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
                    var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content != null)
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            try
                            {
                                headers.Add(new IBTHeader{ID=Convert.ToInt32(row["TrfId"]),TrfDate=row["TrfId"].ToString(),FromWH=row["FromWH"].ToString(),ToWH=row["ToWH"].ToString(),FromDate=row["FromDate"].ToString(),RecDate=row["RecDate"].ToString(),PickerUser = Convert.ToInt32(row["PickerUser"].ToString()), AuthUser=Convert.ToInt32(row["AuthUser"].ToString()), Active=Convert.ToBoolean(row["Active"]) });
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
        private void PickerHeaders_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentHeader =headers.Where(x=>x.ID==Convert.ToInt32(pickerHeaders.SelectedItem.ToString())).FirstOrDefault();
            
            pickerHeaders.IsVisible = false;
            LayoutMain.IsVisible = true;
            lblInfo.Text = "Transfer number :"+CurrentHeader.ID+" started on date :"+CurrentHeader.TrfDate;
        }
    }
}