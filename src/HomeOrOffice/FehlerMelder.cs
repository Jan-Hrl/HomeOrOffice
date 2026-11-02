

namespace HomeOrOffice
{
    #region using

    using System;
    using System.Windows;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Buffers;

    #endregion using

    /// <summary>
    /// Führt Aktionen aus und zeigt aufgetretene Fehler in einem Dialogfenster an.
    /// </summary>
    internal class FehlerMelder
    {
        /// <summary>Fenster, dem angezeigte Fehlerdialoge zugeordnet werden.</summary>
        private readonly Window owner;

        /// <summary>
        /// Initialisiert einen neuen FehlerMelder für ein Fenster.
        /// </summary>
        /// <param name="Owner">Das Fenster, dem die Fehlerdialoge gehören.</param>
        internal FehlerMelder(Window Owner) 
        {
            owner = Owner;
        }

        /// <summary>
        /// Führt eine wertliefernde Aktion aus und gibt bei einem Fehler einen Ersatzwert zurück.
        /// </summary>
        /// <typeparam name="T">Der Rückgabetyp der Aktion.</typeparam>
        /// <param name="aktion">Die auszuführende Aktion.</param>
        /// <param name="fehlerfall">Der Rückgabewert im Fehlerfall.</param>
        /// <returns>Das Ergebnis der Aktion oder den Ersatzwert bei einem Fehler.</returns>
        internal T VersucheAuszuführen<T>(Func<T> aktion, T fehlerfall)
        {
            try
            {
                return aktion();
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, ex.Message);
                return fehlerfall;
            }
        }

        /// <summary>
        /// Führt eine Aktion ohne Rückgabewert aus und zeigt auftretende Fehler an.
        /// </summary>
        /// <param name="aktion">Die auszuführende Aktion.</param>
        internal void VersucheAuszuführen(Action aktion)
        {
            try
            {
                aktion();
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, ex.Message);
            }
        }

    }
}
