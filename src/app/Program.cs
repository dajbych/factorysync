#pragma warning disable CA2000 // Dispose objects before losing scope

using System;
using System.Windows.Forms;

namespace Dajbych.FactorySync {

    /*
        icons:
        https://www.iconsdb.com/white-icons/settings-4-icon.html
        https://www.iconsdb.com/orange-icons/settings-4-icon.html
    */

    internal static class Program {

        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(args));
        }

    }

}