using MG;
using Ookii.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace Credential_Manager_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region MainWindow - Properties
        public AppSettings _app;
        public string StoredCert { get { return (string)_app.Properties["EncryptionCertificate"]; } }
        public Encryption _enc;
        private FontFamily _defFont;
        private string _defu;
        private const string defUserText = @"<Enter the username>";
        private const string defCertText = @"    <No Certificate Chosen>";
        private string CertificateText
        {
            get
            {
                if (String.IsNullOrEmpty(_enc.ActiveThumbprint))
                {
                    return defCertText;
                }
                else
                {
                    return _enc.ActiveThumbprint;
                }
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    activeThumbprintBox.Text = defCertText;
                    _enc.ClearCertificate();
                }
                else
                {
                    activeThumbprintBox.Text = value;
                    _enc.SetActiveCertificate(value);
                }
            }
        }
        public string DefaultUserNameText
        {
            get { return _defu; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    _defu = defUserText;
                }
                else
                {
                    _defu = value;
                }
            }
        }

        #endregion

        #region MainWindow - Constructor
        public MainWindow()
        {
            _app = new AppSettings(GenerateSettingsDictionary());
            _defu = defUserText;
            _enc = new Encryption();
            InitializeComponent();
            if (!String.IsNullOrEmpty(StoredCert))
            {
                CertificateText = StoredCert;
                ChangeEncryptButtonStatus(EncryptButtonStatus.On);
                ChangeCertTextStyle(CertBoxStyles.Good, ((MainWindow)Application.Current.MainWindow).activeThumbprintBox);
            }
            LoadWindowDimensions();
            _defFont = FontFamily;
        }

        #endregion

        #region MainWindow - Methods
        private Dictionary<string, object> GenerateSettingsDictionary()
        {
            Dictionary<string, object> setts = new Dictionary<string, object>()
            {
                { "RegKey", "CredentialManagerApp" },
                { "EncryptionCertificate", String.Empty },
                { "WindowX", 800 },
                { "WindowY", 450 }
            };
            return setts;
        }

        private enum EncryptButtonStatus : int
        {
            Off = 0,
            On = 1
        }

        private void ChangeEncryptButtonStatus(EncryptButtonStatus status)
        {
            switch (status)
            {
                case EncryptButtonStatus.Off:
                    encryptBtn.IsDefault = false;
                    encryptBtn.IsEnabled = false;
                    selectCertBtn.IsDefault = true;
                    break;
                case EncryptButtonStatus.On:
                    encryptBtn.IsEnabled = true;
                    selectCertBtn.IsDefault = false;
                    encryptBtn.IsDefault = true;
                    break;
            }
        }

        private void SaveCertThumbprint(string thumbprint)
        {
            _app.SetPropertyValue("EncryptionCertificate", thumbprint);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!String.IsNullOrEmpty(_enc.ActiveThumbprint) && _enc.ActiveThumbprint != StoredCert)
            {
                MessageBoxResult question = MessageBox.Show("Would you like to save the current certificate for next time?", "Before you go...", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (question == MessageBoxResult.Yes)
                {
                    SaveCertThumbprint(_enc.ActiveThumbprint);
                }
            }
            SaveWindowDimensions();
        }

        #endregion

        #region MainWindow - Dimension Operations
        private void Window_StateChanged(object sender, EventArgs e)
        {
            MainWindow mw = (MainWindow)sender;
            if (mw.WindowState == WindowState.Maximized)
            {
                mw.WindowState = WindowState.Normal;
                mw.Height = 450;
                mw.Width = 800;
            }
        }

        private void LoadWindowDimensions()
        {
            Height = (int)_app.Properties["WindowY"];
            Width = (int)_app.Properties["WindowX"];
        }
        private void SaveWindowDimensions()
        {
            _app.SetPropertyValue("WindowX", (int)ActualWidth);
            _app.SetPropertyValue("WindowY", (int)ActualHeight);
        }

        #endregion

        #region Enter Credential Button
        private void inputPlainPasswordBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (encryptBtn.IsEnabled)
            {
                Dictionary<string, object> credResult = GetCredential();
                if (credResult != null && credResult["Result"].Equals(true))
                {
                    // Do your shit!
                    string base64 = _enc.EncryptStringToString(credResult["Password"]);
                    outputHashPassword.Text = base64;
                }
            }
        }

        private Dictionary<string, object> GetCredential()
        {
            CredentialDialog credDiag = new CredentialDialog()
            {
                MainInstruction = "Type the username and password to store:",
                Target = "Credential_Manager_App",
                ShowSaveCheckBox = true,
                ShowUIForSavedCredentials = true,
                WindowTitle = "Credential Manager App"
            };
            if (!String.IsNullOrEmpty(inputPlainUserName.Text))
            {
                //credDiag.
            }
            Dictionary<string, object> dict = new Dictionary<string, object>();
            SecureString ss = new SecureString();
            System.Windows.Forms.DialogResult diagResult = credDiag.ShowDialog();
            bool result = diagResult.Equals(System.Windows.Forms.DialogResult.OK);
            if (result)
            {
                foreach (char c in credDiag.Password)
                {
                    ss.AppendChar(c);
                }
                dict.Add("Result", result);
                dict.Add("Username", credDiag.UserName);
                dict.Add("Password", ss);
                return dict;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Certificate Field and Button Behavior

        private enum CertBoxStyles : int
        {
            Bad = 0,
            Good = 1
        }
        private void ChangeCertTextStyle(CertBoxStyles style, TextBox cb)
        {
            switch (style)
            {
                case CertBoxStyles.Bad:
                    cb.FontStyle = FontStyles.Italic;
                    cb.FontWeight = FontWeights.Thin;
                    break;
                case CertBoxStyles.Good:
                    cb.FontStyle = FontStyles.Normal;
                    cb.FontWeight = FontWeights.Normal;
                    break;
            }
            
        }
        private void activeThumbprintBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text == defCertText)
            {
                ChangeCertTextStyle(CertBoxStyles.Bad, tb);
            }
            else
            {
                ChangeCertTextStyle(CertBoxStyles.Good, tb);
            }
        }

        #endregion

        #region UserName TextBox Behavior
        private void inputPlainUserName_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            inputPlainUserName.HorizontalContentAlignment = HorizontalAlignment.Left;
            if (inputPlainUserName.FontStyle == FontStyles.Italic)
            {
                inputPlainUserName.Text = String.Empty;
                inputPlainUserName.FontStyle = FontStyles.Normal;
                inputPlainUserName.FontFamily = (FontFamily)Resources["activeFont"];
                inputPlainUserName.FontSize = 14;
                inputPlainUserName.FontWeight = FontWeights.Normal;
            }
            else
            {
                inputPlainUserName.SelectAll();
            }
        }
        private void inputPlainUserName_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox unbox = (TextBox)sender;
            unbox.HorizontalContentAlignment = HorizontalAlignment.Center;
            if (String.IsNullOrEmpty(unbox.Text))
            {
                unbox.FontStyle = FontStyles.Italic;
                unbox.FontFamily = _defFont;
                unbox.FontSize = 13;
                unbox.FontWeight = FontWeights.Thin;
            }
        }
        private void inputPlainUserName_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!inputPlainUserName.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                inputPlainUserName.Focus();
            }
        }

        #endregion

        #region Password Box Behavior
        private void inputPlainPasswordBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            PasswordBox pb = (PasswordBox)sender;
            if (!String.IsNullOrEmpty(pb.Password))
            {
                pb.SelectAll();
            }
        }
        private void inputPlainPasswordBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PasswordBox pb = (PasswordBox)sender;
            if (!pb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                pb.Focus();
            }
        }

        #endregion

        #region Find Certificate Button Behavior
        private void selectCertBtn_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog fileDiag = new VistaOpenFileDialog()
            {
                ShowHelp = true,
                Filter = "Certificate files (*.cer;*.crt;*.pem)|*.cer;*.crt;*.pem|CER files (*.cer)|*.cer|CRT files (*.crt)|*.crt|PEM files (*.pem)|*.pem|All files (*.*)|*.*",
                InitialDirectory = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop",
                Multiselect = false,
                Title = "Select an encryption certificate"
            };
            if (fileDiag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string thumb = _enc.GetCertificateThumbprint(fileDiag.FileName);
                CertificateText = thumb;
            }
        }


        #endregion

        #region CertBox Right-Click Menu
        private void chooseInstalledCert_Click(object sender, RoutedEventArgs e)
        {
            Thread cThread = new Thread(OpenCertSelector)
            {
                Name = "CertSelector",
                IsBackground = true
            };
            cThread.SetApartmentState(ApartmentState.STA);
            cThread.Start();
        }

        private void OpenCertSelector()
        {
            CertSelector certSelector = new CertSelector();
            bool? result = certSelector.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Dispatcher.Invoke(() =>
                {
                    CertificateText = certSelector.SelectedCert.SHA1Thumbprint;
                    ChangeEncryptButtonStatus(EncryptButtonStatus.On);
                });
            }
        }

        private void clearCertificate_Click(object sender, RoutedEventArgs e)
        {
            ChangeEncryptButtonStatus(EncryptButtonStatus.Off);
            _app.SetPropertyValue("EncryptionCertificate", String.Empty);
            CertificateText = String.Empty;
        }




        #endregion

        #region HashBox Right-Click Menu
        private void selectAll_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)e.Source;
            ContextMenu ctxMenu = (ContextMenu)mi.Parent;
            TextBox tb = (TextBox)ctxMenu.PlacementTarget;
            tb.SelectAll();
        }

        #endregion

        #region Copy to Clipboard Button Behavior
        private void CopyBtnClip_Click(object sender, RoutedEventArgs e)
        {
            Button but = (Button)sender;
            var getme = encAreaGrid.Children.OfType<UIElement>().ToList();
            IEnumerable<TextBox> getBoxes = getme.Where(x => x.Uid == "gridTextBox").Cast<TextBox>();
            TextBox gotcha = getBoxes.Single(x => x.Name == but.Uid);

            Clipboard.SetText(gotcha.Text);
        }

        #endregion
    }
}
