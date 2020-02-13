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
    public partial class CountPage : ContentPage
    {
        private List<InventoryItem> items = new List<InventoryItem>();
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        private int countID = 0;
        public CountPage(int i)
        {
            InitializeComponent();
            countID = i;
            txfItemCode.Focused += Entry_Focused;
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            if (countID==0)
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please select a valid Count",true);
                await Navigation.PopAsync();
            }
            if(!await GetItems())
            {
                Vibration.Vibrate();
                message.DisplayMessage("Error in fetching data", true);
                await Navigation.PopAsync();
            }
            if(!RefreshList())
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not display items", true);
                await Navigation.PopAsync();
            }
            txfItemCode.Focus();
        }
        async Task<bool> GetItems()
        {
            if (Connectivity.NetworkAccess==NetworkAccess.Internet)
            {
                RestClient client = new RestClient();
                string path = "Inventory";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?CountIDNum={countID}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    cancellationTokenSource.Dispose();
                    if (res.IsSuccessful && res.Content.Contains("CountID"))
                    {
                        items.Clear();
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            InventoryItem i1 = new InventoryItem();
                            i1.CountID = countID;
                            i1.ItemDesc = row["ItemDesc"].ToString();
                            i1.BarCode = row["BarCode"].ToString();
                            i1.ItemCode = row["ItemCode"].ToString();
                            i1.isFirst = Convert.ToBoolean(row["isFirst"].ToString());
                            try
                            {
                                i1.FirstScanQty = Convert.ToInt32(row["FirstScanQty"].ToString());
                            }
                            catch
                            {
                                i1.FirstScanQty = 0;
                            }
                            try
                            {
                                i1.SecondScanQty = Convert.ToInt32(row["SecondScanQty"].ToString());
                            }
                            catch
                            {
                                i1.SecondScanQty = 0;
                            }
                            try
                            {
                                i1.CountUser = Convert.ToInt32(row["CountUser"].ToString()); ;
                            }
                            catch
                            {
                                i1.CountUser =0;
                            }
                            try
                            {
                                i1.SecondScanAuth = Convert.ToInt32(row["SecondScanAuth"].ToString()); ;
                            }
                            catch
                            {
                                i1.SecondScanAuth = 0;
                            }
                            items.Add(i1);
                        }
                        return true;
                    }
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please connect to the internet",true);
                return false;
            }
            return false;
        }
        bool RefreshList()
        {
            lstItems.ItemsSource = null;
            foreach (InventoryItem ite in items)
            {
                if (ite.Complete)
                {
                    ite.Status = "Complete";
                }else if (ite.ItemDesc==lblCurrentItem.Text)
                {
                    ite.Status = "Current";
                }else if (!ite.isFirst)
                {
                    ite.Status = "2";
                }
                else
                {
                    ite.Status = "1";
                }
            }
            lstItems.ItemsSource = items;
            return true;
        }
        void Setlbl(string desc)
        {
            lblLayout.IsVisible = true;
            lblCurrentItem.Text =items.Where(x=>x.ItemDesc==desc).First().ItemDesc+" : ";
            if (items.Where(x => x.ItemDesc == desc).First().isFirst)
            {
                lblLayout.BackgroundColor = Color.FromHex("#3F51B5");
                lblCurrentQty.Text = ""+items.Where(x => x.ItemDesc == desc).First().FirstScanQty;
            }
            else
            {
                lblLayout.BackgroundColor = Color.Orange;
                lblCurrentQty.Text = "" + items.Where(x => x.ItemDesc == desc).First().SecondScanQty;
            }
        }
        private async void txfItemCode_Completed(object sender, EventArgs e)
        {
            LoadingIndicator.IsVisible = true;
            if (txfItemCode.Text.Length>10)
            {               
                if (items.Where(x => x.BarCode == txfItemCode.Text) != null)
                {
                    if (items.Where(x => x.BarCode == txfItemCode.Text).First().isFirst)
                    {
                        items.Where(x => x.BarCode == txfItemCode.Text).Select(x => { x.FirstScanQty = x.FirstScanQty+1; return x; }).ToList();
                        Setlbl(items.Where(x => x.BarCode == txfItemCode.Text).First().ItemDesc);
                        if (!RefreshList())
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("Could Not Refresh The List", true);
                            txfItemCode.Text = "";
                            LoadingIndicator.IsVisible = false;
                            txfItemCode.Focus();
                            return;
                        }
                    }
                    else
                    {
                        items.Where(x => x.BarCode == txfItemCode.Text).Select(x => { x.SecondScanQty = x.SecondScanQty + 1; return x; }).ToList();
                        Setlbl(items.Where(x => x.BarCode == txfItemCode.Text).First().ItemDesc);
                        if (!RefreshList())
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("Could Not Refresh The List", true);
                            txfItemCode.Text = "";
                            LoadingIndicator.IsVisible = false;
                            txfItemCode.Focus();
                            return;
                        }
                    }                
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("No item on this order with this code", true);
                    txfItemCode.Text = "";
                    LoadingIndicator.IsVisible = false;
                    txfItemCode.Focus();
                    return;
                }
            }
            else if(txfItemCode.Text.Length>7)
            {
                BOMItem bi = new BOMItem();
                try
                {
                    bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfItemCode.Text);
                }
                catch
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("Error! No Item with this code", true);
                    txfItemCode.Text = "";
                    LoadingIndicator.IsVisible = false;
                    txfItemCode.Focus();
                    return;
                }
                if (items.Where(x=>x.ItemCode==bi.ItemCode)!=null)
                {
                    if (items.Where(x=>x.ItemCode==bi.ItemCode).First().isFirst)
                    {
                        items.Where(x => x.ItemCode == bi.ItemCode).Select(x => { x.FirstScanQty = x.FirstScanQty+bi.Qty; return x; }).ToList();
                        Setlbl(items.Where(x => x.ItemCode == bi.ItemCode).First().ItemDesc);
                        if (!RefreshList())
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("Could Not Refresh The List", true);
                            txfItemCode.Text = "";
                            LoadingIndicator.IsVisible = false;
                            txfItemCode.Focus();
                            return;
                        }
                    }
                    else
                    {
                        items.Where(x => x.ItemCode == bi.ItemCode).Select(x => { x.SecondScanQty = x.SecondScanQty+bi.Qty; return x; }).ToList();
                        Setlbl(items.Where(x => x.ItemCode == bi.ItemCode).First().ItemDesc);
                        if (!RefreshList())
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("Could Not Refresh The List", true);
                            txfItemCode.Text = "";
                            LoadingIndicator.IsVisible = false;
                            txfItemCode.Focus();
                            return;
                        }
                    }                 
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("No item on this order with this code", true);
                    txfItemCode.Text = "";
                    LoadingIndicator.IsVisible = false;
                    txfItemCode.Focus();
                    return;
                }
            }
            txfItemCode.Text = "";
            LoadingIndicator.IsVisible = false;
            txfItemCode.Focus();
        }       
        private async void lstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            InventoryItem dl = e.SelectedItem as InventoryItem;
            string output = await DisplayActionSheet("Reset Qty to zero for this item?", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                   
                    if(!await ResetInDB(dl.ItemDesc, dl.isFirst))
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("Could not reset in Database", true);
                        txfItemCode.Text = "";
                        LoadingIndicator.IsVisible = false;
                        txfItemCode.Focus();
                        return;
                    }
                    else
                    {
                        if (dl.isFirst)
                        {
                            items.Where(x => x.ItemDesc == dl.ItemDesc).Select(x => { x.FirstScanQty = 0; return x; }).ToList();
                        }
                        else
                        {
                            items.Where(x => x.ItemDesc == dl.ItemDesc).Select(x => { x.SecondScanQty = 0; return x; }).ToList();
                        }
                    }
                    break;
            }
            if (!RefreshList())
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not refresh list", true);
            }
            txfItemCode.Text = "";
            LoadingIndicator.IsVisible = false;
            txfItemCode.Focus();
        }
        private async Task<bool> ResetInDB(string desc, bool isfirst)
        {
            string scanQty = "";
            if (isfirst)
            {
                scanQty = "FirstScanQty";
            }
            else
            {
                scanQty = "SecondScanQty";
            }
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestClient client = new RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"POST?qry=UPDATE InventoryLines SET {scanQty}=0,Complete=False,SecondScanAuth=0 WHERE CountID={countID} AND ItemDesc='{desc}'";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    cancellationTokenSource.Dispose();
                    if (res.IsSuccessful && res.Content.Contains("Complete"))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }
            return false;
        }
        private void btnComplete_Clicked(object sender, EventArgs e)
        {
            //SendSQLToPastel
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
        private async void btnSave_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Checking Values....", true);
            if (await CheckWithSQL())
            {
                Navigation.RemovePage(Navigation.NavigationStack[2]);
                await Navigation.PopAsync();
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not connect to DB", false);                
            }
        }
        private async Task<bool> CheckWithSQL()
        {
            //Check item Qtys
            var ds = new DataSet();
            try
            {
                var t1 = new DataTable();
                DataRow row = null;
                t1.Columns.Add("CountID");
                t1.Columns.Add("BarCode");
                t1.Columns.Add("ItemDesc");
                t1.Columns.Add("FirstScanQty");
                t1.Columns.Add("SecondScanQty");
                t1.Columns.Add("SecondScanAuth");
                t1.Columns.Add("CountUser");
                t1.Columns.Add("isFirst");
                foreach (InventoryItem iut in items)
                {
                    row = t1.NewRow();
                    row["CountID"] = countID;
                    row["BarCode"] = iut.BarCode;                    
                    row["FirstScanQty"] = iut.FirstScanQty;                    
                    row["SecondScanQty"] = iut.SecondScanQty;                    
                    row["SecondScanAuth"] = iut.SecondScanAuth;                    
                    row["ItemDesc"] = iut.ItemDesc;                    
                    row["CountUser"] = iut.CountUser;                    
                    row["isFirst"] = iut.isFirst;                    
                    t1.Rows.Add(row);
                }
                ds.Tables.Add(t1);
            }
            catch (Exception)
            {
                return false;
            }
            string myds = Newtonsoft.Json.JsonConvert.SerializeObject(ds);
            RestSharp.RestClient client = new RestSharp.RestClient();
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath);
            {
                var Request = new RestSharp.RestRequest("Inventory", RestSharp.Method.POST);
                Request.RequestFormat = RestSharp.DataFormat.Json;
                Request.AddJsonBody(myds);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("Complete"))
                {
                    return true;
                }
            }
            return false;
        }
        private void addCompleteBtn()
        {
            Button btn1 = new Button 
            {
                Text = "Complete Inventory Count" ,
                BackgroundColor = Color.Green, 
                TextColor = Color.Black, 
                ImageSource = "TickSmall.png", 
                FontSize = 30
            };
            btn1.Clicked += btnComplete_Clicked;
            lstItems.Footer = btn1;
        }
    }
}