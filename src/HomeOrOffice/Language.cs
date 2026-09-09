namespace HomeOrOffice
{

    #region using
    using System;
    using System.Windows;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using System.Resources;
    using System.Windows.Controls;
    using System.Windows.Media;
    #endregion

    internal partial class MainWindow
    {
        /// <summary>CultureInfo der App</summary>
        private CultureInfo ci = null;

        /// <summary>Resourcemanager für die sprachabhängige Strings</summary>
        private ResourceManager rm;

        /// <summary>
        /// Legt die Sprache fest, die beim Starten ausgewählt ist
        /// </summary>
        private void SetLanguage()
        {
            rm = new ResourceManager("HomeOrOffice.Resources.String", Assembly.GetExecutingAssembly());

            String cultureName = Thread.CurrentThread.CurrentUICulture.Name;
            if (cultureName.StartsWith("de"))
                ci = new CultureInfo("de");
            else
                ci = new CultureInfo("en");

        }

        /// <summary>
        /// Event das die Sprache wechselt
        /// </summary>
        /// <param name="sender">Das Objekt, das das Ereignis ausgelöst hat</param>
        /// <param name="e">Die RoutedEventArgs, die mit dem Event übergeben werden</param>
        private void btSwitchLanguage(Object sender, RoutedEventArgs e)
        {
            CultureInfo compare = new CultureInfo((String)((Button)sender).Tag);
            if (ci.Name == compare.Name)
                return;
            ci = compare;

            //Sichtbare Texte neu schreiben
            SetMainText();
        }

        /// <summary>
        /// Methode die alle sprachabhängige Texte des Main Dialog schreibt
        /// </summary>
        private void SetMainText()
        {
            if (rm == null)
            {
                SetLanguage();
            }

            btnExport.Content       = rm.GetString("btnExportText", ci);
            btnDelete.Content       = rm.GetString("btnDeleteText", ci);
            btnSubmit.Content       = rm.GetString("btnSubmitText", ci);
            btnCancel.Content       = rm.GetString("btnCancelText", ci);
            cbHome.Content          = rm.GetString("cbHomeText", ci);
            cbOffice.Content        = rm.GetString("cbOfficeText", ci);
            cbBusinessTrip.Content  = rm.GetString("cbBusinessTripText", ci);

            //button Text ändert sich, sicherstellen das Richter angezeigt wird
            if ((Dialog)btnErweitern.Tag == Dialog.IstErweitert)
            {
                btnErweitern.Content = rm.GetString("btnErweiternLess", ci);
                lbHeader.Content = rm.GetString("lbHeaderSelect", ci);
            }
            else if ((Dialog)btnErweitern.Tag == Dialog.IstNichtErweitert)
            {
                btnErweitern.Content = rm.GetString("btnErweiternMore", ci);
                lbHeader.Content = rm.GetString("lbHeaderWhere", ci);
            }

            WritelbRead(lastReadOrWritenEntry);
        }

        /// <summary>
        /// schreibt Text je nach dem was es für einen Eintrag für den 
        /// gewählten Tag gibt.
        /// </summary>
        /// <param name="dbEntry">Datenbankwert des jeweiligen Tages</param>
        private void WritelbRead(Int32 dbEntry)
        {
            //Diese Methdoe ist aktuell nur daüfr ausgelegt das nur ein Ort angewählt ist
            lbRead.Content = "";

            switch (dbEntry)
            {
                case (Int32)DBHandler.DBEntry.Error:
                    lbRead.Content = rm.GetString("lbReadError", ci);
                    lbRead.Background = Brushes.OrangeRed;
                    btnDelete.IsEnabled = false;
                    break;
                case (Int32)DBHandler.DBEntry.AtHome:
                    lbRead.Content = String.Format("{0} {1}", rm.GetString("lbReadAtHome", ci),(((DateTime)cDayPicker?.SelectedDate).ToString("dd.MM.yy"))); 
                    lbRead.Background = Brushes.LightSeaGreen;
                    btnDelete.IsEnabled = true;
                    break;
                case (Int32)DBHandler.DBEntry.InTheOffice:
                    lbRead.Content = String.Format("{0} {1}", rm.GetString("lbReadInTheOffice", ci), (((DateTime)cDayPicker?.SelectedDate).ToString("dd.MM.yy")));
                    lbRead.Background = new SolidColorBrush(Color.FromArgb(0xA0, 0xFF, 0x70, 0x00));
                    btnDelete.IsEnabled = true;
                    break;
                case (Int32)DBHandler.DBEntry.BusinessTrip:
                    lbRead.Content = String.Format("{0} {1}", rm.GetString("lbReadBusinessTrip", ci), (((DateTime)cDayPicker?.SelectedDate).ToString("dd.MM.yy")));
                    lbRead.Background = Brushes.BurlyWood;
                    btnDelete.IsEnabled = true;
                    break;
                case (Int32)DBHandler.DBEntry.NoEntry:
                default:
                    lbRead.Content = rm.GetString("lbReadNoEntry", ci);
                    lbRead.Background = Brushes.LightGray;
                    btnDelete.IsEnabled = false;
                    break;
            }

            lastReadOrWritenEntry = dbEntry;
        }

    }
}
