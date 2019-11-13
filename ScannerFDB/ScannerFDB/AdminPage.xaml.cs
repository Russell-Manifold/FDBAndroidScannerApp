﻿using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ScannerFDB
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AdminPage : ContentPage
    {
        private string auth = "DK198110007|5635796|C:/FDBManifoldData/FDB2020";
        public AdminPage()
        {
            InitializeComponent();
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            LodingIndiactor.IsVisible = true;
           var current = Connectivity.NetworkAccess;
            var profiles = Connectivity.ConnectionProfiles;
            if (current == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "Find";
                client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                {
                    string str = $"GET?authDetails={auth}&qrystr=ACCBOML|0|0&posInString=0&searchValue=0";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteTaskAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful)
                    {
                        await GoodsRecieveingApp.App.Database.DeleteBOMData();
                        foreach (string s in res.Content.Split('#'))
                        {
                            if (s.Contains("0"))
                            {
                                BOMItem item = new BOMItem();
                                item.PackBarcode = s.Split('|')[1];
                                item.ItemCode = s.Split('|')[3];
                                item.Qty = Convert.ToInt32(s.Split('|')[4]);
                                await GoodsRecieveingApp.App.Database.Insert(item);
                            }                                                                                                        
                        }
                        LodingIndiactor.IsVisible = false;
                        await DisplayAlert("Complete","All of the BOM's have been fetched","OK");
                    }
                }
            }
            else
            {
                LodingIndiactor.IsVisible = false;
                await DisplayAlert("Error!!", "Please Reconnect to the internet", "OK");
            }
        }

        private void Button_Clicked_1(object sender, EventArgs e)
        {

        }
    }
}