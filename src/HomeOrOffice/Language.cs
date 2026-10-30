namespace HomeOrOffice
{
    #region using
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    #endregion

    internal partial class MainWindow
    {
        /// <summary>CultureInfo der App</summary>
        private CultureInfo ci = null;

        /// <summary>
        /// Legt die Sprache fest, die beim Starten ausgewählt ist.
        /// </summary>
        private void SetLanguage()
        {
            String cultureName = Thread.CurrentThread.CurrentUICulture.Name;
            ci = cultureName.StartsWith("de", StringComparison.OrdinalIgnoreCase)
                ? new CultureInfo("de")
                : new CultureInfo("en");

            HomeOrOffice.Resources.String.Culture = ci;
        }

        /// <summary>
        /// Event das die Sprache wechselt
        /// </summary>
        private void btSwitchLanguage(Object sender, RoutedEventArgs e)
        {
            CultureInfo compare = new CultureInfo((String)((Button)sender).Tag);
            if (ci.Name == compare.Name)
                return;

            ci = compare;
            Thread.CurrentThread.CurrentUICulture = ci;
            HomeOrOffice.Resources.String.Culture = ci;
            SetMainText();
        }

        /// <summary>
        /// Methode die alle sprachabhängige Texte des Main Dialog schreibt
        /// </summary>
        private void SetMainText()
        {
            if (ci == null)
                SetLanguage();

            tbExport.Text = HomeOrOffice.Resources.String.btnExportText;
            btnDelete.Content = HomeOrOffice.Resources.String.btnDeleteText;
            btnSubmit.Content = HomeOrOffice.Resources.String.btnSubmitText;
            btnCancel.Content = HomeOrOffice.Resources.String.btnCancelText;
            cbHome.Content = HomeOrOffice.Resources.String.cbHomeText;
            cbOffice.Content = HomeOrOffice.Resources.String.cbOfficeText;
            cbBusinessTrip.Content = HomeOrOffice.Resources.String.cbBusinessTripText;

            if ((Dialog)btnErweitern.Tag == Dialog.IstErweitert)
            {
                btnErweitern.Content = HomeOrOffice.Resources.String.btnErweiternLess;
                lbHeader.Content = HomeOrOffice.Resources.String.lbHeaderSelect;
            }
            else if ((Dialog)btnErweitern.Tag == Dialog.IstNichtErweitert)
            {
                btnErweitern.Content = HomeOrOffice.Resources.String.btnErweiternMore;
                lbHeader.Content = HomeOrOffice.Resources.String.lbHeaderWhere;
            }

            WritelbRead(lastReadOrWritenEntry);
        }

        /// <summary>
        /// Schreibt den Text abhängig vom Eintrag des gewählten Tages.
        /// </summary>
        private void WritelbRead(Int32 dbEntry)
        {
            lbRead.Content = "";

            switch (dbEntry)
            {
                case (Int32)DBHandler.DBEntry.Error:
                    lbRead.Content = HomeOrOffice.Resources.String.lbReadError;
                    lbRead.Background = Brushes.OrangeRed;
                    btnDelete.IsEnabled = false;
                    break;
                case (Int32)DBHandler.DBEntry.AtHome:
                    lbRead.Content = String.Format("{0} {1}", HomeOrOffice.Resources.String.lbReadAtHome, ((DateTime)cDayPicker.SelectedDate).ToString("dd.MM.yy"));
                    lbRead.Background = Brushes.LightSeaGreen;
                    btnDelete.IsEnabled = true;
                    break;
                case (Int32)DBHandler.DBEntry.InTheOffice:
                    lbRead.Content = String.Format("{0} {1}", HomeOrOffice.Resources.String.lbReadInTheOffice, ((DateTime)cDayPicker.SelectedDate).ToString("dd.MM.yy"));
                    lbRead.Background = new SolidColorBrush(Color.FromArgb(0xA0, 0xFF, 0x70, 0x00));
                    btnDelete.IsEnabled = true;
                    break;
                case (Int32)DBHandler.DBEntry.BusinessTrip:
                    lbRead.Content = String.Format("{0} {1}", HomeOrOffice.Resources.String.lbReadBusinessTrip, ((DateTime)cDayPicker.SelectedDate).ToString("dd.MM.yy"));
                    lbRead.Background = Brushes.BurlyWood;
                    btnDelete.IsEnabled = true;
                    break;
                case (Int32)DBHandler.DBEntry.NoEntry:
                default:
                    lbRead.Content = HomeOrOffice.Resources.String.lbReadNoEntry;
                    lbRead.Background = Brushes.LightGray;
                    btnDelete.IsEnabled = false;
                    break;
            }

            lastReadOrWritenEntry = dbEntry;
        }
    }
}