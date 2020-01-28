using Data.KeyboardContol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ScannerFDB
{    
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

        public MainPage()
        {
            InitializeComponent();            
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.Title = "Hi "+GoodsRecieveingApp.MainPage.UserName;
            if (GoodsRecieveingApp.MainPage.AccessLevel > 1)
            {
                btnAdmin.IsEnabled = true;
            }
            else
            {
                btnAdmin.IsEnabled = false;
            }
            if (MainPage.fReceive == true)
            {btnReceive.IsVisible = true;}
            else{btnReceive.IsVisible = false;}

            if (MainPage.fRepack == true)
            { btnRepack.IsVisible = true; }
            else { btnRepack.IsVisible = false; }

            if (MainPage.fWhTrf == true)
            { btnWhTrf.IsVisible = true; }
            else { btnWhTrf.IsVisible = false; }

            if (MainPage.fInvCount == true)
            { BtnInvCount.IsVisible = true; }
            else { BtnInvCount.IsVisible = false; }

            if (MainPage.fPickPack == true)
            { btnPickPack.IsVisible = true; }
            else { btnPickPack.IsVisible = false; }
        }
        private void Button_Clicked_Goods_Receving(object sender, EventArgs e)
        {
            Navigation.PushAsync(new GoodsRecieveingApp.MainPage());
        }

        private async void Button_Clicked_Admin(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminPage(GoodsRecieveingApp.MainPage.AccessLevel));
        }

        private async void Button_Clicked_Repacking(object sender, EventArgs e)
        {
           await Navigation.PushAsync(new RepackagingMoblie.MainPage());
        }
        private async void Button_Clicked_WareHouseTransfer(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new WHTransfer.MainPage());
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PickAndPack.MainPage());
        }
    }
}
