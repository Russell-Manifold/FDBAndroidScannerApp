using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RepackagingMoblie
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MenuPage : ContentPage
    {
        IMessage message = DependencyService.Get<IMessage>();
        public MenuPage()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                lblBOMInfo.Text = "You have repacked " +MainPage.docLines.FindAll(x => x.ItemDesc != "1ItemFromMain").Sum(x => x.ItemQty) + " / " + +MainPage.docLines.Find(x => x.ItemDesc == "1ItemFromMain").ItemQty + " items";
            }
            catch
            {
                lblBOMInfo.Text = "0/" + MainPage.docLines.Find(x => x.ItemDesc == "1ItemFromMain").ItemQty + " Repacked";
            }
        }
        private async void BtnSingles_Clicked(object sender, EventArgs e)
        {
            //string output = await DisplayActionSheet("Would you like to scan the same barcode or different?","Same","Different");
            //switch (output)
            //{
            //    case "Same":
                    await Navigation.PushAsync(new Singles());
                //    break;
                //case "Different":
                //    await Navigation.PushAsync(new Singles());
                //    break;
            //}
        }

        private async void BtnCustom_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Custom());
            
        } 
        private async void BtnDamaged_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DamagedGoods());
        }
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {
            if (MainPage.docLines.Find(x=>x.ItemDesc=="1ItemFromMain").ItemQty==MainPage.docLines.FindAll(x=>x.ItemDesc!= "1ItemFromMain").Sum(x=>x.ItemQty))
            {
                // send all to API for printing MainPage.PackCodes
                lblBOMInfo.TextColor = System.Drawing.Color.Gray;
                message.DisplayMessage("Repacking Complete!!", true);
                await Navigation.PopToRootAsync();
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Not All items have benn packed", true);
            }
        }

        private async void Button_Clicked_Home(object sender, EventArgs e)
        {
            string output = await DisplayActionSheet("Exit before the repacking is complete", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    Navigation.RemovePage(Navigation.NavigationStack[2]);
                    await Navigation.PopAsync();
                    break;
            }           
        }
        private async void Clear_Clicked(object sender, EventArgs e)
        {
            string output = await DisplayActionSheet("Confirm:- Clear repacking quantities scanned", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    MainPage.docLines.Clear();
                    await Navigation.PopAsync();
                    break;
            }
        }
    }
}