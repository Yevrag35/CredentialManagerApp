using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        internal CertListItem SelectedCert;
        private ObservableCollection<CertListItem> certCol;
        public CertSelector()
        {
            certCol = Encryption.GetInstalledCerts(StoreLocation.CurrentUser);
            InitializeComponent();
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

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CertListItem selectedCert = listOCerts.SelectedItem as CertListItem;
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
            X509Certificate2 cert = SharedPrompt.PfxPrompt();
            if (cert != null)
            {
                CertListItem certListItem = new CertListItem(cert);
                CommitToCert(certListItem);
            }
        }
    }
}
