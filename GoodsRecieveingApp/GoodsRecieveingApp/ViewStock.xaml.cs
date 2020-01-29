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

namespace GoodsRecieveingApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ViewStock : ContentPage
    {
        string docCode ="";
        IMessage message = DependencyService.Get<IMessage>();
        public ViewStock(string dl)
        {
            InitializeComponent();
            docCode = dl;
            PopData();           
        }
        public async void PopData()
        {
            lstItems.ItemsSource = null;
            List<DocLine> lines = await App.Database.GetSpecificDocsAsync(docCode);
            List<DocLine> list = new List<DocLine>();
            foreach (DocLine dc in lines.Where(x=>x.ItemQty!=0))
            {
                dc.ScanAccQty = lines.Where(c=>c.ItemCode==dc.ItemCode).Sum(x=>x.ScanAccQty);
                dc.ScanRejQty = lines.Where(c => c.ItemCode == dc.ItemCode).Sum(x => x.ScanRejQty);
                dc.Balacnce = dc.ItemQty - (dc.ScanAccQty + dc.ScanRejQty);
                    if (dc.Balacnce == 0 && dc.ItemQty != 0)
                    {
                        dc.Complete = "Yes";
                    }
                    else if (dc.Balacnce == dc.ItemQty)
                    {
                        dc.Complete = "NotStarted";
                    }
                    else if (dc.Balacnce<0)
                    {
                        dc.Complete = "Wrong";
                    }
                    else
                    {
                        dc.Complete = "No";
                    }
                list.Add(dc);                
            }
            list.RemoveAll(x => x.ItemCode.Length < 2);
            lstItems.ItemsSource = list.OrderBy(x=>x.ItemDesc);
        }
        private async void LstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            DocLine dl=e.SelectedItem as DocLine;
            string output =await DisplayActionSheet("Confirm:-Reset QTY to zero?","YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    if (await restetQty(dl))
                    {
                        PopData();
                    }                                                            
                    break;
            }
        }
        public async Task<bool> restetQty(DocLine d)
        {
            List<DocLine> dls = await App.Database.GetSpecificDocsAsync(d.DocNum);
            foreach (DocLine docline in dls.Where(x => x.ItemCode == d.ItemCode && x.ItemQty == 0))
            {
                await App.Database.Delete(docline);
            }
            return true;
        }
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {
            if (await Check())
            {
                message.DisplayMessage("Complete!!!", true);
                //send GRV back to Pastel database
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please make sure all items are GREEN!", true);
            }
        }
        private async Task<bool> Check()
        {
            List<DocLine> lines = await App.Database.GetSpecificDocsAsync(docCode);
            foreach (DocLine dc in lines.Where(x => x.ItemQty != 0))
            {
                foreach (DocLine dl in lines.Where(x => x.ItemQty == 0))
                {
                    if (dc.ItemCode == dl.ItemCode)
                    {
                        dc.Balacnce += dl.ScanAccQty + dl.ScanRejQty;
                    }
                }
                if (dc.Balacnce != dc.ItemQty)
                {
                    return false;
                }
            }
            return true;
        }
        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            if (Navigation.NavigationStack.Count== 4)
            {
                await Navigation.PopAsync();
            }
            else
            {
                Navigation.RemovePage(Navigation.NavigationStack[3]);
                await Navigation.PopAsync();
            }
        }
    }
}