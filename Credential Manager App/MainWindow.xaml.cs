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
        private bool check;
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
                ChangeCertTextStyle(CertBoxStyles.Good, ((MainWindow)Application.Current.MainWindow).activeThumbprintBox);
            }
            LoadWindowDimensions();
            _defFont = FontFamily;
            encryptBtn.CommandParameter = EncryptButtonBehavior.Encrypt;
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
        private bool AllInfoPresent(TabItem ti)
        {
            switch (ti.Name)
            {
                case "EncryptTab":
                    if (String.IsNullOrEmpty(inputPlainPasswordBox.Password) || String.IsNullOrEmpty(inputPlainUserName.Text) ||
                        String.IsNullOrEmpty(outputHashPassword.Text) || String.IsNullOrEmpty(outputHashUserName.Text))
                    {
                        check = false;
                    }
                    else if (String.IsNullOrEmpty(activeThumbprintBox.Text))
                    {
                        check = false;
                    }
                    else
                    {
                        check = true;
                    }
                    break;
                case "DecryptTab":
                    if (String.IsNullOrEmpty(inputHashPassword.Text) || String.IsNullOrEmpty(inputHashUserName.Text) ||
                        String.IsNullOrEmpty(outputPlainUserName.Text) || String.IsNullOrEmpty(outputPass.Password))
                    {
                        check = false;
                    }
                    else if (String.IsNullOrEmpty(activeThumbprintBox.Text))
                    {
                        check = false;
                    }
                    else
                    {
                        check = true;
                    }
                    break;
            }
            bool localChck = check;
            check = false;
            return localChck;
        }

        private enum EncryptButtonStatus : int
        {
            Off = 0,
            On = 1
        }

        private void ChangeEncryptButtonStatus(EncryptButtonStatus status)
        {
            if (encryptBtn != null)
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
            if (encryptBtn.CommandParameter.Equals(EncryptButtonBehavior.Encrypt))
            {
                Dictionary<string, object> credResult = GetCredential();
                if (credResult != null && credResult["Result"].Equals(true))
                {
                    EncryptButton_Encrypt(encryptBtn, credResult);
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

        #region Encrypt Button Behavior
        private enum EncryptButtonBehavior : int
        {
            Encrypt = 0,
            Reset = 1,
            Decrypt = 2
        }

        private void EncryptBtn_Click(object sender, RoutedEventArgs e)
        {
            Button eb = (Button)sender;
            switch (eb.CommandParameter)
            {
                case EncryptButtonBehavior.Encrypt:
                    EncryptButton_Encrypt(eb);
                    break;
                case EncryptButtonBehavior.Reset:
                    EncryptButton_Reset(eb, tabControl);
                    break;
                case EncryptButtonBehavior.Decrypt:
                    EncryptButton_Decrypt(eb);
                    break;
            }
        }
        private void EncryptButton_Encrypt(object sender, Dictionary<string, object> credResult = null)
        {
            Button eb = (Button)sender;
            // Do you shit here!
            string un;
            string ps;
            if (credResult != null)
            {
                un = (string)credResult["UserName"];
                ps = (string)credResult["Password"];
                inputPlainPasswordBox.Password = ps;
            }
            else
            {
                un = inputPlainUserName.Text;
                ps = inputPlainPasswordBox.Password;
            }
            string base64 = _enc.EncryptStringToString(ps);
            string base64User = _enc.EncryptStringToString(un);
            outputHashPassword.Text = base64;
            inputPlainUserName.Text = un;
            outputHashUserName.Text = base64User;
            eb.Content = "RESET";
            eb.CommandParameter = EncryptButtonBehavior.Reset;
        }
        private void EncryptButton_Decrypt(object sender)
        {
            Button eb = (Button)sender;
            outputPlainUserName.Text = _enc.PlainDecrypt(inputHashUserName.Text);
            outputPass.Password = _enc.PlainDecrypt(inputHashPassword.Text);
            if (!String.IsNullOrEmpty(outputPass.Password) && !String.IsNullOrEmpty(outputPlainUserName.Text))
            {
                eb.Content = "RESET";
                eb.CommandParameter = EncryptButtonBehavior.Reset;
            }
        }
        private void EncryptButton_Reset(object sender, TabControl tbc)
        {
            Button eb = (Button)sender;
            // Do you shit here!
            switch (tbc.SelectedIndex)
            {
                case 0:
                    inputPlainUserName.Text = String.Empty;
                    inputPlainPasswordBox.Password = String.Empty;
                    outputHashUserName.Text = String.Empty;
                    outputHashPassword.Text = String.Empty;
                    eb.Content = "ENCRYPT";
                    eb.CommandParameter = EncryptButtonBehavior.Encrypt;
                    eb.IsEnabled = false;
                    break;
                case 1:
                    inputHashUserName.Text = String.Empty;
                    inputHashPassword.Text = String.Empty;
                    outputPlainUserName.Text = String.Empty;
                    outputPass.Password = String.Empty;
                    eb.Content = "DECRYPT";
                    eb.CommandParameter = EncryptButtonBehavior.Decrypt;
                    break;
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
                ChangeEncryptButtonStatus(EncryptButtonStatus.Off);
                ChangeCertTextStyle(CertBoxStyles.Bad, tb);
            }
            else
            {
                ChangeCertTextStyle(CertBoxStyles.Good, tb);
            }
        }

        #endregion

        #region Input Plain UserName TextBox Behavior
        private void inputPlainUserName_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.HorizontalContentAlignment = HorizontalAlignment.Left;
            if (tb.FontStyle == FontStyles.Italic)
            {
                tb.Text = String.Empty;
                tb.FontStyle = FontStyles.Normal;
                tb.FontFamily = (FontFamily)Resources["activeFont"];
                tb.FontSize = 14;
                tb.FontWeight = FontWeights.Normal;
            }
            else
            {
                tb.SelectAll();
            }
        }
        private void inputPlainUserName_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox unbox = (TextBox)sender;
            if (String.IsNullOrEmpty(unbox.Text))
            {
                unbox.HorizontalContentAlignment = HorizontalAlignment.Center;
                unbox.FontStyle = FontStyles.Italic;
                unbox.FontFamily = _defFont;
                unbox.FontSize = 13;
                unbox.FontWeight = FontWeights.Thin;
            }
        }
        private void inputPlainUserName_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (!tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
            }
        }
        private void UserNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text != defUserText && !String.IsNullOrEmpty(inputPlainPasswordBox.Password) &&
                activeThumbprintBox.Text != defCertText)
            {
                ChangeEncryptButtonStatus(EncryptButtonStatus.On);
            }
            else
            {
                ChangeEncryptButtonStatus(EncryptButtonStatus.Off);
            }
        }

        #endregion

        #region Input Plain Password Box Behavior
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
        private void PassBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox pb = (PasswordBox)sender;
            if (!String.IsNullOrEmpty(pb.Password) && !String.IsNullOrEmpty(inputPlainUserName.Text) &&
                activeThumbprintBox.Text != defCertText)
            {
                ChangeEncryptButtonStatus(EncryptButtonStatus.On);
            }
            else
            {
                ChangeEncryptButtonStatus(EncryptButtonStatus.Off);
            }
        }

        #endregion

        #region Input Hash UserName TextBox Behavior
        private void InputHash_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (!tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
            }
        }
        private void InputHash_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (!String.IsNullOrEmpty(tb.Text))
            {
                tb.SelectAll();
            }
        }

        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (!String.IsNullOrEmpty(tb.Text))
            {
                if (!String.IsNullOrEmpty(inputHashUserName.Text) && !String.IsNullOrEmpty(inputHashPassword.Text))
                {
                    ChangeEncryptButtonStatus(EncryptButtonStatus.On);
                }
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
                    if (!String.IsNullOrEmpty(inputPlainPasswordBox.Password) && !String.IsNullOrEmpty(inputPlainUserName.Text))
                    {
                        ChangeEncryptButtonStatus(EncryptButtonStatus.On);
                    }
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
            foreach (UIElement item in decAreaGrid.Children.OfType<UIElement>().ToList())
            {
                getme.Add(item);
            }
            IEnumerable<TextBox> getBoxes = getme.Where(x => x.Uid == "gridTextBox").Cast<TextBox>();
            TextBox gotcha = getBoxes.Single(x => x.Name == but.Uid);

            Clipboard.SetText(gotcha.Text);
        }
        private void passBoxCopyBtn_Click(object sender, RoutedEventArgs e)
        {
            PasswordBox pb = (PasswordBox)sender;
            var getme = encAreaGrid.Children.OfType<UIElement>().ToList();
            foreach (UIElement item in decAreaGrid.Children.OfType<UIElement>().ToList())
            {
                getme.Add(item);
            }
            IEnumerable<PasswordBox> getPBs = getme.Where(x => x.Uid == "passDecrypt").Cast<PasswordBox>();
            PasswordBox gotcha = getPBs.Single(x => x.Name == pb.Uid);

            Clipboard.SetText(gotcha.Password);
        }

        #endregion

        #region Tab Control Selection Changed
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            TabItem ti = (TabItem)e.AddedItems[0];
            switch (ti.Name)
            {
                case "EncryptTab":
                    if (!AllInfoPresent(ti))
                    {
                        if (String.IsNullOrEmpty(inputPlainPasswordBox.Password) || String.IsNullOrEmpty(inputPlainUserName.Text))
                        {
                            encryptBtn.IsEnabled = false;
                        }
                        else
                        {
                            ChangeEncryptButtonStatus(EncryptButtonStatus.On);
                        }
                        encryptBtn.Content = "ENCRYPT";
                        encryptBtn.CommandParameter = EncryptButtonBehavior.Encrypt;
                    }
                    else
                    {
                        ChangeEncryptButtonStatus(EncryptButtonStatus.On);
                        encryptBtn.Content = "RESET";
                        encryptBtn.CommandParameter = EncryptButtonBehavior.Reset;
                    }
                    break;
                case "DecryptTab":
                    if (!AllInfoPresent(ti))
                    {
                        if (String.IsNullOrEmpty(inputHashPassword.Text) || String.IsNullOrEmpty(inputHashUserName.Text))
                        {
                            ChangeEncryptButtonStatus(EncryptButtonStatus.Off);
                        }
                        else
                        {
                            ChangeEncryptButtonStatus(EncryptButtonStatus.On);
                        }
                        encryptBtn.Content = "DECRYPT";
                        encryptBtn.CommandParameter = EncryptButtonBehavior.Decrypt;
                    }
                    else
                    {
                        ChangeEncryptButtonStatus(EncryptButtonStatus.On);
                        encryptBtn.Content = "RESET";
                        encryptBtn.CommandParameter = EncryptButtonBehavior.Reset;
                    }
                    break;
            }
            
        }

        #endregion
    }
}
