using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoodsRecieveingApp;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PickAndPack
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ViewItems : ContentPage
    {
        string docCode = "";
        public ViewItems(string dl)
        {
            InitializeComponent();
            docCode = dl;
            PopData();
        }
        public async void PopData()
        {
            lstItems.ItemsSource = null;
            List<DocLine> lines = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
            List<DocLine> list = new List<DocLine>();
            foreach (string s in lines.Select(x => x.ItemDesc).Distinct())
            {
                foreach (int i in lines.Where(x=>x.ItemDesc==s&&x.ItemQty==0).Select(x => x.PalletNum).Distinct())
                {
                    DocLine TDoc = lines.Where(x => x.ItemDesc == s&&x.PalletNum==i).First();
                    DocLine TempDoc = new DocLine() {PalletNum=TDoc.PalletNum,ItemDesc=TDoc.ItemDesc};
                    TempDoc.ScanAccQty = (lines.Where(x => x.ItemDesc == s && x.PalletNum == i).Sum(x => x.ScanAccQty));
                    TempDoc.ItemQty = (lines.Where(x => x.ItemDesc == s && x.ItemQty != 0).First().ItemQty);
                    TempDoc.Balacnce = TempDoc.ItemQty - (lines.Where(x => x.ItemDesc == s).Sum(x => x.ScanAccQty));
                    if (TempDoc.Balacnce == 0)
                    {
                        TempDoc.Complete = "Yes";
                    }
                    else if (TempDoc.Balacnce == TempDoc.ItemQty)
                    {
                        TempDoc.Complete = "NotStarted";
                    }
                    else
                    {
                        TempDoc.Complete = "No";
                    }
                    list.Add(TempDoc);
                }               
            }
            //list.RemoveAll(x => x.ItemCode.Length < 2);
            //lstItems.ItemsSource = list.OrderBy(x => new { x.ItemDesc , x.PalletNum } );
            lstItems.ItemsSource = list;
        }
        private async void LstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            DocLine dl = e.SelectedItem as DocLine;
            string output = await DisplayActionSheet("Confirm:-Reset QTY to zero?", "YES", "NO");
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
            List<DocLine> dls = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(d.DocNum);
            foreach (DocLine docline in dls.Where(x => x.ItemCode == d.ItemCode && x.ItemQty == 0))
            {
                await GoodsRecieveingApp.App.Database.Delete(docline);
            }
            return true;
        }
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {
            if (await Check())
            {
                await DisplayAlert("Complete!", "The recieving for this PO is complete", "OK");
                //send GRV back to Pastel database
            }
            else
            {
                await DisplayAlert("Errro!", "There is an error in the order, Please make sure all items are green", "OK");
            }
        }
        private async Task<bool> Check()
        {
            List<DocLine> lines = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
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
    }
}