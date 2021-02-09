using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using System.IO;


namespace CLIMusicDotNet
{
    class Program
    {





        static void Main(string[] args)
        {



            GUIApp CLIMusicDotNetApp = new GUIApp();

            CLIMusicDotNetApp.Init();
            CLIMusicDotNetApp.AddControls();
        }

   

        private static void MusicView_OpenSelectedItem(ListViewItemEventArgs obj)
        {
            throw new NotImplementedException();
        }
    }
}
