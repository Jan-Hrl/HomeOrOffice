using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Resources;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

namespace HomeOrOffice
{
    public partial class MainWindow
    {
        private CultureInfo ci = null;
        private ResourceManager rm;

        private void SetLanguage()
        {
            rm = new ResourceManager("HomeOrOffice.Resources.String", Assembly.GetExecutingAssembly());
            String CurrentCulture = Thread.CurrentThread.CurrentUICulture.Name;
            switch (CurrentCulture)
            {
                case "de":
                case "de-DE":
                    ci = new CultureInfo("de");
                    break;
                case "en-EN":
                case "en-GB":
                    ci = new CultureInfo("en");
                    break;
                default:
                    ci = new CultureInfo("en-GB");
                    break;
            }
        }

        private void btSwitchLanguage(Object sender, RoutedEventArgs e)
        {
            CultureInfo compare = new CultureInfo((String)((Button)sender).Tag);
            if (Equals(compare, ci))
                return;
            ci = compare;
            SetMainText();
        }

        private void SetMainText()
        {
            if (rm == null)
            {
                SetLanguage();
            }
            btnExport.Content       = rm.GetString($"btnExportText", ci);
            btnDelete.Content       = rm.GetString($"btnDeleteText", ci);
            btnSubmit.Content       = rm.GetString($"btnSubmitText", ci);
            btnCancel.Content       = rm.GetString($"btnCancelText", ci);
            cbHome.Content          = rm.GetString($"cbHomeText", ci);
            cbOffice.Content        = rm.GetString($"cbOfficeText", ci);
            cbBusinessTrip.Content  = rm.GetString($"cbBusinessTripText", ci);

            if ((Boolean)btnErweitern.Tag != NichtErweitert)
            {
                btnErweitern.Content = rm.GetString($"btnErweiternLess", ci);
                lbHeader.Content = rm.GetString($"lbHeaderSelect", ci);
            }
            else
            {
                btnErweitern.Content = rm.GetString($"btnErweiternMore", ci);
                lbHeader.Content = rm.GetString($"lbHeaderWhere", ci);
            }
            if (lbRead?.Tag != null)
                WritelbRead((DBHandler.DBEntry)lbRead?.Tag);
        }

        private void WritelbRead(DBHandler.DBEntry DBEntry)
        {
            if (lbRead == null)
                return;

            lbRead.Content = "";
            switch (DBEntry)
            {
                case DBHandler.DBEntry.NoEntry:
                    lbRead.Content = rm.GetString($"lbReadNoEntry", ci);
                    lbRead.Background = Brushes.LightGray;
                    btnDelete.IsEnabled = false;
                    break;
                case DBHandler.DBEntry.AtHome:
                    lbRead.Content = String.Format("{0} {1}", rm.GetString($"lbReadAtHome", ci),(((DateTime)cDayPicker?.SelectedDate).ToString("dd.MM.yy"))); 
                    lbRead.Background = Brushes.LightSeaGreen;
                    btnDelete.IsEnabled = true;
                    break;
                case DBHandler.DBEntry.InTheOffice:
                    lbRead.Content = String.Format("{0} {1}", rm.GetString($"lbReadInTheOffice", ci), (((DateTime)cDayPicker?.SelectedDate).ToString("dd.MM.yy")));
                    lbRead.Background = new SolidColorBrush(Color.FromArgb(0xA0, 0xFF, 0x70, 0x00));;
                    btnDelete.IsEnabled = true;
                    break;
                case DBHandler.DBEntry.BuisnessTrip:
                    lbRead.Content = String.Format("{0} {1}", rm.GetString($"lbReadBusinessTrip", ci), (((DateTime)cDayPicker?.SelectedDate).ToString("dd.MM.yy")));
                    lbRead.Background = Brushes.BurlyWood;
                    btnDelete.IsEnabled = true;
                    break;
                case DBHandler.DBEntry.Error:
                default:
                    lbRead.Content = rm.GetString($"lbReadError", ci);
                    lbRead.Background = Brushes.OrangeRed;
                    btnDelete.IsEnabled = false;
                    break;
            }
            lbRead.Content = lbRead.Content.ToString().Replace("\\n", Environment.NewLine);
            lbRead.Tag = DBEntry;
        }

    }
}
