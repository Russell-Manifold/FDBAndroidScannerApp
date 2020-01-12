using Data.KeyboardContol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Model;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Threading;
using System.Data;
using RestSharp;

namespace WHTransfer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OutPage : ContentPage
    {
        private string FromWH,ToWH,FromDate,RecDate;
        private async void Button_Clicked(object sender, EventArgs e)
        {
            FromDate = DatePickerFrom.Date.ToString("dd MMM yyyy");
            RecDate = DatePickerRec.Date.ToString("dd MMM yyyy");
            if (FromWH==null||ToWH==null||FromDate==null||RecDate==null||FromWH == "" || ToWH == "" || FromDate == "" || RecDate == "")
            {
                await DisplayAlert("Error!", "Please enter ALL the fields", "OK");
            }
            else
            {
                await GoodsRecieveingApp.App.Database.Insert(new IBTHeader { TrfDate = DateTime.Now.ToString("dd MMM yyyy"), FromWH = FromWH, ToWH = ToWH, FromDate = FromDate, RecDate = RecDate, Active = true });
                await DisplayAlert("Complete!", "Transfer Successfully Started", "OK");
                await Navigation.PushAsync(new OutItems());
            }
        }
        private void PickerFromWH_SelectedIndexChanged(object sender, EventArgs e)
        {
            FromWH = pickerFromWH.SelectedItem.ToString();
            pickerToWH.IsEnabled = true;
        }
        private async void BtnStartNewHeader_Clicked(object sender, EventArgs e)
        {
            try
            {
                await GoodsRecieveingApp.App.Database.DeleteAllHeaders();
            }
            catch
            {

            }
            LayoutHeader.IsVisible = false;
            StartNewLayout.IsVisible = true;
        }
        private async void BtnContinue_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OutItems());
        }
        private void DatePickerFrom_Unfocused(object sender, FocusEventArgs e)
        {
            lblDatePickFrom.Text = "Sending Date: " + DatePickerFrom.Date.ToString("dd MMM yyyy");
        }
        private void DatePickerRec_Unfocused(object sender, FocusEventArgs e)
        {
            lblDatePickRec.Text = "Receving Date: " + DatePickerRec.Date.ToString("dd MMM yyyy");
        }
        private async void PickerToWH_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (pickerToWH.SelectedIndex != -1)
            {
                ToWH = pickerToWH.SelectedItem.ToString();
                if (ToWH == FromWH)
                {
                    ToWH = "";
                    pickerToWH.SelectedIndex = -1;
                    pickerToWH.Focus();
                    await DisplayAlert("Error!", "You cannot transfer from and to the same warehouse", "Okay");
                }
                else
                {
                    btnAdd.IsEnabled = true;
                    DatePickerRec.IsEnabled = true;
                    DatePickerFrom.IsEnabled = true;
                }
            }
        }
        public OutPage()
        {
            InitializeComponent();
        }      
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                IBTHeader current = (await GoodsRecieveingApp.App.Database.GetIBTHeaders()).First();
                DatePickerFrom.Date = DateTime.Today;
                DatePickerRec.Date = DateTime.Today;
            }
            catch
            {
                LayoutHeader.IsVisible = false;
                StartNewLayout.IsVisible = true;
            }
        }
    }
}