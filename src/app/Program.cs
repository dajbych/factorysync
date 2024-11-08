using System;
using System.Windows.Forms;

namespace Dajbych.FactorySync {

    /*
        icons:
        https://www.iconsdb.com/white-icons/gear-icon.html
        https://www.iconsdb.com/orange-icons/gear-icon.html
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