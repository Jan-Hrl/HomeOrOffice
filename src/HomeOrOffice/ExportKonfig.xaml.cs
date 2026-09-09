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
        /// <summary>
        /// Das Objekt welche die Kommunikation mit der SQLite Datenbank übernimmt
        /// </summary>
        private readonly DBHandler dBHandler;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="rm">Resourcemanager</param>
        /// <param name="ci">CultureInfo</param>
        /// <param name="dbH">Kommunikation mit SQLite</param>
        public ExportKonfig(ResourceManager rm, CultureInfo ci, DBHandler dbH)
        {
            InitializeComponent();
            dBHandler = dbH;
            UpdateList();
            UpdateUI();
            lbNumbersHome.Content = rm.GetString("lbNumbersHomeText", ci);
            lbNumbersNoEntry.Content = rm.GetString("lbNumbersNoEntryText", ci);
            lbNumbersOffice.Content = rm.GetString("lbNumbersOfficeText", ci);
            lbNumbersTrip.Content = rm.GetString("lbNumbersTripText", ci);
            btnDeleteYear.Content = rm.GetString("btnDeleteYearText", ci);
            btnSubmitExport.Content = rm.GetString("btnSubmitExportText", ci);
        }

        /// <summary>
        /// aktuallisiert die Liste, der verfügbaren Jahre
        /// </summary>
        private void UpdateList()
        {
            lbYears.Items.Clear();
            List<String> everyTable = dBHandler.GetEveryTable();
            for (Int32 i = 0; i < everyTable.Count; i++)
            {
                lbYears.Items.Add(everyTable[i]);
            }
            if (lbYears.Items.Count > 0 )
                lbYears.SelectedItem = everyTable[everyTable.Count - 1];
        }

        /// <summary>
        /// Aktuallisiert das UI mit den Informationen des ausgewählten Jahres
        /// </summary>
        private void UpdateUI()
        {
            if (lbYears.SelectedItem == null)
                return;

            DateTime EveryDateOfYear = new DateTime(Convert.ToInt32(lbYears.SelectedItem), 1, 1);
            Int32 CountDayOff = 0,CountOfficeday = 0, CountHomeday = 0, CountBusinessTrip = 0;
            do
            {
                if (EveryDateOfYear.DayOfWeek != DayOfWeek.Saturday && EveryDateOfYear.DayOfWeek != DayOfWeek.Sunday)
                {
                    Int32 dbEntry = dBHandler.GetDate(EveryDateOfYear);
                    switch (dbEntry)
                    {
                        case (Int32)DBHandler.DBEntry.Error:
                            break;                                              //TODO: Fehlermeldung hinzufügen
                        case (Int32)DBHandler.DBEntry.NoEntry:                  //Wenn no Entry ist, kann es nichts anderes mehr sein, daher continue
                            CountDayOff++;
                            break;
                        default:
                            if ((dbEntry & (Int32)DBHandler.DBEntry.InTheOffice) == (Int32)DBHandler.DBEntry.InTheOffice)
                                CountOfficeday++;
                            if ((dbEntry & (Int32)DBHandler.DBEntry.AtHome) == (Int32)DBHandler.DBEntry.AtHome)
                                CountHomeday++;
                            if ((dbEntry & (Int32)DBHandler.DBEntry.BusinessTrip) == (Int32)DBHandler.DBEntry.BusinessTrip)
                                CountBusinessTrip++;
                            break;
                    }

                }

                if (Convert.ToInt32(lbYears.SelectedItem) == DateTime.Now.Year && EveryDateOfYear.DayOfYear >= DateTime.Now.DayOfYear)
                {
                    break;
                }

                EveryDateOfYear = EveryDateOfYear.AddDays(1);
            } while (EveryDateOfYear.Year == Convert.ToInt32(lbYears.SelectedItem));

            tbNmbHome.Text = CountHomeday.ToString(); 
            tbNmbOffice.Text = CountOfficeday.ToString();
            tbNmbTrip.Text = CountBusinessTrip.ToString();
            tbNmbDayOff.Text = CountDayOff.ToString();
        }

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

                Microsoft.Win32.SaveFileDialog SFDlg = new Microsoft.Win32.SaveFileDialog();
                SFDlg.FileName = lbYears.SelectedItem.ToString();
                SFDlg.DefaultExt = ".xlsx";
                SFDlg.Filter = "Excel files (*.xlsx)|*.xlsx";
                if (SFDlg.ShowDialog() != true)
                    return;
                string filePath = SFDlg.FileName;
            
                XLWorkbook workbook = new XLWorkbook();

                SetExcelHeader(workbook);
                SetExcelEntries(workbook);

                if (!filePath.EndsWith(".xlsx"))
                    filePath += ".xlsx";
                workbook.SaveAs(filePath);
                Process.Start(filePath);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
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
                    Int32 dbEntry = dBHandler.GetDate(EveryDateOfYear);
                    switch (dbEntry)
                    {
                        case (Int32)DBHandler.DBEntry.Error:
                            break;                                              //TODO: Fehlermeldung hinzufügen
                        case (Int32)DBHandler.DBEntry.NoEntry:                  //Wenn no Entry ist, kann es nichts anderes mehr sein, daher continue
                            DateCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                            break;
                        default:
                            IXLCell cell = null;
                            if ((dbEntry & (Int32)DBHandler.DBEntry.InTheOffice) == (Int32)DBHandler.DBEntry.InTheOffice)
                            {
                                cell = ws.Cell(i + 3, 3);
                                cell.Value = "X";
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            }
                            if ((dbEntry & (Int32)DBHandler.DBEntry.AtHome) == (Int32)DBHandler.DBEntry.AtHome)
                            {
                                cell = ws.Cell(i + 3, 2);
                                cell.Value = "X";
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            }
                            if ((dbEntry & (Int32)DBHandler.DBEntry.BusinessTrip) == (Int32)DBHandler.DBEntry.BusinessTrip)
                            {
                                cell = ws.Cell(i + 3, 4);
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
            if ((MessageBoxResult.No == MessageBox.Show($"Are you sure you want to delete the entire Database of {lbYears.SelectedItem}?", "Delete Year", MessageBoxButton.YesNo)))
                return;
            else
                dBHandler.DeleteTable(lbYears.SelectedItem.ToString());

            UpdateList();
            UpdateUI();
        }
    }
}
