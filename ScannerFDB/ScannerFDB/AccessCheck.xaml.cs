﻿using Data.KeyboardContol;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
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
                RestClient client = new RestClient();
                string path = "GetUser";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"GET?UserName={txfUserBarcode.Text}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
                    cancellationTokenSource.Dispose();
                    if (res.IsSuccessful && res.Content != null)
                    {
                        int userAcccess = 0;
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            userAcccess = Convert.ToInt32(row["AccessLevel"].ToString());
                        }                            
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
                await DisplayAlert("Error!", "Invalid User Access", "OK");                             
            }
            return false;
        }
    }
}