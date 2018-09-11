using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Credential_Manager_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // Application is running
            // Process command line args
            var mw = new MainWindow();
            bool launchCertSelectorAsUser = false;
            bool launchCertSelectorAsMachine = false;
            bool launchNewCert = false;
            Dictionary<string, object> newCertArgs = null;
            for (int i = 0; i < e.Args.Length; i++)
            {
                string arg = e.Args[i];
                if (arg.StartsWith("/help", StringComparison.OrdinalIgnoreCase) || arg.Contains("?"))
                {
                    var help = new Help();
                    help.ShowDialog();
                    this.Shutdown();
                }
                else if (arg.StartsWith("/LaunchCertSelector", StringComparison.OrdinalIgnoreCase))
                {
                    //switch (arg.E(":machine", StringComparison.OrdinalIgnoreCase) >= 0)
                    switch (arg.EndsWith(":Machine", StringComparison.OrdinalIgnoreCase))
                    {
                        case true:
                            if (Credential_Manager_App.MainWindow.IsAdministrator())
                            {
                                launchCertSelectorAsMachine = true;
                            }
                            else
                            {
                                MessageBox.Show("To launch into machine context, " + Environment.NewLine + "you must \"Run As Administrator\".",
                                    "ERROR!", MessageBoxButton.OK, MessageBoxImage.Error);
                                this.Shutdown();
                            }
                            break;
                        case false:
                            launchCertSelectorAsUser = true;
                            break;
                    }
                }
                else if (arg.StartsWith("/LaunchNewCert"))
                {
                    launchNewCert = true;
                    if (arg.Contains(":"))
                    {
                        string backPor = (arg.Split(new string[1] { ":" }, StringSplitOptions.RemoveEmptyEntries))[1];
                        string[] lncArgs = backPor.Split(new string[1] { "," }, StringSplitOptions.None);
                        if (lncArgs.Length >= 3 && lncArgs.Length < 6)
                        {
                            string useme = null;
                            if (lncArgs.Length == 5)
                            {
                                useme = lncArgs[4];
                            }
                            newCertArgs = new Dictionary<string, object>()
                            {
                                { "Subject", lncArgs[0] },
                                { "FriendlyName", lncArgs[1] },
                                { "SavedAlgorithm", lncArgs[2] },
                                { "SavedKeyLength", lncArgs[3] },
                                { "SavedExpiration", useme }
                            };
                        }
                        else
                        {
                            var help = new Help();
                            help.ShowDialog();
                            this.Shutdown();
                        }
                    }
                }
            }

            // Create Main Application Window based on arguments
            
            mw.Show();
            if (e.Args.Length > 0)
            {
                if (launchCertSelectorAsMachine)
                {
                    mw.CertSelectorThread(true);
                }
                else if (launchCertSelectorAsUser)
                {
                    mw.CertSelectorThread(false);
                }
                if (launchNewCert)
                {
                    mw.CallNewCert(newCertArgs);
                }
            }
        }
    }
}
