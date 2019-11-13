using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ScannerFDB
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Password : Rg.Plugins.Popup.Pages.PopupPage
    {
        public Password()
        {
            InitializeComponent();
        }

        private async void TxfuserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txfuserName.Text.Equals("LIAM"))
            {
                await Navigation.PushAsync(new AdminPage());
                await PopupNavigation.Instance.PopAsync();
            }
            else
            {
                await DisplayAlert("Error!","Incorrect Code","Okay");
                await PopupNavigation.Instance.PopAsync();
            }
        }
        protected async override bool OnBackButtonPressed()
        {
            await PopupNavigation.Instance.PopAsync();
            return base.OnBackButtonPressed();
        }
    }
}