using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoodsRecieveingApp;
using Data.Model;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Data.Message;

namespace ScannerFDB
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigPage : ContentPage
    {
        bool isNew = false;
        DeviceConfig config = new DeviceConfig();
        IMessage message = DependencyService.Get<IMessage>();
        public ConfigPage()
        {
            InitializeComponent();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            config= await GoodsRecieveingApp.App.Database.GetConfig();
            if (config == null || config.ConnectionS == null)
            {
                isNew = true;
                config = new DeviceConfig();
            }
            txfAccWH.Text =config.DefaultAccWH;
            txfRejWH.Text =config.DefaultRejWH;
            ConnectionString.Text = config.ConnectionS;

        }
        private async void btnSave_Clicked(object sender, EventArgs e)
        {
            config.ConnectionS = ConnectionString.Text;
            config.DefaultAccWH = txfAccWH.Text;
            config.DefaultRejWH = txfRejWH.Text;
            if (isNew)
            {
                await GoodsRecieveingApp.App.Database.Insert(config);
            }
            else
            {
                await GoodsRecieveingApp.App.Database.Update(config);
            }
            message.DisplayMessage("Saved!",true);
            await Navigation.PopAsync();
        }
    }
}