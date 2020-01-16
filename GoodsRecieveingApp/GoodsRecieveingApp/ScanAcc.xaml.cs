using Data.KeyboardContol;
using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace GoodsRecieveingApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanAcc : ContentPage
    {
        DocLine UsingDoc = new DocLine();
        List<DocLine> currentDocs;
        string lastItem;
        bool wrong;
        private ExtendedEntry _currententry;
        public ScanAcc(DocLine d)
        {
            InitializeComponent();
            txfAccCode.Focused += Entry_Focused;
            UsingDoc = d;
            lblMainAcc.Text = UsingDoc.SupplierName + " - " + UsingDoc.DocNum;
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            currentDocs = await App.Database.GetSpecificDocsAsync(UsingDoc.DocNum);
            txfAccCode.Focus();
        }
        private async void EntryAcc_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(500);
            //BOM Barcode
            if (txfAccCode.Text.Length > 0)
            {
                lblBarCode.Text =txfAccCode.Text;
                if (txfAccCode.Text.Length != 13)
                {
                    BOMItem bi;
                    try
                    {
                        bi = await App.Database.GetBOMItem(txfAccCode.Text);
                    }
                    catch
                    {
                        await DisplayAlert("Error!", "There is no item with this barcode", "OK");
                        txfAccCode.Text = "";
                        return;
                    }
                    if (Check(bi.ItemCode))
                    {
                        await App.Database.Insert(new DocLine { ItemCode = bi.ItemCode, ScanAccQty = bi.Qty, ScanRejQty = 0, DocNum = UsingDoc.DocNum, WarehouseID = "001", isRejected = false });
                        PicImage.IsVisible = true;
                        lastItem = bi.ItemCode;
                        SetQtyDisplay(lastItem);
                        txfAccCode.Text = "";
                    }
                    else
                    {
                        await DisplayAlert("Error!!", "There was no item on the PO with this barcode", "OK");
                        PicImage.IsVisible = true;
                        PicImage.ImageSource = "Wrong.png";
                        txfAccCode.Text = "";
                    }
                }
                //item barcode
                else if (txfAccCode.Text.Length == 13)
                {
                    if (CheckBarcode(txfAccCode.Text))
                    {
                        string iCode = currentDocs.Find(x => x.ItemBarcode == txfAccCode.Text && x.ItemQty != 0).ItemCode;
                        await App.Database.Insert(new DocLine { ItemCode = iCode, ScanAccQty = 1, ScanRejQty = 0, DocNum = UsingDoc.DocNum, WarehouseID = "002", isRejected = false });
                        PicImage.IsVisible = true;
                        lastItem = iCode;
                        SetQtyDisplay(iCode);
                        txfAccCode.Text = "";
                    }
                    else
                    {
                        await DisplayAlert("Error!!", "There was no item on the PO with this barcode", "OK");
                        PicImage.IsVisible = true;
                        PicImage.ImageSource = "Wrong.png";
                        txfAccCode.Text = "";
                    }
                }
            }

            txfAccCode.Focus();
        }
        public bool Check(string Icode)
        {
            foreach (DocLine dl in currentDocs.Where(x => x.ItemQty != 0))
            {
                if (Icode == dl.ItemCode)
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
            int scanQty=0;            
            int OrderQty = currentDocs.Find(x => x.ItemCode == Icode && x.ItemQty != 0).ItemQty;
            lblitemDescAcc.Text = currentDocs.Find(x => x.ItemCode == Icode && x.ItemQty != 0).ItemDesc+"\n"+txfAccCode.Text;
            int balance = OrderQty;
            foreach (DocLine dl in currentDocs.Where(x=>x.ItemCode== Icode && x.ItemQty!=0))
            {
                foreach(DocLine dc in currentDocs.Where(x => x.ItemCode == Icode && x.ItemQty == 0))
                {
                    scanQty +=dc.ScanAccQty+dc.ScanRejQty;
                    balance -= dc.ScanAccQty+dc.ScanRejQty;
                }
            }            
            lblBalance.Text = balance + "";
            lblScanQTY.Text = scanQty + "";
            lblOrderQTY.Text = OrderQty + "";
            if (balance==0)
            {
                PicImage.ImageSource = "Tick.png";
                wrong = false;
            }
            else if (balance != OrderQty&&balance>0)
            {
                PicImage.ImageSource = "PLus.png";
                wrong = false;
            }
            else if (scanQty > OrderQty|| balance < 0)
            {
                PicImage.ImageSource = "Wrong.png";
                wrong = true;
            }
            else
            {
                PicImage.ImageSource = "Wrong.png";
                wrong = true;
            }
            txfAccCode.Focus();
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
            lblBalance.Text =  "";
            lblScanQTY.Text =  "";
            lblOrderQTY.Text = "";
            lblitemDescAcc.Text = "";
            PicImage.IsVisible = false;            
            txfAccCode.Focus();
            return true;
        }
        private async void PicImage_Clicked(object sender, EventArgs e)
        {
            if (wrong)
            {
                string output = await DisplayActionSheet("Confirm:- Reset QTY to zero?", "YES", "NO");
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