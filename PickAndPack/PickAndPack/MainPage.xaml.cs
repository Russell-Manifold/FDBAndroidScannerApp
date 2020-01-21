using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using GoodsRecieveingApp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace PickAndPack
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();           
        }

        private async void btnSingleOrder_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SingleOrder());
        }

        private async void btnSinglePallet_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SinglePallet());
        }

        private async void btnPickingSlips_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PickSlips());
        }
    }
}