
namespace HomeOrOffice
{
    #region Using 

    using System;
    using System.Reflection;
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
        #region Properties, Variablen, Enums

        /// <summary>
        /// enum zum vermeiden von Magic numbers
        /// </summary>
        private enum Dialog
        {
            /// <summary>Die erweiterten Steuerelemente sind ausgeblendet.</summary>
            IstNichtErweitert = 0,
            /// <summary>Die erweiterten Steuerelemente sind eingeblendet.</summary>
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
        private DBHandler.DBEntry lastReadOrWritenEntry = DBHandler.DBEntry.Error;

        /// <summary>Zeigt Fehler aus Benutzeraktionen in Dialogfenstern an.</summary>
        private FehlerMelder fm;

        #endregion Properties, Variablen, Enums

        #region Konstruktor

        /// <summary>
        /// Konstruktor des Main Windows
        /// </summary>
        public MainWindow()
        {

            checkCommandLineArgs();
            setStartupLanguage();
            dbHandler = new DBHandler();
            InitializeComponent();

            fm = new FehlerMelder(this);
            if (Autostart && 
                fm.VersucheAuszuführen(() => dbHandler.GetDate(DateTime.Now), DBHandler.DBEntry.Error) != DBHandler.DBEntry.NoEntry)
            {
                this.Close();
                return;
            }


            Assembly assemblyInfos = Assembly.GetExecutingAssembly();
#if DEBUG
            Console.WriteLine(assemblyInfos.GetName().Version);
            Console.WriteLine(assemblyInfos.ImageRuntimeVersion);
#endif

            cbOffice.IsChecked = false;
            cbHome.IsChecked = false;
            cbOffice.Checked += cbCheckedChanged;
            cbHome.Checked += cbCheckedChanged;
            cbBusinessTrip.Checked += cbCheckedChanged;
            cbOffice.Unchecked += cbCheckedChanged;
            cbHome.Unchecked += cbCheckedChanged;
            cbBusinessTrip.Unchecked += cbCheckedChanged;
            btnErweitern.Tag = Dialog.IstNichtErweitert;
            btnExport.HorizontalContentAlignment = HorizontalAlignment.Center;
            btnExport.VerticalContentAlignment = VerticalAlignment.Center;
            cDayPicker.VerticalAlignment = VerticalAlignment.Center;
            cDayPicker.SelectedDate = DateTime.Now;
            lbVersion.Content = assemblyInfos.GetName().Version.ToString();

            setMainText();
            this.Height = 200;
            this.Width = 600;

        }

        #endregion Konstruktor

        #region Events 

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
                    if (MessageBox.Show(HomeOrOffice.Resources.String.ErrorMessageFutureDate, HomeOrOffice.Resources.String.ErrorCaptionFutureDate, MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                //Überprüft ob es bereits einen Datenbankeintrag gibt
                if (fm.VersucheAuszuführen(() => dbHandler.GetDate((DateTime)cDayPicker.SelectedDate), DBHandler.DBEntry.Error) != DBHandler.DBEntry.NoEntry)
                {
                    if (MessageBoxResult.No == MessageBox.Show(HomeOrOffice.Resources.String.ErrorMessageAlreadyEntry, HomeOrOffice.Resources.String.ErrorCaptionAlreadyEntry, MessageBoxButton.YesNo))
                    {
                        return;
                    }
                }
              
                //DB Eintrag wird gesetzt 
                fm.VersucheAuszuführen(() => dbHandler.SetEntry(SelectedDay, (bool)cbHome.IsChecked, (bool)cbOffice.IsChecked, (bool)cbBusinessTrip.IsChecked));

                if ((Dialog)btnErweitern.Tag == Dialog.IstNichtErweitert)
                {
                    this.Close();
                }
                else
                {    
                    writelbRead(fm.VersucheAuszuführen(() => dbHandler.GetDate((DateTime)cDayPicker.SelectedDate), DBHandler.DBEntry.Error));
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
            fm.VersucheAuszuführen(()=> dbHandler.Dispose());
            
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
                animation(400);
                btnErweitern.Content = HomeOrOffice.Resources.String.btnErweiternLess;
                lbHeader.Content = HomeOrOffice.Resources.String.lbHeaderSelect;
                btnErweitern.Tag = Dialog.IstErweitert;
                cDayPicker.SelectedDate = DateTime.Now;
                writelbRead(fm.VersucheAuszuführen(() => dbHandler.GetDate((DateTime)cDayPicker.SelectedDate), DBHandler.DBEntry.Error));
            }
            else if ((Dialog)btnErweitern.Tag == Dialog.IstErweitert)
            {
                animation(200);
                btnErweitern.Content = HomeOrOffice.Resources.String.btnErweiternMore;
                lbHeader.Content = HomeOrOffice.Resources.String.lbHeaderWhere;
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
                writelbRead(fm.VersucheAuszuführen(() => dbHandler.GetDate((DateTime)cDayPicker.SelectedDate), DBHandler.DBEntry.Error));
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
            
            if (MessageBoxResult.Yes == MessageBox.Show(HomeOrOffice.Resources.String.ErrorMessageDeleteEntry, HomeOrOffice.Resources.String.ErrorCaptionDeleteEntry, MessageBoxButton.YesNo))
                if (fm.VersucheAuszuführen(() => dbHandler.DeleteEntry((DateTime)cDayPicker?.SelectedDate), false))
                    writelbRead(fm.VersucheAuszuführen(() => dbHandler.GetDate((DateTime)cDayPicker.SelectedDate), DBHandler.DBEntry.Error));

        }

        /// <summary>
        /// Event, ausgelöst durch exportieren Button
        /// </summary>
        /// <param name="sender">Senderinformationen</param>
        /// <param name="e">Eventinformationens</param>
        private void BtnExport_Click(Object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            ExportKonfig export = new ExportKonfig(ci, dbHandler)
                                  {
                                      Owner = this,
                                      WindowStartupLocation = WindowStartupLocation.Manual,
                                      Left = this.Left,
                                      Top = this.Top
                                  };
            export.ShowDialog();
            this.IsEnabled = true;
        }

        #endregion Events

        #region Methoden

        /// <summary>
        /// Überprüft die Übergabeparameter
        /// </summary>
        private void checkCommandLineArgs()
        {
            String[] CommandLineArgs = Environment.GetCommandLineArgs();
            if (CommandLineArgs.Length > 0)
            {
                for (int i = 1; i < CommandLineArgs.Length; i++)
                {
                    String[] SplitArg = CommandLineArgs[i].Split('\\');
                    if (SplitArg[SplitArg.Length - 1] == "auto")
                    {
                        Autostart = true;
                    }
                }
            }
        }

        /// <summary>
        /// Animiert die größenveränderung des Hauptwindow
        /// </summary>
        /// <param name="heigth">die gewünschte Höhe</param>
        private void animation(int heigth)
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

        #endregion Methoden

    }
}
