using Data.KeyboardContol;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

        private void Button_Clicked_Goods_Receving(object sender, EventArgs e)
        {
            Navigation.PushAsync(new GoodsRecieveingApp.MainPage());
        }

        private async void Button_Clicked_Admin(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AccessCheck());
        }

        private void Button_Clicked_Repacking(object sender, EventArgs e)
        {
            Navigation.PushAsync(new RepackagingMoblie.MainPage());
        }                
    }
}
