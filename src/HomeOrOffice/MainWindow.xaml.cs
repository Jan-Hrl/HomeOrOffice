namespace HomeOrOffice
{
    #region Using 

    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media.Animation;

    #endregion

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        /// <summary>
        /// enum zum vermeiden von Magic numbers
        /// </summary>
        private enum Dialog
        {
            IstNichtErweitert = 0,
            IstErweitert
        }

        /// <summary>
        /// Das Objekt welche die Kommunikation mit der SQLite Datenbank übernimmt
        /// </summary>
        private readonly DBHandler dbHandler;

        /// <summary>
        /// Zeigt an ob die App über den Autostart gestartet wurde
        /// </summary>
        private Boolean Autostart = false;

        /// <summary>
        /// Der zuletzt verwendete Eintrag
        /// </summary>
        private Int32 lastReadOrWritenEntry = (Int32)DBHandler.DBEntry.Error;

        /// <summary>
        /// Konstruktor des Main Windows
        /// </summary>
        public MainWindow()
        {
            SetLanguage();
            CheckCommandLineArgs();
            dbHandler = new DBHandler();
            if (Autostart && dbHandler.GetDate(DateTime.Now) != (Int32)DBHandler.DBEntry.NoEntry)
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

        /// <summary>
        /// Überprüft die Übergabeparameter
        /// </summary>
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
                        SplitArg = CommandLineArgs[i].Split('\\');
                        if (SplitArg[SplitArg.Length-1] == "auto")
                        {
                            Autostart = true;
                        }
                    }
                }
                catch (Exception)
                {
                    if (SplitArg != null)
                        MessageBox.Show(rm.GetString($"CommandLineError", ci));
                }
            }
        }

        /// <summary>
        /// Event das durch den bestätigungs Button ausgelöst wird
        /// </summary>
        /// <param name="sender">Sernderinformationen</param>
        /// <param name="e">Eventinformationen</param>
        private void btnSubmitClick(Object sender, RoutedEventArgs e)
        {
            if (cDayPicker?.SelectedDate != null)
            {
                DateTime SelectedDay = (DateTime)cDayPicker?.SelectedDate;
                //Überprüft ob das Datum in der Zukunft liegt
                if (SelectedDay > DateTime.Now)
                {
                    if (MessageBox.Show(rm.GetString("ErrorMessageFutureDate", ci), rm.GetString("ErrorCaptionFutureDate", ci), MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                //Überprüft ob es bereits einen Datenbankeintrag gibt
                if (dbHandler.GetDate(SelectedDay) != (Int32)DBHandler.DBEntry.NoEntry)
                {
                    if (MessageBoxResult.No == MessageBox.Show(rm.GetString("ErrorMessageAlreadyEntry", ci), rm.GetString("ErrorCaptionAlreadyEntry", ci), MessageBoxButton.YesNo))
                    {
                        return;
                    }
                }
                
                //DB Eintrag wird gesetzt 
                Boolean setEntry = dbHandler.SetEntry(SelectedDay, (bool)cbHome.IsChecked, (bool)cbOffice.IsChecked, (bool)cbBusinessTrip.IsChecked);
                if (setEntry && (Dialog)btnErweitern.Tag == Dialog.IstNichtErweitert)
                {
                    this.Close();
                }
                else
                {
                    WritelbRead(dbHandler.GetDate((DateTime)cDayPicker.SelectedDate));
                }
            }
        }

        /// <summary>
        /// Event, ausgelöst durch Abbruch Button
        /// </summary>
        /// <param name="sender">Senderinformationen</param>
        /// <param name="e">Eventinformationen</param>
        private void btnCancelClick(Object sender, RoutedEventArgs e)
        {
            dbHandler.Dispose();
            this.Close();
        }

        /// <summary>
        /// Event, ausgelöt durch wechseln der Radiobuttons 
        /// </summary>
        /// <param name="sender">Senderinformationen</param>
        /// <param name="e">Eventinformationen</param>
        private void cbCheckedChanged(Object sender, RoutedEventArgs e)
        {
            btnSubmit.IsEnabled = (cbHome.IsChecked ?? false) || (cbOffice.IsChecked ?? false) || (cbBusinessTrip.IsChecked ?? false);
        }

        /// <summary>
        /// Event, ausgelöst durch Erweitern Button
        /// </summary>
        /// <param name="sender">Senderinformationen</param>
        /// <param name="e">Eventinformationen</param>
        private void BtnErweitern_Click(Object sender, RoutedEventArgs e)
        {
            if ((Dialog)btnErweitern.Tag == Dialog.IstNichtErweitert)
            {
                Animation(400);
                btnErweitern.Content = rm.GetString("btnErweiternLess", ci);
                lbHeader.Content = rm.GetString("lbHeaderSelect", ci);
                btnErweitern.Tag = Dialog.IstErweitert;
                cDayPicker.SelectedDate = DateTime.Now;
                WritelbRead(dbHandler.GetDate((DateTime)cDayPicker.SelectedDate));
            }
            else if ((Dialog)btnErweitern.Tag == Dialog.IstErweitert)
            {
                Animation(200);
                btnErweitern.Content = rm.GetString("btnErweiternMore", ci);
                lbHeader.Content = rm.GetString("lbHeaderWhere", ci);
                cDayPicker.SelectedDate = DateTime.Now;
                btnErweitern.Tag = Dialog.IstNichtErweitert;
            }
        }

        /// <summary>
        /// Event, ausgelöst durch das änderen des Datums im CDayPicker
        /// </summary>
        /// <param name="sender">Senderinformationen</param>
        /// <param name="e">Eventinformationen</param>
        private void CDayPicker_SelectedDatesChanged(Object sender, SelectionChangedEventArgs e)
        {
            if (cDayPicker.SelectedDate != null)
            {
                WritelbRead(dbHandler.GetDate((DateTime)cDayPicker.SelectedDate));
            }
            Mouse.Capture(null);
            btnSubmit.Focus();
        }

        /// <summary>
        /// Event, ausgelöst durch Eintrag löschen Button
        /// </summary>
        /// <param name="sender">Senderinformationen</param>
        /// <param name="e">Eventinformationens</param>
        private void BtnDelete_Click(Object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.Yes == MessageBox.Show(rm.GetString("ErrorMessageDeleteEntry", ci), rm.GetString("ErrorCaptionDeleteEntry", ci), MessageBoxButton.YesNo))
                if (dbHandler.DeleteEntry((DateTime)cDayPicker?.SelectedDate))
                    WritelbRead(dbHandler.GetDate((DateTime)cDayPicker.SelectedDate));
                
        }

        /// <summary>
        /// Event, ausgelöst durch exportieren Button
        /// </summary>
        /// <param name="sender">Senderinformationen</param>
        /// <param name="e">Eventinformationens</param>
        private void BtnExport_Click(Object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            ExportKonfig export = new ExportKonfig(rm, ci, dbHandler);
            export.Owner = this;
            export.WindowStartupLocation = WindowStartupLocation.Manual;
            export.Left = this.Left;
            export.Top = this.Top;
            export.ShowDialog();
            this.IsEnabled = true;
        }

        /// <summary>
        /// Animiert die größenveränderung des Hauptwindow
        /// </summary>
        /// <param name="heigth">die gewünschte Höhe</param>
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
