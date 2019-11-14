using Data.KeyboardContol;
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
    public partial class AccessCheck : ContentPage
    {
        private ExtendedEntry _currententry;
        public AccessCheck()
        {
            InitializeComponent();
            txfUserBarcode.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfUserBarcode.Focus();
        }
        private void TxfUserBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txfUserBarcode.Text=="LIAM")
            {
                Navigation.PushAsync(new AdminPage(3));
            }
            else if (txfUserBarcode.Text == "PO101786")
            {
                Navigation.PushAsync(new AdminPage(2));
            }
            else
            {
                DisplayAlert("Error!","Sorry you do not have access to the admin page","Okay");
            }
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
    }
}