using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace GoodsRecieveingApp
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private string auth = "";
        private string CurrentUser="";
        public MainPage()
        {
            InitializeComponent();
        }

        private void TxfUserCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            Thread.Sleep(100);
            CurrentUser = txfUserCode.Text;
        }
    private async void TxfPOCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            Thread.Sleep(100);
            string code = txfPOCode.Text;
            if (code.Length == 8)
            {
                try
                {
                    string Qstr = "ACCHISTH|4|";//add txfPOCode
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "FindDescAndCode";
                    client.BaseUrl = new Uri("Add path" + path);
                    {
                        string str = $"GET?authDetails={auth}&qrystr={Qstr}";
                        var Request = new RestSharp.RestRequest();
                        Request.Resource = str;
                        Request.Method = RestSharp.Method.GET;
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
                        if (res.IsSuccessful)
                        {

                        }
                    }
                }
                catch
                {
                    await DisplayAlert("Error", "There was an error in connecting to the API", "Okay");
                }
            }
        }
    }
}
