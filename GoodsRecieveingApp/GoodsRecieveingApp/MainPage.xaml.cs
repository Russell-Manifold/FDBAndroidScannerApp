﻿using System;
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
        public static string UserName = "";
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

        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        public MainPage()
        {
            InitializeComponent();
            txfPOCode.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfPOCode.Focus();
        }       
        //private async void TxfUserCode_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    await Task.Delay(200);
        //    CurrentUser = txfUserCode.Text;
        //    if (CurrentUser.Length>1)
        //    {
        //        LodingIndiactor.IsVisible = true;
        //        if (await userCheck()) {
        //            LodingIndiactor.IsVisible = false;
        //            txfUserCode.IsVisible = false;
        //            lblUserCode.IsVisible = false;
        //            txfPOCode.IsVisible = true;
        //            lblPOCode.IsVisible = true;
        //            txfPOCode.Text = "";
        //            await Task.Delay(100);
        //            txfPOCode.Focus();
        //        }
        //        else
        //        {
        //            LodingIndiactor.IsVisible = false;
        //            txfUserCode.Text = "";
        //            await DisplayAlert("Error!", "Invalid User", "OK");
        //            txfUserCode.Focus();
        //        }
        //    }
        //}
        //private async Task<bool> userCheck()
        //{
        //    try
        //    {
        //        RestSharp.RestClient client = new RestSharp.RestClient();
        //        string path = "GetUser";
        //        client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
        //        {
        //            string str = $"GET?UserName={CurrentUser}";
        //            var Request = new RestSharp.RestRequest();
        //            Request.Resource = str;
        //            Request.Method = RestSharp.Method.GET;
        //            var cancellationTokenSource = new CancellationTokenSource();
        //            var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
        //            if (res.IsSuccessful && res.Content != null)
        //            {
        //                DataSet myds = new DataSet();
        //                myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
        //                foreach (DataRow row in myds.Tables[0].Rows)
        //                {
        //                    try
        //                    {
        //                        if (Convert.ToInt32(row["AccessLevel"])>0)
        //                        {
        //                            return true;
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Console.WriteLine(ex);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch
        //    {                
        //    }          
        //    return false;
        //}
        private async void TxfPOCode_TextChanged(object sender, TextChangedEventArgs e)
        {       
            if (txfPOCode.Text.Length == 8)
            {            
                LodingIndiactor.IsVisible = true;
                if (await GetItems(txfPOCode.Text.ToUpper()))
                {
                    DocLine d = await App.Database.GetOneSpecificDocAsync(txfPOCode.Text.ToUpper());
                    lblCompany.Text = d.SupplierName.ToUpper();
                    lblPONum.Text = txfPOCode.Text.ToUpper();
                    lblCompany.IsVisible = true;
                    lblPONum.IsVisible = true;
                    txfPOCode.IsVisible = false;
                    lblPOCode.IsVisible = false;
                    btnAccept.IsVisible = true;
                    btnRej.IsVisible = true;
                    btnAll.IsVisible = true;
                        
                    try
                    {
                        await App.Database.Delete(await App.Database.GetHeader(d.DocNum));
                    }
                    catch
                    {

                    }                      
                    await App.Database.Insert(new DocHeader {DocNum= txfPOCode.Text,PackerUser= UserCode, AccName=d.SupplierName,AcctCode=d.SupplierCode});
                    LodingIndiactor.IsVisible = false;
                    //await DisplayAlert("Done", "All the data has been loaded for this order", "OK");                       
                }
                else
                {
                    txfPOCode.Text = "";
                    txfPOCode.Focus();
                }          
            }
        }     
        private async void ButtonAccepted_Clicked(object sender, EventArgs e)
        {
            try
            {
                DocLine dl = await App.Database.GetOneSpecificDocAsync(txfPOCode.Text.ToUpper());
                await Navigation.PushAsync(new ScanAcc(dl));
            }
            catch
            {
                await DisplayAlert("Error", "Could not load info of the entered PO", "Try Again");
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
                await DisplayAlert("Error", "Could not load info of the entered PO", "Try Again");
            }
        }
        private async Task<bool> GetItems(string code)
        {
            if (await RemoveAllOld(code))
            {
                var current = Connectivity.NetworkAccess;
                var profiles = Connectivity.ConnectionProfiles;
                if (current == NetworkAccess.Internet)
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "GetDocument";
                    client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                    {
                        string str = $"GET?qrystr=ACCHISTL|6|{code}|106|" + GoodsRecieveingApp.MainPage.UserCode;
                        var Request = new RestSharp.RestRequest();
                        Request.Resource = str;
                        Request.Method = RestSharp.Method.GET;
                        var cancellationTokenSource = new CancellationTokenSource();
                        try { 
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
                                        Doc.ScanAccQty = 0;
                                        Doc.ScanRejQty = 0;
                                        Doc.ItemQty = Convert.ToInt32(row["ItemQty"].ToString().Trim());
                                        await App.Database.Insert(Doc);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                    }
                                }
                                return true;
                            }
                            else
                            {
                                LodingIndiactor.IsVisible = false;
                                await DisplayAlert("Error", "There is no data in this PO", "OK");
                            }
                        }
                        catch (Exception ex) { }
                    }
                }
                else
                {
                    LodingIndiactor.IsVisible = false;
                    await DisplayAlert("Error!!", "Please Reconnect to the internet", "OK");
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
                await DisplayAlert("Error","Could not load info of the entered PO","Try Again");
            }           
        }             
        private async Task<bool> RemoveAllOld(string docNum)
        {
            try
            {
                foreach (DocLine dl in (await App.Database.GetSpecificDocsAsync(docNum)).Where(x => x.ItemQty != 0))
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
    }
}
