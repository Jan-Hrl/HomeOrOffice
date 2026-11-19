namespace HomeOrOffice
{

    #region using 
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Resources;
    using System.Globalization;
    using ClosedXML.Excel;
    #endregion

    /// <summary>
    /// Interaktionslogik für ExportKonfig.xaml
    /// </summary>
    internal partial class ExportKonfig : Window
    {
        
        #region variablen

        /// <summary>
        /// Das Objekt welche die Kommunikation mit der SQLite Datenbank übernimmt
        /// </summary>
        private readonly DBHandler dbHandler;

        /// <summary>Zeigt Fehler aus Benutzeraktionen in Dialogfenstern an.</summary>
        private FehlerMelder fm;

        #endregion variablen

        #region Konstruktor

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="ci">Die aktuelle Anwendungskultur.</param>
        /// <param name="dbH">Die Datenbankverbindung für den Export.</param>
        public ExportKonfig(CultureInfo ci, DBHandler dbH)
        {
            InitializeComponent();
            dbHandler = dbH;
            fm = new FehlerMelder(this);
            UpdateList();
            UpdateUI();
            lbNumbersHome.Content = HomeOrOffice.Resources.String.lbNumbersHomeText;
            lbNumbersNoEntry.Content = HomeOrOffice.Resources.String.lbNumbersNoEntryText;
            lbNumbersOffice.Content = HomeOrOffice.Resources.String.lbNumbersOfficeText;
            lbNumbersTrip.Content = HomeOrOffice.Resources.String.lbNumbersTripText;
            btnDeleteYear.Content = HomeOrOffice.Resources.String.btnDeleteYearText;
            btnSubmitExport.Content = HomeOrOffice.Resources.String.btnSubmitExportText;
        }

        #endregion Konstruktor

        #region Events

        /// <summary>
        /// Event nach betätigen des Export Button
        /// </summary>
        /// <param name="sender">Sernderinformationen</param>
        /// <param name="e">Eventinformationen</param>
        private void BtnSubmitExport_Click(Object sender, RoutedEventArgs e)
        {
            try
            {
                if (lbYears.SelectedItem == null)
                    return;

                String filePath = getExcelPath();
                if (filePath == null) 
                    return;

                if (!filePath.EndsWith(".xlsx"))
                    filePath += ".xlsx";
                
                XLWorkbook workbook = new XLWorkbook();
                SetExcelHeader(workbook);
                SetExcelEntries(workbook);

                workbook.SaveAs(filePath);
                //Seit .NET Core ist UseShellExecute standardmäßig false; ohne Shell kann die .xlsx nicht mit Excel geöffnet werden
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        /// <summary>
        /// Event das ausgelöst wird durch anwahl eines Jahres
        /// </summary>
        /// <param name="sender">Sernderinformationen</param>
        /// <param name="e">Eventinformationen</param>
        private void LbYears_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            UpdateUI();
        }

        /// <summary>
        /// Event das ausgelöst wird durch anklicken des löschen Buttons
        /// </summary>
        /// <param name="sender">Sernderinformationen</param>
        /// <param name="e">Eventinformationen
        /// </param>
        private void BtnDeleteYear_Click(Object sender, RoutedEventArgs e)
        {
            if (lbYears.SelectedItem == null)
                return;

            String selectedYear = lbYears.SelectedItem.ToString();
            if ((MessageBoxResult.No == MessageBox.Show(String.Format(HomeOrOffice.Resources.String.SindSieSicherDasSieDieDatenbankLöschenMöchten, selectedYear), HomeOrOffice.Resources.String.btnDeleteYearText, MessageBoxButton.YesNo)))
                return;

            fm.VersucheAuszuführen(()=>dbHandler.DeleteTable(selectedYear));
                        
            UpdateList();
            UpdateUI();
        }

        #endregion Events

        #region Methode

        /// <summary>
        /// aktuallisiert die Liste, der verfügbaren Jahre
        /// </summary>
        private void UpdateList()
        {
            lbYears.Items.Clear();
            
            List<String> everyTable = null;
            everyTable = fm.VersucheAuszuführen(() => dbHandler.GetEveryTable(), null);
            if (everyTable != null)
            {
                for (Int32 i = 0; i < everyTable.Count; i++)
                {
                    lbYears.Items.Add(everyTable[i]);
                }
                if (lbYears.Items.Count > 0 )
                    lbYears.SelectedItem = everyTable[everyTable.Count - 1];
            }
        }

        /// <summary>
        /// Aktuallisiert das UI mit den Informationen des ausgewählten Jahres
        /// </summary>
        private void UpdateUI()
        {
            //Ohne ausgewähltes Jahr (z. B. nachdem das letzte Jahr gelöscht wurde) gibt es nichts zu löschen oder zu exportieren
            Boolean yearSelected = lbYears.SelectedItem != null;
            btnDeleteYear.IsEnabled = yearSelected;
            btnSubmitExport.IsEnabled = yearSelected;

            //Alte Zahlen entfernen, damit keine Werte eines gelöschten oder anderen Jahres stehen bleiben
            tbNmbHome.Text = String.Empty;
            tbNmbOffice.Text = String.Empty;
            tbNmbTrip.Text = String.Empty;
            tbNmbDayOff.Text = String.Empty;

            if (!yearSelected)
                return;

            Int32 selectedYear = Convert.ToInt32(lbYears.SelectedItem);
            if (!TryCalculateYearStatistics(selectedYear, out Int32 countDayOff, out Int32 countOfficeDay, out Int32 countHomeDay, out Int32 countBusinessTrip))
                return;

            tbNmbHome.Text = countHomeDay.ToString();
            tbNmbOffice.Text = countOfficeDay.ToString();
            tbNmbTrip.Text = countBusinessTrip.ToString();
            tbNmbDayOff.Text = countDayOff.ToString();
        }

        /// <summary>
        /// Berechnet die Arbeitsort-Statistik für ein Jahr.
        /// </summary>
        /// <param name="year">Das auszuwertende Jahr.</param>
        /// <param name="countDayOff">Anzahl der Werktage ohne Eintrag.</param>
        /// <param name="countOfficeDay">Anzahl der Bürotage.</param>
        /// <param name="countHomeDay">Anzahl der Homeoffice-Tage.</param>
        /// <param name="countBusinessTrip">Anzahl der Geschäftsreisetage.</param>
        /// <returns><c>true</c>, wenn die Statistik vollständig berechnet werden konnte.</returns>
        private Boolean TryCalculateYearStatistics(Int32 year, out Int32 countDayOff, out Int32 countOfficeDay, out Int32 countHomeDay, out Int32 countBusinessTrip)
        {
            String table = year.ToString();
            countOfficeDay      = fm.VersucheAuszuführen(() => dbHandler.CountDays(DBHandler.DBEntry.InTheOffice, table), -1);
            countHomeDay        = fm.VersucheAuszuführen(() => dbHandler.CountDays(DBHandler.DBEntry.AtHome, table), -1);
            countBusinessTrip   = fm.VersucheAuszuführen(() => dbHandler.CountDays(DBHandler.DBEntry.BusinessTrip, table), -1);
            Int32 countDaysWithEntry = countOfficeDay + countHomeDay + countBusinessTrip;

            if (countOfficeDay < 0 || countHomeDay < 0 || countBusinessTrip < 0)
            {
                countDayOff = 0;
                return false;
            }

            countDayOff = CountWorkdays(year) - countDaysWithEntry;
            return true;
        }

        /// <summary>Zählt die Werktage eines Jahres bis einschließlich heute.</summary>
        private Int32 CountWorkdays(Int32 year)
        {
            DateTime lastDay = year == DateTime.Now.Year ? DateTime.Today : new DateTime(year, 12, 31);
            Int32 workdayCount = 0;

            for (DateTime day = new DateTime(year, 1, 1); day <= lastDay; day = day.AddDays(1))
            {
                if (day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday)
                {
                    workdayCount++;
                }
            }

            return workdayCount;
        }

        /// <summary>
        /// Beschreibt und Formatiert den Header des exportierten Excel Dokument
        /// </summary>
        /// <param name="wb">Das Workbook des Excel exports</param>
        private void SetExcelHeader(XLWorkbook wb)
        {
            if (lbYears.SelectedItem == null)
                return;

            IXLWorksheet ws = wb.Worksheets.Add(lbYears.SelectedItem.ToString());

            IXLColumns columns = ws.Columns(1, 4);
            columns.Width = 15.0;
            columns.Style.Font.FontSize = 12;
            columns.Style.Border.RightBorder = XLBorderStyleValues.Thin;

            ws.Range("A1:D1").Merge();
            ws.Cell(1, 1).Value = lbYears.SelectedItem.ToString();
            ws.Cell(1, 1).Style.Font.FontSize = 30;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cells("A1:D1").Style.Border.BottomBorder = XLBorderStyleValues.Thick;

            ws.Cells("A2:D2").Style.Font.FontSize = 15;
            ws.Cells("A2:D2").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
            ws.Cells("A2:D2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("B2").Style.Border.LeftBorder = XLBorderStyleValues.Medium;
            ws.Cell("C2").Style.Border.LeftBorder = XLBorderStyleValues.Medium;
            ws.Cell("D2").Style.Border.LeftBorder = XLBorderStyleValues.Medium;

            ws.Cell(2, 1).Value = "Date";
            ws.Cell(2, 2).Value = "Homeoffice";
            ws.Cell(2, 3).Value = "Office";
            ws.Cell(2, 4).Value = "Business Trip";
        }

        /// <summary>
        /// Beschreibt die einzelnen Tage für das Excel Dokument
        /// </summary>
        /// <param name="wb">Das Workbook des Excel exports</param>
        private void SetExcelEntries(XLWorkbook wb)
        {
            IXLWorksheet ws = wb.Worksheets.Worksheet(lbYears.SelectedItem.ToString());
            DateTime EveryDateOfYear = new DateTime(Convert.ToInt32(lbYears.SelectedItem), 1, 1);
            DateTime LastDateOfYear = new DateTime(Convert.ToInt32(lbYears.SelectedItem), 12, 31);
            for (int i = 0; i < LastDateOfYear.DayOfYear; i++)
            {
                IXLCell DateCell = ws.Cell(i + 3, 1);
                DateCell.Value = EveryDateOfYear.ToString("dd.MM.yy") + " " + EveryDateOfYear.DayOfWeek.ToString()[0] + EveryDateOfYear.DayOfWeek.ToString()[1];


                if (EveryDateOfYear.DayOfWeek == DayOfWeek.Saturday || EveryDateOfYear.DayOfWeek == DayOfWeek.Sunday)
                {
                    ws.Cell(i + 3, 2).Value = "We";
                    ws.Cell(i + 3, 2).Style.Fill.BackgroundColor = XLColor.Gray;
                    ws.Cell(i + 3, 3).Value = "We";
                    ws.Cell(i + 3, 3).Style.Fill.BackgroundColor = XLColor.Gray;
                    ws.Cell(i + 3, 4).Value = "We";
                    ws.Cell(i + 3, 4).Style.Fill.BackgroundColor = XLColor.Gray;
                }
                else
                {
                    DBHandler.DBEntry dbEntry = getDate(EveryDateOfYear);
                    switch (dbEntry)
                    {
                        case DBHandler.DBEntry.Error:
                            break;                                              //TODO: Fehlermeldung hinzufügen
                        case DBHandler.DBEntry.NoEntry:                         //Wenn no Entry ist, kann es nichts anderes mehr sein, daher continue
                            DateCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                            break;
                        default:
                            IXLCell cell = null;
                            if ((dbEntry & DBHandler.DBEntry.InTheOffice) == DBHandler.DBEntry.InTheOffice)
                                cell = ws.Cell(i + 3, 3);
                            if ((dbEntry & DBHandler.DBEntry.AtHome) == DBHandler.DBEntry.AtHome)
                                cell = ws.Cell(i + 3, 2);
                            if ((dbEntry & DBHandler.DBEntry.BusinessTrip) == DBHandler.DBEntry.BusinessTrip)
                                cell = ws.Cell(i + 3, 4);

                            if (cell != null)
                            {
                                cell.Value = "X";
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            }
                            break;
                    }

                }
                EveryDateOfYear = EveryDateOfYear.AddDays(1);
            }
        }

        /// <summary>
        /// Liest den Eintrag eines Tages und behandelt Fehler durch einen Dialog.
        /// </summary>
        /// <param name="SelectedDate">Der auszulesende Tag.</param>
        /// <returns>Der gespeicherte Datenbankeintrag oder <see cref="DBHandler.DBEntry.Error"/> bei einem Fehler.</returns>
        private DBHandler.DBEntry getDate(DateTime SelectedDate)
        {
            try
            {
                return dbHandler.GetDate(SelectedDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return DBHandler.DBEntry.Error;
            }
        }

        /// <summary>
        /// Öffnet einen Speichern-Dialog und ermittelt den Zielpfad für den Excel-Export.
        /// </summary>
        /// <returns>Der ausgewählte Dateipfad oder <c>null</c>, wenn der Dialog abgebrochen wurde.</returns>
        private String getExcelPath()
        {
            Microsoft.Win32.SaveFileDialog SFDlg = new Microsoft.Win32.SaveFileDialog();
            SFDlg.FileName = lbYears.SelectedItem.ToString();
            SFDlg.DefaultExt = ".xlsx";
            SFDlg.Filter = "Excel files (*.xlsx)|*.xlsx";
            if (SFDlg.ShowDialog() != true)
                return null;
            return SFDlg.FileName;
        }

        #endregion Methode

    }
}
