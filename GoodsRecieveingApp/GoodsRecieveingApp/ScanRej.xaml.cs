using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace GoodsRecieveingApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanRej : ContentPage
    {
        DocLine UsingDoc = new DocLine();
        List<DocLine> currentDocs ;
        string lastItem;
        bool wrong;
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        public ScanRej(DocLine d)
        {
            InitializeComponent();
            txfRejCode.Focused += Entry_Focused;
            UsingDoc = d;
            lblMainRej.Text = UsingDoc.SupplierName + " - " + UsingDoc.DocNum;           
        }
        private void ButtonRej_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Rejected items added", true);
            Navigation.PopAsync();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            txfRejCode.Focus();
            currentDocs = await App.Database.GetSpecificDocsAsync(UsingDoc.DocNum);
        }
        private async void EntryRej_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(500);
            lblBarCode.Text = txfRejCode.Text;
            //BOM Barcode
            if (txfRejCode.Text.Length==14|| txfRejCode.Text.Length==8)
            {
                BOMItem bi;
                try
                {
                    bi = await App.Database.GetBOMItem(txfRejCode.Text);
                }
                catch
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("There is no item with this code on this order", true);
                    return;
                }
                if (Check(bi.ItemCode))
                {
                    await App.Database.Insert(new DocLine { ItemCode = bi.ItemCode, ScanRejQty = bi.Qty, ScanAccQty = 0, DocNum = UsingDoc.DocNum, WarehouseID = "001", isRejected = true });
                    PicImage.IsVisible = true;
                    lastItem = bi.ItemCode;
                    SetQtyDisplay(lastItem);
                    txfRejCode.Text = "";
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("There is no item on this PO with this code", true);
                    PicImage.IsVisible = true;
                    PicImage.ImageSource = "Wrong.png";
                }
            }
            //item barcode
            else if (txfRejCode.Text.Length == 13)
            {
                if (CheckBarcode(txfRejCode.Text))
                {
                    string iCode = currentDocs.Find(x => x.ItemBarcode == txfRejCode.Text && x.ItemQty != 0).ItemCode;
                    await App.Database.Insert(new DocLine {ItemCode=iCode,ScanRejQty=1, ScanAccQty = 0, DocNum=UsingDoc.DocNum,WarehouseID="002",isRejected=true});
                    PicImage.IsVisible = true;
                    lastItem = iCode;
                    SetQtyDisplay(iCode);
                    txfRejCode.Text = "";
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("There was no item found with this code", true);
                    PicImage.IsVisible = true;
                    PicImage.ImageSource = "Wrong.png";
                }
            }
            txfRejCode.Focus();
        }
        public bool Check(string ICode)
        {
          foreach (DocLine dl in currentDocs.Where(x => x.ItemQty != 0))
          {
                if (ICode == dl.ItemCode)
                {
                    return true;
                }
          }
          return false;
        }
        public bool CheckBarcode(string Barcode)
        {
            foreach (DocLine dl in currentDocs.Where(x => x.ItemQty != 0))
            {
                if (Barcode == dl.ItemBarcode)
                {
                    return true;
                }
            }
            return false;
        }     
        private async void SetQtyDisplay(string Icode)
        {
            currentDocs = await App.Database.GetSpecificDocsAsync(UsingDoc.DocNum);
            int scanQty = 0;
            int OrderQty = currentDocs.Find(x => x.ItemCode == Icode && x.ItemQty != 0).ItemQty;
            lblitemDescRej.Text = currentDocs.Find(x => x.ItemCode == Icode && x.ItemQty != 0).ItemDesc + "\n" + txfRejCode.Text;
            int balance = OrderQty;
            scanQty = currentDocs.Where(x => x.ItemCode == Icode).Sum(x => x.ScanAccQty) + currentDocs.Where(x => x.ItemCode == Icode).Sum(x => x.ScanRejQty);
            balance = balance - scanQty;
            lblBalance.Text = balance + "";
            lblScanQTY.Text = scanQty + "";
            lblOrderQTY.Text = OrderQty + "";
            if (balance == 0)
            {
                PicImage.ImageSource = "Tick.png";
                wrong = false;
            }
            else if (balance != OrderQty && balance > 0)
            {
                PicImage.ImageSource = "PLus.png";
                wrong = false;
            }
            else if (scanQty > OrderQty || balance < 0)
            {
                PicImage.ImageSource = "Wrong.png";
                wrong = true;
            }
            else
            {
                PicImage.ImageSource = "Wrong.png";
                wrong = true;
            }
            txfRejCode.Focus();
        }
        public async Task<bool> restetQty()
        {
            try
            {
                List<DocLine> dls = await App.Database.GetSpecificDocsAsync(UsingDoc.DocNum);
                foreach (DocLine docline in dls.Where(x => x.ItemCode == lastItem && x.ItemQty == 0))
                {
                    await App.Database.Delete(docline);
                }
            }
            catch
            {

            }
            lblBalance.Text = "";
            lblScanQTY.Text = "";
            lblOrderQTY.Text = "";
            lblitemDescRej.Text = "";
            PicImage.IsVisible = false;
            txfRejCode.Focus();
            return true;
        }
        private async void PicImage_Clicked(object sender, EventArgs e)
        {
            if (wrong)
            {
                string output = await DisplayActionSheet("Confirm:-Reset QTY to zero?", "YES", "NO");
                switch (output)
                {
                    case "NO":
                        break;
                    case "YES":
                        await restetQty();
                        break;
                }
            }
            else
            {
                await Navigation.PopAsync();
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
       private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ViewStock(UsingDoc.DocNum.ToUpper()));
        }
    }
}