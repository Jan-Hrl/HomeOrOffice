namespace HomeOrOffice
{

    #region using

    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.Windows;
    
    #endregion

    /// <summary>
    /// Klasse für die Kommunikationen zur SQLlite Datenbank
    /// </summary>
    internal class DBHandler : IDisposable
    {
        #region Konstanten, globale Variablen, enum

        /// <summary>Verbindungsstring für SQLLite Datenbank</summary>
        private const String connectionString = @"Data Source=c:\ProgramData\HomeOrOffice\Datenbank\HomeOrOfficeDB.db;";

        /// <summary>Mögliche Einträge für einen Tag in der Datenbank</summary>
        [Flags]
        public enum DBEntry 
        {
            Error = -1,
            NoEntry = 0,
            InTheOffice = 1,
            AtHome = 2,
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
                Open();

                //Überprüfen ob für das aktuelle Jahr schon eine Tabelle angelegt ist
                if (!DoesTableExist(DateTime.Now.Year.ToString()))
                    CreateTableOfYear(DateTime.Now);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening DB connection\n" + ex.Message);
            }
        }

        #endregion Konstruktor

        /// <summary>
        /// Öffnet die Verbindung zur Datenbank
        /// </summary>
        private void Open()
        {
            connection = new SQLiteConnection(connectionString);
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
            
        }

        /// <summary>Erstellt eine Datenbanktabelle für das Jahr des übergebenden Tages</summary>
        /// <param name="SelectedDay">Der ausgewählte Tag</param>
        private void CreateTableOfYear(DateTime SelectedDay)
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
                MessageBox.Show("Error creating Table!\n" + ex.Message);
            }

        }

        //Ist über die drei Bool Werte geregelt, weil ich mal überlegt ob Sinn ergibt mehrer 
        //Optionen für einen Tag anklickbar zu machen.
        //Falls man das jedoch machen möchte muss man ein paar stellen nochmal überarbeiten
        /// <summary>Erstellt den Datenbankeintrag für den gewünschten Tag</summary>
        /// <param name="day">Der ausgewählte Tag</param>
        /// <param name="Home">Hab ich an diesem Tag im Homeoffice gearbeitet?</param>
        /// <param name="Office">Hab ich an diesem Tag im Büro gearbeitet?</param>
        /// <param name="BusinessTrip">War ich an dem Tag auf einer Geschäftsreise</param>
        /// <returns>true: speichern erfolgreich, false: speichern fehlgeschlagen</returns>
        public Boolean SetEntry(DateTime day, Boolean Home, Boolean Office, Boolean BusinessTrip)
        {
            try
            {
                //Tabelle muss erstellt sein
                if (!DoesTableExist(day.Year.ToString()))
                    CreateTableOfYear(day);
                
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    //Falls ich einen bestehenden Eintrag ändere
                    using(SQLiteCommand cmd = new SQLiteCommand($"SELECT DayOfYear FROM '{day.Year}' WHERE DayOfYear ='{day.DayOfYear}';", connection))
                    {
                        if (cmd.ExecuteScalar() != null)
                        {
                            cmd.CommandText = $"UPDATE '{day.Year}' Set atHome = {Home}, inTheOffice = {Office}, BusinessTrip = {BusinessTrip} WHERE DayOfYear = '{day.DayOfYear}'";
                            if (cmd.ExecuteNonQuery()<1)
                            {
                                throw new Exception("Somthing went wrong! \nNo coloum were changed");
                            }
                            return true;
                        }
                    }
                    //Falls es sich um einen neuen Eintrag handelt
                    using (SQLiteCommand createColoum = connection.CreateCommand())
                    {
                        createColoum.CommandText = $"INSERT INTO '{day.Year}'(DayOfYear, atHome, inTheOffice, BusinessTrip) VALUES({day.DayOfYear}, {Home}, {Office}, {BusinessTrip})";
                        if (createColoum.ExecuteNonQuery() < 1)
                        {
                            throw new Exception("Somthing went wrong! \nNo coloum were insert");
                        }
                        return true;
                    }
                }
                else
                {
                    throw new Exception("Database connection is not open.");
                }
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message);
               return false; 
            }
            
        } 

        /// <summary>Liest den Eintrag für ein gewünschtes Datum aus.</summary>
        /// <param name="SelectedDate">Der Tag der ausgelesen werden soll</param>
        /// <returns>Information darüber wo man sich an diesen Tag aufgehalten hat</returns>
        public Int32 GetDate(DateTime SelectedDate)
        {
            Int32 res = (Int32)DBEntry.NoEntry;
            if (!DoesTableExist(SelectedDate.Year.ToString()))
            {
                return res;
            }

            if (connection.State != System.Data.ConnectionState.Open)
            {
               return (Int32)DBEntry.Error;
            }
            else
            {
                try
                {
                    using (SQLiteCommand cmd = new SQLiteCommand($"SELECT atHome, inTheOffice, BusinessTrip FROM '{SelectedDate.Year}' WHERE DayOfYear={SelectedDate.DayOfYear};", connection))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Boolean Office         = !reader.IsDBNull(1) && Convert.ToBoolean(reader.GetInt32(1));
                                Boolean Home           = !reader.IsDBNull(0) && Convert.ToBoolean(reader.GetInt32(0));
                                Boolean BusinessTrip   = !reader.IsDBNull(2) && Convert.ToBoolean(reader.GetInt32(2)); 

                                //Dachte mal darüber nach mehrerr auswahl für einen Tag anwählbar zu machne, darum verodert
                                if(Office)
                                    res |= (Int32)DBEntry.InTheOffice;
                                if (Home)
                                    res |= (Int32)DBEntry.AtHome;
                                if (BusinessTrip)
                                    res |= (Int32)DBEntry.BusinessTrip;
                                
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return (Int32)DBEntry.Error;
                }
            }
            return res;
        }

        /// <summary>Überprüft ob die Tabelle Vorhanden bereits Vorhanden ist</summary>
        /// <param name="Table">Name der Tabellle</param>
        /// <returns>true: Tabelle exestiert, false: Tabelle exestiert nicht</returns>
        private Boolean DoesTableExist(String Table)
        {
            try
            {
                List<String> existingTables = GetEveryTable();
                if (existingTables.Contains(Table))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// List alle verfügbara Tabellen (Jahre) aus
        /// </summary>
        /// <returns>Liste mit alle Tabellennamen (Jahreszahlen)</returns>
        public List<String> GetEveryTable()
        {
            List<String> everyTable = new List<String>();
            try
            {
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
                    return everyTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            try
            {
                Int32 dbEntry = GetDate(SelectedDate);
                if (dbEntry == (Int32)DBEntry.NoEntry || dbEntry == (Int32)DBEntry.Error)
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        /// <summary>Löscht ein ganzes Jahr bzw. eine komplette Tabelle</summary>
        /// <param name="Table">Die zu löschende Tabell/Jahr</param>
        public void DeleteTable(String Table)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    if (!DoesTableExist(Table))
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
                MessageBox.Show($"The Database {Table} is not delete! " +ex.Message);
            }
        }

        /// <summary>
        /// Verbindung zur Datenbank wird getrennt
        /// </summary>
        private void Close()
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show("Fehler beim Schließen der Datenbank! \n" + ex.Message);
            }
        }

        /// <summary>
        /// Methode zum Schließen
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        #region Archiv

        /// <summary>
        /// Am Anfang gab es die Spalte BusinessTrip nicht, erst später dazugekommen.
        /// Wird mitlerweil nicht mehr gebraucht
        /// </summary>
        /// <param name="Table">Name der zu überprüfenden Tabelle</param>
        private void CheckColumn(String Table)
        {
            // Abfrage, um die Spalten der Tabelle zu erhalten
            string query = $"PRAGMA table_info({Table})";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    int columnCount = 0;
                    while (reader.Read())
                    {
                        columnCount++;
                    }

                    // Wenn die Tabelle nur zwei Spalten hat, füge eine dritte hinzu
                    if (columnCount == 3)
                    {
                        string alterTableQuery = $"ALTER TABLE '{Table}' ADD COLUMN BusinessTrip INTEGER";
                        using (SQLiteCommand alterCommand = new SQLiteCommand(alterTableQuery, connection))
                        {
                            alterCommand.ExecuteNonQuery();
                        }
                    }
                    else if(columnCount != 4)
                    {
                        throw new Exception("Table editing error!");
                    }
                }
            }
        }

        //Wird nicht mehr verwendet
        /// <summary>Liest aus wie viele Tage du an einem gewünschnten Ort warst</summary>
        /// <param name="entryType">Die gewünschte Kategorie</param>
        /// <param name="Table"></param>
        /// <returns></returns>
        public Int16 CountDays(DBEntry entryType, String Table)
        {
            string query;

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
                    return -1;
            }
            try
            {
                using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM '{Table}' WHERE " + query, connection))
                {
                    short result = 0;
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result++;
                        }
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return -1;
        }

        #endregion Archiv
    }
}
