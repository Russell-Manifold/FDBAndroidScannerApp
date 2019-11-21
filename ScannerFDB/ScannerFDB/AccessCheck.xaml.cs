using Data.KeyboardContol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ScannerFDB
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AccessCheck : ContentPage
    {
        private ExtendedEntry _currententry;
        public AccessCheck()
        {
            InitializeComponent();
            txfUserBarcode.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();      
            txfUserBarcode.Focus();
        }
        private async void TxfUserBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(200);
            if (!await CheckUser())
            {
                txfUserBarcode.Text = "";
                txfUserBarcode.Focus();
            }
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

        private async Task<bool> CheckUser()
        {
            AccessLoading.IsVisible = true;
            try
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "GetUser";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"GET?UserName={txfUserBarcode.Text}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content != null)
                    {
                        int userAcccess = Convert.ToInt32(res.Content.Replace('\\', ' ').Replace('\"', ' ').Trim());
                        if (userAcccess > 1)
                        {
                            await Navigation.PushAsync(new AdminPage(userAcccess));
                            AccessLoading.IsVisible = false;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                AccessLoading.IsVisible = false;
                await DisplayAlert("Error!", "Invalid User Access", "Okay");                             
            }
            return false;
        }
    }
}