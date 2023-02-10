using System;
using System.Collections.Generic;
using System.Windows;

namespace Kanojo_AssetDecDL
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Root = Environment.CurrentDirectory;
        public static string Respath = String.Empty;
        public static string Unzippath = String.Empty;
        public static int TotalCount = 0;
        public static int glocount = 0;
        public static string ServerURL = "http://kanojo-cdn.sunnyjpn.com/hotzip/1.0.962/";
        public static List<string> log = new List<string>();
    }
}
