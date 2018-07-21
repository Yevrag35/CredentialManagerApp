using MG;
using Ookii.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
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
        public string AppNameAndVersion
        {
            get
            {
                AppDomain appId = AppDomain.CurrentDomain;
                string ver = FileVersionInfo.GetVersionInfo(appId.BaseDirectory + appId.FriendlyName).FileVersion;

                return appId.FriendlyName.Replace(".exe", String.Empty) + " - v" + ver;
            }
            set
            {
                AppNameAndVersion = value;
            }
        }
        public object StoredCert;
        public Encryption _enc;
        private FontFamily _defFont;
        private string _defu;
        private string un;
        private string ps;

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
            StoredCert = _app.Properties["EncryptionCertificate"];
            _defu = defUserText;
            _enc = new Encryption();
            InitializeComponent();
            if (!String.IsNullOrEmpty((string)StoredCert))
            {
                CertificateText = (string)StoredCert;
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
        private Dictionary<string, object> AllInfoPresent(TabItem ti)
        {
            bool result = false;
            string stillNeed;
            // First check if Certificate is defined...
            if (activeThumbprintBox != null && activeThumbprintBox.Text != defCertText)
            {
                // Now, check if the requisite text boxes are filled in...
                stillNeed = "Username or Password";
                string gridName = ti.Uid;
                Grid gotcha = (Grid)FindName(gridName);
                List<UIElement> allElms = gotcha.Children.OfType<UIElement>().Cast<UIElement>().ToList();
                IEnumerable<UIElement> gridBoxes = allElms.Where(x => x.Uid.Contains("gridTextBoxInput"));
                for (int i = 0; i < gridBoxes.Count(); i++)
                {
                    UIElement item = gridBoxes.ToList()[i];
                    Type itemType = item.GetType();
                    if (itemType == typeof(TextBox) && !String.IsNullOrEmpty(((TextBox)item).Text)
                        && ((TextBox)item).Text != defUserText)
                    {
                        result = true;
                    }
                    else if (itemType == typeof(PasswordBox) && !String.IsNullOrEmpty(((PasswordBox)item).Password))
                    {
                        result = true;
                    }
                }
            }
            else
            {
                stillNeed = "Certificate";
            }
            return new Dictionary<string, object>()
            {
                { "Result", result },
                { "FailedItems", stillNeed }
            };
        }

        //private enum EncryptButtonStatus : int
        //{
        //    Off = 0,
        //    On = 1
        //}

        //private void ChangeEncryptButtonStatus(EncryptButtonStatus status)
        //{
        //    if (encryptBtn != null)
        //    {
        //        switch (status)
        //        {
        //            case EncryptButtonStatus.Off:
        //                encryptBtn.IsDefault = false;
        //                encryptBtn.IsEnabled = false;
        //                selectCertBtn.IsDefault = true;
        //                break;
        //            case EncryptButtonStatus.On:
        //                encryptBtn.IsEnabled = true;
        //                selectCertBtn.IsDefault = false;
        //                encryptBtn.IsDefault = true;
        //                break;
        //        }
        //    }
        //}

        private void SaveCertThumbprint(string thumbprint)
        {
            _app.SetPropertyValue("EncryptionCertificate", thumbprint);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_enc.ActiveThumbprint.Equals(StoredCert))
            {
                MessageBoxResult question = MessageBox.Show("Would you like to save the current certificate settings?", "Before you go...", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (question == MessageBoxResult.Yes)
                {
                    SaveCertThumbprint(_enc.ActiveThumbprint);
                }
                else if (question == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
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

        private Dictionary<string, object> ResetFields(int tabIndex)
        {
            switch (tabIndex)
            {
                case 0:
                    inputPlainUserName.Text = String.Empty;
                    inputPlainPasswordBox.Password = String.Empty;
                    outputHashUserName.Text = String.Empty;
                    outputHashPassword.Text = String.Empty;
                    return new Dictionary<string, object>()
                    {
                        { "Content", "ENCRYPT" },
                        { "Behavior", EncryptButtonBehavior.Encrypt }
                    };
                case 1:
                    inputHashUserName.Text = String.Empty;
                    inputHashPassword.Text = String.Empty;
                    outputPlainUserName.Text = String.Empty;
                    outputPass.Password = String.Empty;
                    return new Dictionary<string, object>()
                    {
                        { "Content", "DECRYPT" },
                        { "Behavior", EncryptButtonBehavior.Decrypt }
                    };
                default:
                    return null;
            }
        }
        private void EncryptBtn_Click(object sender, RoutedEventArgs e)
        {
            Button eb = (Button)sender;
            Dictionary<string, object> Check = AllInfoPresent((TabItem)tabControl.SelectedItem);
            if (Check["Result"].Equals(true))
            {
                switch (eb.CommandParameter)
                {
                    case EncryptButtonBehavior.Encrypt:
                        EncryptButton_Encrypt(eb);
                        break;
                    case EncryptButtonBehavior.Reset:
                        EncryptButton_Reset(eb, tabControl.SelectedIndex);
                        break;
                    case EncryptButtonBehavior.Decrypt:
                        EncryptButton_Decrypt(eb);
                        break;
                }
            }
            else
            {
                MessageBox.Show("Some fields are still blank!  Populate the following fields and try again." +
                    Environment.NewLine + Environment.NewLine +
                    Check["FailedItems"], "WOAH!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
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
            if (!String.IsNullOrEmpty(ps))
            {
                outputHashPassword.Text = _enc.EncryptStringToString(ps);
            }
            if (!String.IsNullOrEmpty(un))
            {
                outputHashUserName.Text = _enc.EncryptStringToString(un);
            }
            eb.Content = "RESET";
            eb.CommandParameter = EncryptButtonBehavior.Reset;
        }
        private void EncryptButton_Decrypt(object sender)
        {
            Button eb = (Button)sender;
            try
            {
                if (!String.IsNullOrEmpty(inputHashUserName.Text))
                {
                    un = _enc.PlainDecrypt(inputHashUserName.Text);
                }
                if (!String.IsNullOrEmpty(inputHashPassword.Text))
                {
                    ps = _enc.PlainDecrypt(inputHashPassword.Text);
                }
            }
            catch (Exception e)
            { 
                MessageBox.Show(e.Message, e.GetType().FullName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!String.IsNullOrEmpty(un))
            {
                outputHashUserName.Text = un;
                un = null;
            }
            if (!String.IsNullOrEmpty(ps))
            {
                outputHashPassword.Text = ps;
                ps = null;
            }
            eb.Content = "RESET";
            eb.CommandParameter = EncryptButtonBehavior.Reset;
        }
        private void EncryptButton_Reset(object sender, int selectedIndex)
        {
            Button eb = (Button)sender;
            // Do you shit here!
             Dictionary<string, object> result = ResetFields(selectedIndex);
            eb.Content = result["Content"];
            eb.CommandParameter = result["Behavior"];
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
        //private void UserNameBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    TextBox tb = (TextBox)sender;
        //    if (tb.Text != defUserText && !String.IsNullOrEmpty(tb.Text) && activeThumbprintBox.Text != defCertText)
        //    {
        //        ChangeEncryptButtonStatus(EncryptButtonStatus.On);
        //    }
        //    else
        //    {
        //        ChangeEncryptButtonStatus(EncryptButtonStatus.Off);
        //    }
        //}

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
        //private void PassBox_PasswordChanged(object sender, RoutedEventArgs e)
        //{
        //    PasswordBox pb = (PasswordBox)sender;
        //    if ((!String.IsNullOrEmpty(pb.Password) || !String.IsNullOrEmpty(inputPlainUserName.Text)) &&
        //        activeThumbprintBox.Text != defCertText)
        //    {
        //        ChangeEncryptButtonStatus(EncryptButtonStatus.On);
        //    }
        //    else
        //    {
        //        ChangeEncryptButtonStatus(EncryptButtonStatus.Off);
        //    }
        //}

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

        //private void Input_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    TextBox tb = (TextBox)sender;
        //    if (!String.IsNullOrEmpty(tb.Text) && !String.IsNullOrEmpty(activeThumbprintBox.Text))
        //    {
        //        ChangeEncryptButtonStatus(EncryptButtonStatus.On);
        //    }
        //}

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
                });
            }
        }

        private void clearCertificate_Click(object sender, RoutedEventArgs e)
        {
            CertificateText = String.Empty;
        }
        public void ViewCertificate_Click(object sender, RoutedEventArgs e)
        {
            CertListItem cert = new CertListItem(_enc.GetActiveCertificate());
            cert.ViewCertificate();
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
            Button pb = (Button)sender;
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
                    if (!String.IsNullOrEmpty(outputHashUserName.Text) || !String.IsNullOrEmpty(outputHashPassword.Text))
                    {
                        encryptBtn.Content = "RESET";
                        encryptBtn.CommandParameter = EncryptButtonBehavior.Reset;
                    }
                    else
                    {
                        encryptBtn.Content = "ENCRYPT";
                        encryptBtn.CommandParameter = EncryptButtonBehavior.Encrypt;
                    }
                    break;
                case "DecryptTab":
                    if (!String.IsNullOrEmpty(outputPlainUserName.Text) || !String.IsNullOrEmpty(outputPass.Password))
                    {
                        encryptBtn.Content = "RESET";
                        encryptBtn.CommandParameter = EncryptButtonBehavior.Reset;
                    }
                    else
                    {
                        encryptBtn.Content = "DECRYPT";
                        encryptBtn.CommandParameter = EncryptButtonBehavior.Decrypt;
                    }
                    break;
            }
            
        }

        #endregion
    }
}
