using Data.KeyboardContol;
using Rg.Plugins.Popup.Services;
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
        public MainPage()
        {
            InitializeComponent();            
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.Title = "Hi "+GoodsRecieveingApp.MainPage.UserName;
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
