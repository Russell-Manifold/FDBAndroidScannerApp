using Data.KeyboardContol;
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

namespace InventoryCount
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AcceptScanPage : ContentPage
    {
        private ExtendedEntry _currententry;
        IMessage message = DependencyService.Get<IMessage>();
        InventoryItem i = new InventoryItem();
        public AcceptScanPage(InventoryItem ite)
        {
            InitializeComponent();
            txfUserCode.Focused += Entry_Focused;
            i = ite;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            lblInfo.Text = "Approve QTY for:\n"+i.ItemDesc+"\nFirst Count: "+i.FirstScanQty+"\nSecond Count: "+i.SecondScanQty+"";
            txfUserCode.Focus();
        }
        private async void txfUserCode_Completed(object sender, EventArgs e)
        {
            LoadingIndicator.IsVisible = true;
            txfUserCode.Completed -= txfUserCode_Completed;
            txfUserCode.Text =GoodsRecieveingApp.MainPage.CalculateCheckDigit(txfUserCode.Text);
            try
            {
                RestClient client = new RestClient();
                string path = "GetUser";
                //client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?UserName={txfUserCode.Text}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("UserName"))
                    {
                        bool inv = false;
                        int id = 0;
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {                           
                            inv = Convert.ToBoolean(row["InvReCountAUTH"]);
                            id = Convert.ToInt32(row["Id"].ToString());
                        }
                        if (inv)
                        {
                            CountPage.items.Where(x => x.ItemCode == i.ItemCode).Select(x=> { x.SecondScanAuth = id;x.Complete = true;return x; });
                            await Navigation.PopAsync();
                        }
                    }
                    else
                    {
                        LoadingIndicator.IsVisible = false;
                        message.DisplayMessage("Invalid user!", true);
                        Vibration.Vibrate();
                        txfUserCode.Text = "";
                        txfUserCode.Focus();
                        return;
                    }
                }
            }
            catch
            {
                LoadingIndicator.IsVisible = false;
                message.DisplayMessage("Invalid user!", true);
                Vibration.Vibrate();
                txfUserCode.Text = "";
                txfUserCode.Focus();
                return;
            }
            txfUserCode.Completed += txfUserCode_Completed;
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