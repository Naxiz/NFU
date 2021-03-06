﻿using Nfu.Properties;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Nfu
{
    public enum TransferType
    {
        Ftp,
        FtpsExplicit,
        Sftp,
        SftpKeys,
        Cifs
    }

    static class Program
    {
        public static Core FormCore;
        public static About FormAbout;
        public static Cp FormCp;

        public static bool AutoSaveSettings = true;

        private static readonly Mutex Mutex = new Mutex(true, "NFU {537e6f56-11cb-461a-9983-634307543f5b}");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Uncomment the following lines to change the UI culture in order to test translations
            //Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("nl-NL");
            //Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("nl-NL");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Use TLS 1.2, TLS 1.1 and TLS 1.0
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // Automatically save settings on change
            Settings.Default.PropertyChanged += (sender, e) =>
            {
                if (AutoSaveSettings)
                {
                    Settings.Default.Save();
                }
            };

            try
            {
                if (args.Any(a => a == "debug"))
                {
                    if (!Settings.Default.Debug)
                    {
                        try
                        {
                            if (!EventLog.SourceExists(Resources.AppName))
                            {
                                EventLog.CreateEventSource(Resources.AppName, "Application");
                            }

                            Settings.Default.Debug = true;
                        }
                        catch { }
                    }

                    return;
                }

                if (Mutex.WaitOne(TimeSpan.Zero, true))
                {
                    if (Settings.Default.NeedsUpgrade)
                    {
                        Settings.Default.Upgrade();
                        Settings.Default.NeedsUpgrade = false;
                    }

                    if (Screen.AllScreens.Length > Settings.Default.Screen - 1)
                        Settings.Default.Screen = 0;

                    FormCore = new Core();
                    FormAbout = new About();
                    FormCp = new Cp();

                    FormCore.Setup();

                    if (Settings.Default.FirstRun)
                    {
                        FormCore.Load += (sender, e) =>
                        {
                            FormCp.ShowDialog();
                        };
                    }

                    if (args.Any(a => a == "minimized"))
                    {
                        FormCore.WindowState = FormWindowState.Minimized;
                    }

                    Application.Run(FormCore);
                }
                else
                {
                    MessageBox.Show(Resources.AlreadyRunning,
                        Resources.AlreadyRunningTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
