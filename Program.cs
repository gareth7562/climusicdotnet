using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using System.IO;


namespace TerminalMP3
{
    class Program
    {





        static void Main(string[] args)
        {



            GUIApp TerminalMp3 = new GUIApp();

            TerminalMp3.Init();
            TerminalMp3.AddControls();
        }

   

        private static void MusicView_OpenSelectedItem(ListViewItemEventArgs obj)
        {
            throw new NotImplementedException();
        }
    }
}
