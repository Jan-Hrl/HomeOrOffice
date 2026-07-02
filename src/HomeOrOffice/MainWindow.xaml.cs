using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace HomeOrOffice
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        private enum Dialog
        {
            IstNichtErweitert = 0,
            IstErweitert
        }

        //private static Boolean IstErweitert = false;
        //private static Boolean NichtErweitert = true;
        private DBHandler dbhandler;
        private Boolean Autostart = false;

        private Int32 lastReadOrWritenEntry = (Int32)DBHandler.DBEntry.Error;


        public MainWindow()
        {
            SetLanguage();
            CheckCommandLineArgs();
            dbhandler = new DBHandler();
            if (Autostart && dbhandler.GetDate(DateTime.Now) != (Int32)DBHandler.DBEntry.NoEntry)
            {
                this.Close();
                return;
            }

            InitializeComponent();
            cbOffice.IsChecked = false;
            cbHome.IsChecked = false;
            cbOffice.Checked += cbCheckedChanged;
            cbHome.Checked += cbCheckedChanged;
            cbBusinessTrip.Checked += cbCheckedChanged;
            cbOffice.Unchecked += cbCheckedChanged;
            cbHome.Unchecked += cbCheckedChanged;
            cbBusinessTrip.Unchecked += cbCheckedChanged;


            btnErweitern.Tag = Dialog.IstNichtErweitert;
            SetMainText();
            this.Height = 200;
            this.Width = 600;

            btnExport.HorizontalContentAlignment = HorizontalAlignment.Center;
            btnExport.VerticalContentAlignment = VerticalAlignment.Center;
            cDayPicker.VerticalAlignment = VerticalAlignment.Center;

            cDayPicker.SelectedDate = DateTime.Now;

        }

        private void CheckCommandLineArgs()
        {
            String[] CommandLineArgs = Environment.GetCommandLineArgs();
            if (CommandLineArgs.Length > 0)
            {
                String[] SplitArg = null;
                try
                {
                    for (int i = 1; i < CommandLineArgs.Length; i++)
                    {
                        SplitArg = CommandLineArgs[i].Split(':');
                        if (SplitArg[0] == "auto")
                        {
                            Autostart = true;
                        }
                    }
                }
                catch (Exception)
                {
                    if (SplitArg != null) MessageBox.Show(rm.GetString($"CommandLineError", ci));
                }
            }
        }

        private void btnSubmitClick(Object sender, RoutedEventArgs e)
        {
            DateTime SelectedDay = (DateTime)cDayPicker?.SelectedDate;
            if (SelectedDay > DateTime.Now)
            {
                if (MessageBox.Show(rm.GetString($"ErrorMessageFutureDate", ci), rm.GetString($"ErrorCaptionFutureDate", ci), MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
            }

            if (dbhandler.GetDate(SelectedDay) != (Int32)DBHandler.DBEntry.NoEntry)
            {
                if (MessageBoxResult.No == MessageBox.Show(rm.GetString($"ErrorMessageAlreadyEntry", ci), rm.GetString($"ErrorCaptionAlreadyEntry", ci), MessageBoxButton.YesNo))
                {
                    return;
                }
            }
            Boolean setEntry = dbhandler.SetEntry(SelectedDay, (bool)cbHome.IsChecked, (bool)cbOffice.IsChecked, (bool)cbBusinessTrip.IsChecked);
            if (setEntry && (Dialog)btnErweitern.Tag == Dialog.IstNichtErweitert)
            {
                this.Close();
            }
            else
            {
                WritelbRead(dbhandler.GetDate((DateTime)cDayPicker.SelectedDate));
            }
        }

        private void btnCancelClick(Object sender, RoutedEventArgs e)
        {
            dbhandler.Dispose();
            this.Close();
        }

        private void cbCheckedChanged(Object sender, RoutedEventArgs e)
        {
            btnSubmit.IsEnabled = (cbHome.IsChecked ?? false) || (cbOffice.IsChecked ?? false) || (cbBusinessTrip.IsChecked ?? false);
        }

        private void BtnErweitern_Click(Object sender, RoutedEventArgs e)
        {
            if ((Dialog)btnErweitern.Tag == Dialog.IstNichtErweitert)
            {
                Animation(400);
                btnErweitern.Content = rm.GetString($"btnErweiternLess", ci);
                lbHeader.Content = rm.GetString($"lbHeaderSelect", ci);
                btnErweitern.Tag = Dialog.IstErweitert;
                cDayPicker.SelectedDate = DateTime.Now;
                WritelbRead(dbhandler.GetDate((DateTime)cDayPicker.SelectedDate));
            }
            else if ((Dialog)btnErweitern.Tag == Dialog.IstErweitert)
            {
                Animation(200);
                btnErweitern.Content = rm.GetString($"btnErweiternMore", ci);
                lbHeader.Content = rm.GetString($"lbHeaderWhere", ci);
                cDayPicker.SelectedDate = DateTime.Now;
                btnErweitern.Tag = Dialog.IstNichtErweitert;
            }
        }

        private void CDayPicker_SelectedDatesChanged(Object sender, SelectionChangedEventArgs e)
        {
            if (cDayPicker.SelectedDate != null)
            {
                WritelbRead(dbhandler.GetDate((DateTime)cDayPicker.SelectedDate));
            }
            Mouse.Capture(null);
            btnSubmit.Focus();
        }

        private void BtnDelete_Click(Object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.No == MessageBox.Show(rm.GetString($"ErrorMessageDeleteEntry", ci), rm.GetString($"ErrorCaptionDeleteEntry", ci), MessageBoxButton.YesNo))
                return;
            else if (dbhandler.DeleteEntry((DateTime)cDayPicker?.SelectedDate))
                WritelbRead(dbhandler.GetDate((DateTime)cDayPicker.SelectedDate));
        }

        private void BtnExport_Click(Object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            ExportKonfig export = new ExportKonfig(rm, ci);
            export.Owner = this;
            export.WindowStartupLocation = WindowStartupLocation.Manual;
            export.Left = this.Left;
            export.Top = this.Top;
            export.ShowDialog();
            this.IsEnabled = true;
        }

        private void Animation(int heigth)
        {
            // Animation für die Höhe (Height)
            DoubleAnimation heightAnimation = new DoubleAnimation();
            heightAnimation.From = this.Height; // aktuelle Höhe
            heightAnimation.To = heigth; // Zielhöhe
            heightAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.5));

            // Erstelle das Storyboard
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(heightAnimation);

            // Wende die Animationen auf das Fenster an
            Storyboard.SetTarget(heightAnimation, this);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(Window.HeightProperty));

            // Starte die Animation
            storyboard.Begin();
        }
    }
}
