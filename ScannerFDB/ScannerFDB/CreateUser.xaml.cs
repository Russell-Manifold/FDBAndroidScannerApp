using Data.Model;
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
    public partial class CreateUser : ContentPage
    {
        public CreateUser()
        {
            InitializeComponent();            
        }

        private async void BtnCreateUser_Clicked(object sender, EventArgs e)
        {
            if(txfUserName.Text!=null&&txfLevel.Text!=null){
                switch (txfLevel.Text)
                {
                    case "1":
                        await GoodsRecieveingApp.App.Database.Insert(new User{UserName=txfUserName.Text,useLevel=1});
                        await DisplayAlert("Complete", "The User has been added", "Okay");
                        await Navigation.PopAsync();
                        break;
                    case "2":
                        await GoodsRecieveingApp.App.Database.Insert(new User { UserName = txfUserName.Text, useLevel = 2 });
                        await DisplayAlert("Complete", "The User has been added", "Okay");
                        await Navigation.PopAsync();
                        break;
                    case "3":
                        await GoodsRecieveingApp.App.Database.Insert(new User { UserName = txfUserName.Text, useLevel = 3 });
                        await DisplayAlert("Complete", "The User has been added", "Okay");
                        await Navigation.PopAsync();
                        break;
                    default:
                        await DisplayAlert("Error","The access level you have entered is invalid","Okay");
                        break;
                }
            }
        }
    }
}