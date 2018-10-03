using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Credential_Manager_App
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class CertSelector : Window
    {
        private protected const string userToMachCtx = "Switch to Computer";
        private protected const string machToUserCtx = "Switch to User";
        private CertContext Context { get; set; }
        public StoreLocation usingStore { get; set; }

        internal bool Prompt = false;

        internal CertListItem SelectedCert;
        private ObservableCollection<CertListItem> certCol;
        public CertSelector(bool machineContext = false)
        {
            InitializeComponent();
            Context = !machineContext ? CertContext.User : CertContext.Machine;
            ChangeContextButton(Context);
        }

        public enum CertContext
        {
            User = 1,
            Machine = 2
        }

        private void ChangeContextButton(CertContext flip)
        {
            Context = flip;
            switch (flip)
            {
                case CertContext.User:
                    if (!Credential_Manager_App.MainWindow.IsAdministrator())
                    {
                        uacShield.IsEnabled = true;
                        uacShield.Visibility = Visibility.Visible;
                    }
                    switchContextBtn.Content = userToMachCtx;
                    certCol = Encryption.GetInstalledCerts(StoreLocation.CurrentUser);
                    usingStore = StoreLocation.CurrentUser;
                    break;
                case CertContext.Machine:
                    uacShield.Visibility = Visibility.Hidden;
                    uacShield.IsEnabled = false;
                    switchContextBtn.Content = machToUserCtx;
                    certCol = Encryption.GetInstalledCerts(StoreLocation.LocalMachine);
                    usingStore = StoreLocation.LocalMachine;
                    break;
            }
            listOCerts.ItemsSource = certCol;
        }

        #region Button Interactions
        private void useBtn_Click(object sender, RoutedEventArgs e)
        {
            object selItem = listOCerts.SelectedItem;
            if (selItem == null)
            {
                MessageBox.Show("You must select/highlight 1 certificate to proceed.", "ERROR!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            CommitToCert(selItem as CertListItem);
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedCert = listOCerts.SelectedItem as CertListItem;
            selectedCert.ViewCertificate();
        }

        private void CommitToCert(CertListItem cert)
        {
            SelectedCert = cert;
            DialogResult = true;
            Close();
        }

        private void pfxInstallerBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (Context)
            {
                case CertContext.User:
                    usingStore = StoreLocation.CurrentUser;
                    break;
                case CertContext.Machine:
                    usingStore = StoreLocation.LocalMachine;
                    break;
            }
            X509Certificate2 cert = SharedPrompt.PfxPrompt(usingStore);
            if (cert != null)
            {
                var certListItem = new CertListItem(cert);
                CommitToCert(certListItem);
            }
        }

        private void switchContextBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (Context)
            {
                case CertContext.User:
                    if (MainWindow.IsAdministrator())
                    {
                        ChangeContextButton(CertContext.Machine);
                    }
                    else
                    {
                        Prompt = true;
                        Close();
                    }
                    break;
                case CertContext.Machine:
                    ChangeContextButton(CertContext.User);
                    break;
            }
        }

        private void CertSelector_Closing(object sender, CancelEventArgs e)
        {
            // Prompt for elevation
            if (Prompt)
            {
                string exe = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;
                var procInfo = new ProcessStartInfo(exe, "/LaunchCertSelector:machine")
                {
                    Verb = "RunAs",
                    CreateNoWindow = false
                };
                try
                {
                    var proc = Process.Start(procInfo);
                }
                catch (Win32Exception ex)   // In case the answer is 'No' to the UAC prompt.
                {
                    Prompt = false;
                    if (ex.Message == "The operation was canceled by the user")
                    {
                        e.Cancel = true;
                        return;
                    }
                    else
                    {
                        throw new Win32Exception("Woah!", ex);
                    }
                }
            }
        }

        private void viewCertMI_Click(object sender, RoutedEventArgs e)
        {
            var lvi = listOCerts.SelectedItem as CertListItem;
            lvi.ViewCertificate();
        }

        private void delCertMI_Click(object sender, RoutedEventArgs e)
        {
            var rusure = MessageBox.Show("Are you sure want to delete this certificate?" + Environment.NewLine + Environment.NewLine +
                "This action is permanent and final!", "DELETE CERTIFICATE?", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (rusure == MessageBoxResult.Yes)
            {
                var cli = listOCerts.SelectedItem as CertListItem;
                X509Certificate2 cert = cli.GetCertificate();
                using (var store = new X509Store(usingStore))
                {
                    store.Open(OpenFlags.OpenExistingOnly);
                    store.Remove(cert);
                }
            }
        }

        private void listOCerts_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            switch (listOCerts.SelectedItems.Count)
            {
                case 1:
                    viewCertMI.IsEnabled = true;
                    //delCertMI.IsEnabled = true;
                    break;
                default:
                    //delCertMI.IsEnabled = false;
                    viewCertMI.IsEnabled = false;
                    break;
            }
        }
    }
}
