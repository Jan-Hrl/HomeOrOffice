using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Windows;

namespace HomeOrOffice
{
    class DBHandler : IDisposable
    {
        #region Konstanten

        String connectionString = @"Data Source=c:\ProgramData\HomeOrOffice\Datenbank\HomeOrOfficeDB.db;";

        #endregion Konstanten 

        public enum DBEntry
        {
            Error = -1,
            NoEntry = 0,
            AtHome,
            InTheOffice,
            BuisnessTrip
        }

        private SQLiteConnection connection;


        public DBHandler(DateTime SelectedDay)
        {
            try
            {
                //string connectionString = $"Data Source=.\\Datenbank\\HomeOrOfficeDB.db;";

                connection = new SQLiteConnection(connectionString);
                Open();
                if (!DoesTableExist(SelectedDay.Year.ToString()))
                    CreateTableOfYear(SelectedDay);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public Boolean SetEntry(DateTime day, Boolean Home, Boolean Office, Boolean BusinessTrip)
        {
            try
            {
                if (!DoesTableExist(day.Year.ToString()))
                    CreateTableOfYear(day);
                if (connection.State == System.Data.ConnectionState.Open)
                {
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
                    using (SQLiteCommand createColoum = connection.CreateCommand())
                    {
                        createColoum.CommandText = $"INSERT INTO '{day.Year}'(DayOfYear, atHome, inTheOffice, BusinessTrip) VALUES({day.DayOfYear}, {Home}, {Office}, {BusinessTrip})";
                        if (createColoum.ExecuteNonQuery() >= 0)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    throw new Exception("Database connection is not open.");
                }
                return false;
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message);
                return false; 
            }
            
        }

        private void Open()
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show("Error opening DB connection\n" + ex.Message);
            }
            
        }

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
                case DBEntry.BuisnessTrip:
                    query = "BusinessTrip = '1';";
                    break;
                default:
                    return -1;
                    break;
            }
            try
            {
                using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM '{Table}' WHERE " + query, connection))
                {
                    short result =0;
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

        public DBEntry GetDate(DateTime SelectedDate)
        {
            DBEntry res = DBEntry.NoEntry;
            if (!DoesTableExist(SelectedDate.Year.ToString()))
            {
                return DBEntry.NoEntry; 
            }
            if (connection.State != System.Data.ConnectionState.Open)
            {
               return DBEntry.Error;
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
                                bool Home = Convert.ToBoolean(reader.GetInt32(0));
                                bool Office = Convert.ToBoolean(reader.GetInt32(1));
                                bool BusinessTrip = reader.IsDBNull(2) ? false : Convert.ToBoolean(reader.GetInt32(2));
                                if (BusinessTrip)
                                {
                                    res =  DBEntry.BuisnessTrip;
                                }
                                else if(Office)
                                {
                                    res = DBEntry.InTheOffice;
                                }
                                else if (Home)
                                {
                                    res = DBEntry.AtHome;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return DBEntry.Error;
                }
            }
            return res;
        }

        private Boolean DoesTableExist(String Table)
        {
            try
            {
                List<String> list = new List<string>();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';", connection))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add($"{reader["name"]}");
                        }
                    }
                }
                if (list.Contains(Table))
                {
                    CheckColumn(Table);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

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

        public List<String> GetEveryTable()
        {
            List<String> everyTable = new List<string>();
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

        public Boolean DeleteEntry(DateTime SelectedDate)
        {
            try
            {
                DBEntry DBEntry = GetDate(SelectedDate);
                if (DBEntry == DBEntry.NoEntry || DBEntry == DBEntry.Error)
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

        public void Dispose()
        {
            Close();
        }
    }
}
