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
        private ExtendedEntry _currententry;
        IMessage message = DependencyService.Get<IMessage>();
        bool Unpack = false;
        public MenuPage(bool isUnPacking)
        {
            InitializeComponent();
            Unpack = isUnPacking;
            txfScanPack.Focused += Entry_Focused;
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
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Unpack)
            {
                Mainlayout.IsVisible = false;
                IntoPackLayout.IsVisible = true;
                txfScanPack.Focus();
            }
            else
            {
                try
                {
                    lblBOMInfo.Text = "You have repacked " + MainPage.docLines.FindAll(x => x.ItemDesc != "1ItemFromMain").Sum(x => x.ItemQty) + " / " + +MainPage.docLines.Find(x => x.ItemDesc == "1ItemFromMain").ItemQty + " items";
                }
                catch
                {
                    lblBOMInfo.Text = "0/" + MainPage.docLines.Find(x => x.ItemDesc == "1ItemFromMain").ItemQty + " Repacked";
                }
            }
        }
        private async void BtnSingles_Clicked(object sender, EventArgs e)
        {           
            await Navigation.PushAsync(new Singles());
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
                Navigation.RemovePage(Navigation.NavigationStack[2]);
                await Navigation.PopAsync();
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
        private async void txfScanPack_Completed(object sender, EventArgs e)
        {
            if (txfScanPack.Text.Length > 1)
            {
                try
                {
                    BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfScanPack.Text);
                    string result=await DisplayActionSheet($"Pack {bi.Qty} : {bi.ItemDesc} into one pack","YES","NO");
                    if (result=="YES")
                    {
                        //Send Data To Pastel
                        message.DisplayMessage("COMPLETE!",false);                        
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        txfScanPack.Text = "";
                        txfScanPack.Focus();
                    }
                }
                catch
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("No pack code found", true);
                    txfScanPack.Text = "";
                    txfScanPack.Focus();
                }
            }
        }
        private async void txfNumberOfItem_Completed(object sender, EventArgs e)
        {
            if (txfNumberOfItem.Text.Length>0)
            {
                try
                {
                    int i =Convert.ToInt32(txfNumberOfItem.Text);
                    string Item = "F";
                    if (i>9)
                    {
                        Item +=txfNumberOfItem.Text;
                    }else
                    {
                        Item +="0"+txfNumberOfItem.Text;
                    }
                    Item += ""+MainPage.docLines.First().ItemBarcode.Substring(7,5);
                    try
                    {
                       BOMItem boi=  await GoodsRecieveingApp.App.Database.GetBOMItem(Item);
                        //send To Pastel
                        message.DisplayMessage("Complete!",true);
                        await Navigation.PopAsync();
                    }
                    catch
                    {
                        string result = await DisplayActionSheet("No Pack Found would you like to create a new packcode?","YES","NO");
                        if (result =="YES")
                        {
                            message.DisplayMessage("Linking Codes .....",false);
                            await Task.Delay (2000);
                            //Send To Pastel
                            message.DisplayMessage("Complete!",true);
                            await Navigation.PopAsync();
                        }
                        else
                        {
                            txfNumberOfItem.Text = "";
                            return;
                        }
                    }
                }
                catch 
                {
                    message.DisplayMessage("No BOM created!", true);
                }               
            }
        }
    }
}