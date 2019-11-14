using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RepackagingMoblie
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MenuPage : ContentPage
    {
        public MenuPage()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            lblBOMInfo.Text = "You have accounted for "+ MainPage.docLines.Find(x => x.ItemDesc == "1ItemFromMain").ItemQty+ " items in the BOM of "+ MainPage.docLines.FindAll(x => x.ItemDesc != "1ItemFromMain").Sum(x => x.ItemQty) + " items";
        }
        private async void BtnSingles_Clicked(object sender, EventArgs e)
        {
            string output = await DisplayActionSheet("Would you like to scan the same barcode or different?","Same","Different");
            switch (output)
            {
                case "Same":
                    await Navigation.PushAsync(new Singles());
                    break;
                case "Different":
                    await Navigation.PushAsync(new Singles());
                    break;
            }
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
                await DisplayAlert("Done!", "Your repacking has been completed", "Okay");
            }
            else
            {
                await DisplayAlert("Error!", "Not all items have been accounted for in the BOM", "Okay");
            }
        }
    }
}