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
    public partial class UserInfoPopup : Rg.Plugins.Popup.Pages.PopupPage
    {
        public UserInfoPopup()
        {
            InitializeComponent();
            pickerLevel.Items.Add("Default User");
            pickerLevel.Items.Add("High Level User");
            pickerLevel.Items.Add("Super User");
        }

        private async void PickerLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (txfuserName.Text!=""&&txfuserName.Text!=null)
            {
                switch (pickerLevel.SelectedItem.ToString())
                {
                    case "Default User":
                        await GoodsRecieveingApp.App.Database.Insert(new User { UserName = txfuserName.Text.Trim(), useLevel =1});
                        break;
                    case "High Level User":
                        await GoodsRecieveingApp.App.Database.Insert(new User { UserName = txfuserName.Text.Trim(), useLevel =2});
                        break;
                    case "Super User":
                        await GoodsRecieveingApp.App.Database.Insert(new User { UserName = txfuserName.Text.Trim(), useLevel =3});
                        break;
                }               
            }
        }

        private void TxfuserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            pickerLevel.IsVisible = true;
        }
    }
}