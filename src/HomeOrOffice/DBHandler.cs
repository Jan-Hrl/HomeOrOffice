namespace HomeOrOffice
{
    using DocumentFormat.OpenXml.Bibliography;
    using DocumentFormat.OpenXml.Drawing.Diagrams;

    #region using

    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    
    #endregion

    /// <summary>
    /// Klasse für die Kommunikationen zur SQLlite Datenbank
    /// </summary>
    internal class DBHandler : IDisposable
    {
        #region Konstanten, globale Variablen, enum

        /// <summary>Pfad der SQLite Datenbankdatei</summary>
        private const String databasePath = @"c:\ProgramData\HomeOrOffice\Datenbank\HomeOrOfficeDB.db";

        /// <summary>Verbindungsstring für SQLLite Datenbank</summary>
        private const String connectionString = "Data Source=" + databasePath + ";";

        /// <summary>Mögliche Einträge für einen Tag in der Datenbank</summary>
        [Flags]
        public enum DBEntry 
        {
            /// <summary>Es ist ein Fehler beim Lesen oder Verarbeiten aufgetreten.</summary>
            Error = -1,
            /// <summary>Für den Tag ist kein Eintrag gespeichert.</summary>
            NoEntry = 0,
            /// <summary>Der Arbeitstag wurde im Büro verbracht.</summary>
            InTheOffice = 1,
            /// <summary>Der Arbeitstag wurde im Homeoffice verbracht.</summary>
            AtHome = 2,
            /// <summary>Der Arbeitstag wurde auf einer Geschäftsreise verbracht.</summary>
            BusinessTrip = 4
        }

        /// <summary>Verbindungsobjekt zur Datenbank</summary>
        private SQLiteConnection connection;

        #endregion Konstanten, globale Variablen

        #region Konstruktor

        /// <summary>
        /// Konstruktor der Klasse DBHandler
        /// </summary>
        public DBHandler()
        {
            try
            {
                open();

                //Überprüfen ob für das aktuelle Jahr schon eine Tabelle angelegt ist
                if (!doesTableExist(DateTime.Now.Year.ToString()))
                    createTableOfYear(DateTime.Now);
            }
            catch (Exception ex)
            {
                throw new Exception("Error opening DB connection\n" + ex.Message, ex);
            }
        }

        #endregion Konstruktor

        #region public

        //Ist über die drei Bool Werte geregelt, weil ich mal überlegt ob Sinn ergibt mehrer 
        //Optionen für einen Tag anklickbar zu machen.
        //Falls man das jedoch machen möchte muss man ein paar stellen nochmal überarbeiten
        /// <summary>Speichert den Arbeitsort-Eintrag für den gewünschten Tag.</summary>
        /// <param name="day">Der ausgewählte Tag</param>
        /// <param name="Home">Hab ich an diesem Tag im Homeoffice gearbeitet?</param>
        /// <param name="Office">Hab ich an diesem Tag im Büro gearbeitet?</param>
        /// <param name="BusinessTrip">War ich an dem Tag auf einer Geschäftsreise</param>
        public void SetEntry(DateTime day, Boolean Home, Boolean Office, Boolean BusinessTrip)
        {
            //Tabelle muss erstellt sein
            if (!doesTableExist(day.Year.ToString()))
                createTableOfYear(day);

            if (connection.State == System.Data.ConnectionState.Open)
            {
                if (GetDate(day) == DBEntry.NoEntry)
                    //Falls es sich um einen neuen Eintrag handelt
                    createEntry(day, Home, Office, BusinessTrip);
                else
                    //Falls ich einen bestehenden Eintrag ändere
                    updateEntry(day, Home, Office, BusinessTrip);
            }
            else
            {
                throw new Exception("Database connection is not open.");
            }   
        }

        /// <summary>Liest den Eintrag für ein gewünschtes Datum aus.</summary>
        /// <param name="SelectedDate">Der Tag der ausgelesen werden soll</param>
        /// <returns>Information darüber wo man sich an diesen Tag aufgehalten hat</returns>
        public DBEntry GetDate(DateTime SelectedDate)
        {
            DBEntry res = DBEntry.NoEntry;
            if (!doesTableExist(SelectedDate.Year.ToString()))
            {
                return res;
            }

            if (connection.State != System.Data.ConnectionState.Open)
            {
                return DBEntry.Error;
            }
            else
            {   
                using (SQLiteCommand cmd = new SQLiteCommand($"SELECT atHome, inTheOffice, BusinessTrip FROM '{SelectedDate.Year}' WHERE DayOfYear={SelectedDate.DayOfYear};", connection))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Boolean Home = !reader.IsDBNull(0) && Convert.ToBoolean(reader.GetInt32(0));
                            Boolean Office = !reader.IsDBNull(1) && Convert.ToBoolean(reader.GetInt32(1));
                            Boolean BusinessTrip = !reader.IsDBNull(2) && Convert.ToBoolean(reader.GetInt32(2));

                            //Dachte mal darüber nach mehrerr auswahl für einen Tag anwählbar zu machne, darum verodert
                            if (Office)
                                res |= DBEntry.InTheOffice;
                            if (Home)
                                res |= DBEntry.AtHome;
                            if (BusinessTrip)
                                res |= DBEntry.BusinessTrip;
                                
                            //Wenn es einen Eintrag gibt muss auch eines der drei ausgewählt sein. 
                            if (res == DBEntry.NoEntry)
                                res = DBEntry.Error;

                        }
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// List alle verfügbara Tabellen (Jahre) aus
        /// </summary>
        /// <returns>Liste mit alle Tabellennamen (Jahreszahlen)</returns>
        public List<String> GetEveryTable()
        {
            List<String> everyTable = new List<String>();
            if (connection.State == System.Data.ConnectionState.Open)
            {
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';", connection))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            everyTable.Add($"{reader["name"]}");
                        }
                    }
                }
                everyTable.Sort();
            }
            return everyTable;
        }

        /// <summary>
        /// Löscht den Eintrag für einen bestimmten Tag
        /// </summary>
        /// <param name="SelectedDate">Der zu löschende Tag</param>
        /// <returns>true: erfolgreich, false:something went wrong</returns>
        public Boolean DeleteEntry(DateTime SelectedDate)
        {           
            DBEntry dbEntry = GetDate(SelectedDate);
            if (dbEntry == DBEntry.NoEntry)
            {
                throw new Exception("No entry found");
            }

            using (SQLiteCommand cmd = new SQLiteCommand($"DELETE FROM '{SelectedDate.Year}' WHERE DayOfYear = '{SelectedDate.DayOfYear}';", connection))
            {
                if (cmd.ExecuteNonQuery() < 1)
                    throw new Exception("ERROR!! Entry was not deleted.");
                else
                    return true;
            }
        }

        /// <summary>Löscht ein ganzes Jahr bzw. eine komplette Tabelle</summary>
        /// <param name="Table">Die zu löschende Tabell/Jahr</param>
        public void DeleteTable(String Table)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    if (!doesTableExist(Table))
                    {
                        throw new Exception($"There is no Database {Table}!");
                    }
                    using (SQLiteCommand dropTable = connection.CreateCommand())
                    {
                        dropTable.CommandText = $"DROP TABLE '{Table}';";
                        dropTable.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"The Database {Table} is not delete! " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Methode zum Schließen
        /// </summary>
        public void Dispose()
        {
            close();
        }

        /// <summary>Liest aus wie viele Tage du an einem gewünschnten Ort warst</summary>
        /// <param name="entryType">Die gewünschte Kategorie</param>
        /// <param name="Table">Die Jahres-Tabelle, in der gezählt wird.</param>
        /// <returns>Die Anzahl der gespeicherten Einträge für die Kategorie.</returns>
        public Int32 CountDays(DBEntry entryType, String Table)
        {
            String query;
            switch (entryType)
            {
                case DBEntry.AtHome:
                    query = "atHome = '1';";
                    break;
                case DBEntry.InTheOffice:
                    query = "inTheOffice = '1';";
                    break;
                case DBEntry.BusinessTrip:
                    query = "BusinessTrip = '1';";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entryType), entryType, "Unsupported database entry type.");
            }

            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT COUNT(*) FROM '{Table}' WHERE " + query, connection))
            {
                Int32 result = 0;
                result = Convert.ToInt32(cmd.ExecuteScalar());
                    
                return result;
            }
        }

        #endregion public

        #region private

        /// <summary>
        /// Öffnet die Verbindung zur Datenbank
        /// </summary>
        private void open()
        {
            //SQLite legt die Datei an, aber keine fehlenden Ordner (z. B. wenn die App ohne Setup gestartet wird)
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(databasePath));

            connection = new SQLiteConnection(connectionString);
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
            
        }

        /// <summary>Erstellt eine Datenbanktabelle für das Jahr des übergebenden Tages</summary>
        /// <param name="SelectedDay">Der ausgewählte Tag</param>
        private void createTableOfYear(DateTime SelectedDay)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    using (SQLiteCommand createTable = connection.CreateCommand())
                    {
                        createTable.CommandText = $"CREATE TABLE IF NOT EXISTS '{SelectedDay.Year}' (DayOfYear INTEGER PRIMARY KEY, atHome INTEGER, inTheOffice INTEGER, BusinessTrip INTEGER);";
                        createTable.ExecuteNonQuery();
                    }
                }
                else
                {
                    throw new Exception("Connection is not open.");
                }
            }
            catch (Exception ex)
            {
               throw new Exception("Error creating Table!\n" + ex.Message, ex);
            }

        }

        /// <summary>Überprüft ob die Tabelle Vorhanden bereits Vorhanden ist</summary>
        /// <param name="Table">Name der Tabellle</param>
        /// <returns>true: Tabelle exestiert, false: Tabelle exestiert nicht</returns>
        private Boolean doesTableExist(String Table)
        {            
            List<String> existingTables = GetEveryTable();
            if (existingTables != null && existingTables.Contains(Table))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Verbindung zur Datenbank wird getrennt
        /// </summary>
        private void close()
        {
            try
            {
                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {

                throw new Exception("Fehler beim Schließen der Datenbank! \n" + ex.Message, ex);
            }
        }

        /// <summary>Aktualisiert einen vorhandenen Eintrag für den gewünschten Tag.</summary>
        private void updateEntry(DateTime day, Boolean Home, Boolean Office, Boolean BusinessTrip)
        {
            using (SQLiteCommand cmd = new SQLiteCommand($"UPDATE '{day.Year}' Set atHome = {Home}, inTheOffice = {Office}, BusinessTrip = {BusinessTrip} WHERE DayOfYear = '{day.DayOfYear}'", connection))
            {
                if (cmd.ExecuteNonQuery() < 1)
                {
                    throw new Exception("Somthing went wrong! \nNo coloum were changed");
                }
            }
        }

        /// <summary>Erstellt einen neuen Eintrag für den gewünschten Tag.</summary>
        private void createEntry(DateTime day, Boolean Home, Boolean Office, Boolean BusinessTrip)
        {
            using (SQLiteCommand createColoum = connection.CreateCommand())
            {
                createColoum.CommandText = $"INSERT INTO '{day.Year}'(DayOfYear, atHome, inTheOffice, BusinessTrip) VALUES({day.DayOfYear}, {Home}, {Office}, {BusinessTrip})";
                if (createColoum.ExecuteNonQuery() < 1)
                {
                    throw new Exception("Somthing went wrong! \nNo coloum were insert");
                }
            }
        }

        #endregion privates

    }
}
