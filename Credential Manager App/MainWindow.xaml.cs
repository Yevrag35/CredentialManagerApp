using MG;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Credential_Manager_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region MainWindow - Fields/Properties
        internal AppSettings _app;
        public string AppNameAndVersion
        {
            get
            {
                AppDomain appId = AppDomain.CurrentDomain;
                string ver = FileVersionInfo.GetVersionInfo(appId.BaseDirectory + appId.FriendlyName).FileVersion;

                return appId.FriendlyName.Replace(".exe", string.Empty) + " - v" + ver;
            }
            set => AppNameAndVersion = value;
        }
        private bool _clrcrten = false;
        public bool ClearCertEnabled
        {
            get => _clrcrten;
            set => _clrcrten = value;
        }
        internal bool Prompt = false;
        internal object StoredCert;
        internal Encryption _enc;
        private FontFamily _defFont;
        private string _defu;
        private string un;
        private string ps;
        public CertListItem selectedCert;

        public StoreLocation usingStore { get; set; }

        private const string defUserText = @"<Enter the username>";
        private const string defCertText = @"    <No Certificate Chosen>";
        public string CertificateText
        {
            get => string.IsNullOrEmpty(_enc.ActiveThumbprint) ? defCertText : _enc.ActiveThumbprint;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    activeThumbprintBox.Text = defCertText;
                    selectedCert = null;
                    _enc.ClearCertificate();
                }
                else
                {
                    activeThumbprintBox.Text = value;
                }
            }
        }
        internal string DefaultUserNameText
        {
            get => _defu;
            set => _defu = string.IsNullOrEmpty(value) ? defUserText : value;
        }

        #endregion

        #region MainWindow - Constructor
        public MainWindow()
        {
            _app = new AppSettings(GenerateSettingsDictionary());
            StoredCert = _app.Properties["EncryptionCertificate"];
            var cs = (int)_app.Properties["CertificateStore"];
            Array arr = typeof(StoreLocation).GetEnumValues();
            for (int i = 0; i < arr.Length; i++)
            {
                var sl = (StoreLocation)arr.GetValue(i);
                if (sl == (StoreLocation)cs)
                {
                    usingStore = sl;
                }
            }
            _defu = defUserText;
            _enc = new Encryption();
            selectedCert = _enc.TextToCLI((string)StoredCert, usingStore);
            InitializeComponent();
            // If certificate is missing, go back to default
            
            if (!string.IsNullOrEmpty((string)StoredCert) && selectedCert != null)
            {
                CertificateText = selectedCert.SHA1Thumbprint;
                ChangeCertTextStyle(CertBoxStyles.Good, ((MainWindow)Application.Current.MainWindow).activeThumbprintBox);
            }
            else if (selectedCert == null)
            {
                CertificateText = defCertText;
                ChangeCertTextStyle(CertBoxStyles.Bad, ((MainWindow)Application.Current.MainWindow).activeThumbprintBox);
            }
            LoadWindowDimensions();
            _defFont = FontFamily;
            encryptBtn.CommandParameter = EncryptButtonBehavior.Encrypt;
        }

        #endregion

        #region MainWindow - Methods
        private Dictionary<string, object> GenerateSettingsDictionary()
        {
            var setts = new Dictionary<string, object>()
            {
                { "RegKey", "CredentialManagerApp" },
                { "EncryptionCertificate", string.Empty },
                { "CertificateStore", 1 },
                { "WindowX", 800 },
                { "WindowY", 450 }
            };
            return setts;
        }
        private Dictionary<string, object> AllInfoPresent(TabItem ti)
        {
            bool result = false;
            Dictionary<string, Collection<UIElement>> stillNeed = new Dictionary<string, Collection<UIElement>>();
            var gridCol = new Collection<UIElement>();
            // Now, check if the requisite text boxes are filled in...
            string gridName = ti.Uid;
            var gotcha = (Grid)FindName(gridName);
            var allElms = gotcha.Children.OfType<UIElement>().Cast<UIElement>().ToList();
            IEnumerable<UIElement> gridBoxes = allElms.Where(x => x.Uid.Contains("gridTextBoxInput"));
            foreach (UIElement u in gridBoxes)
            {
                gridCol.Add(u);
            }
            for (int i = 0; i < gridBoxes.Count(); i++)
            {
                UIElement item = gridBoxes.ToList()[i];
                Type itemType = item.GetType();
                if (itemType == typeof(TextBox) && !string.IsNullOrEmpty(((TextBox)item).Text)
                    && ((TextBox)item).Text != defUserText)
                {
                    result = true;
                }
                else if (itemType == typeof(PasswordBox) && !string.IsNullOrEmpty(((PasswordBox)item).Password))
                {
                    result = true;
                }
            }
            // First check if Certificate is defined...
            if ((ti.Uid == "encAreaGrid" && string.IsNullOrEmpty(inputPlainUserName.Text) && string.IsNullOrEmpty(inputPlainPasswordBox.Password)) ||
                 (ti.Uid == "decAreaGrid" && string.IsNullOrEmpty(inputHashUserName.Text) && string.IsNullOrEmpty(inputHashPassword.Text)))
            {
                stillNeed.Add("Username or Password", gridCol);
            }
            if (activeThumbprintBox == null || activeThumbprintBox.Text == defCertText)
            {
                result = false;
                stillNeed.Add("Certificate", new Collection<UIElement>() { activeThumbprintBox });
            }
            return new Dictionary<string, object>()
            {
                { "Result", result },
                { "FailedItems", stillNeed }
            };
        }

        private void SaveCertThumbprint(string thumbprint) => _app.SetPropertyValue("EncryptionCertificate", thumbprint);

        private void exitBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!Prompt && !_enc.ActiveThumbprint.Equals(StoredCert))
            {
                MessageBoxResult question = MessageBox.Show("Would you like to save the current certificate settings?", "Before you go...", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (question == MessageBoxResult.Yes)
                {
                    SaveCertThumbprint(_enc.ActiveThumbprint);
                    _app.SetPropertyValue("CertificateStore", usingStore);
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

        #region Elevation
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        #endregion

        #region MainWindow - Dimension Operations
        private void Window_StateChanged(object sender, EventArgs e)
        {
            var mw = (MainWindow)sender;
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

        #region Graphics Methods
        private void BlinkColor(dynamic ele, Color color)
        {
            var ca = new ColorAnimation(color, Colors.White, new Duration(TimeSpan.FromMilliseconds(1500)));
            var solid = new SolidColorBrush(color);
            DependencyProperty dp = SolidColorBrush.ColorProperty;
            switch (ele.GetType().Name)
            {
                case "TextBox":
                    var tb = (TextBox)ele;
                    tb.Background = solid;
                    tb.Background.BeginAnimation(dp, ca);
                    break;
                case "PasswordBox":
                    var pb = (PasswordBox)ele;
                    pb.Background = solid;
                    pb.Background.BeginAnimation(dp, ca);
                    break;
                case "TextBlock":
                    var tbk = (TextBlock)ele;
                    tbk.Background = solid;
                    tbk.Background.BeginAnimation(dp, ca);
                    break;
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
                    inputPlainUserName.Text = string.Empty;
                    inputPlainPasswordBox.Password = string.Empty;
                    outputHashUserName.Text = string.Empty;
                    outputHashPassword.Text = string.Empty;
                    return new Dictionary<string, object>()
                    {
                        { "Content", "ENCRYPT" },
                        { "Behavior", EncryptButtonBehavior.Encrypt }
                    };
                case 1:
                    inputHashUserName.Text = string.Empty;
                    inputHashPassword.Text = string.Empty;
                    outputPlainUserName.Text = string.Empty;
                    outputPass.Password = string.Empty;
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
            var eb = (Button)sender;
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
                var failedItems = Check["FailedItems"] as Dictionary<string, Collection<UIElement>>;
                Color red = Colors.Red;
                string[] failures = failedItems.Keys.ToArray();
                MessageBox.Show("Some fields are still blank!  Populate the following fields and try again." +
                    Environment.NewLine + Environment.NewLine +
                    string.Join(" and ", failures), "WOAH!", MessageBoxButton.OK, MessageBoxImage.Error);
                for (int v = 0; v < failedItems.Count; v++)
                {
                    string key = failedItems.Keys.ToArray()[v];
                    Collection<UIElement> dictVal = failedItems[key];
                    for (int i = 0; i < dictVal.Count; i++)
                    {
                        UIElement uie = dictVal[i];
                        BlinkColor(uie, red);
                    }
                }
                return;
            }
        }

        private void EncryptButton_Encrypt(object sender, Dictionary<string, object> credResult = null)
        {
            var eb = (Button)sender;
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
            if (!string.IsNullOrEmpty(ps))
            {
                outputHashPassword.Text = _enc.EncryptStringToString(ps);
            }
            if (!string.IsNullOrEmpty(un))
            {
                outputHashUserName.Text = _enc.EncryptStringToString(un);
            }
            eb.Content = "RESET";
            eb.CommandParameter = EncryptButtonBehavior.Reset;
        }
        private void EncryptButton_Decrypt(object sender)
        {
            var eb = (Button)sender;
            try
            {
                if (!string.IsNullOrEmpty(inputHashUserName.Text))
                {
                    un = _enc.PlainDecrypt(inputHashUserName.Text);
                }
                if (!string.IsNullOrEmpty(inputHashPassword.Text))
                {
                    ps = _enc.PlainDecrypt(inputHashPassword.Text);
                }
            }
            catch (Exception e)
            { 
                MessageBox.Show(e.Message, e.GetType().FullName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!string.IsNullOrEmpty(un))
            {
                outputPlainUserName.Text = un;
                un = null;
            }
            if (!string.IsNullOrEmpty(ps))
            {
                outputPass.Password = ps;
                ps = null;
            }
            eb.Content = "RESET";
            eb.CommandParameter = EncryptButtonBehavior.Reset;
        }
        private void EncryptButton_Reset(object sender, int selectedIndex)
        {
            var eb = (Button)sender;
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
            if (selectedCert != null && _enc.ActiveThumbprint != selectedCert.SHA1Thumbprint)
            {
                _enc.SetActiveCertificate(selectedCert);
            }

            var specialBlue = Color.FromRgb(
                byte.Parse(Convert.ToString(51)),
                byte.Parse(Convert.ToString(102)),
                byte.Parse(Convert.ToString(153))
            );
            var tb = (TextBox)sender;
            if (tb.Text == defCertText)
            {
                ChangeContextMenuItemAvailability("clearCertificate", false);
                ChangeCertTextStyle(CertBoxStyles.Bad, tb);
            }
            else
            {
                ChangeContextMenuItemAvailability("clearCertificate", true);
                ChangeCertTextStyle(CertBoxStyles.Good, tb);
            }
            BlinkColor(tb, specialBlue);
        }

        #endregion

        #region Input Plain UserName TextBox Behavior
        private void inputPlainUserName_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            tb.HorizontalContentAlignment = HorizontalAlignment.Left;
            if (tb.FontStyle == FontStyles.Italic)
            {
                tb.Text = string.Empty;
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
            var unbox = (TextBox)sender;
            if (string.IsNullOrEmpty(unbox.Text))
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
            var tb = (TextBox)sender;
            if (!tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
            }
        }
        //private void UserNameBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    TextBox tb = (TextBox)sender;
        //    if (tb.Text != defUserText && !string.IsNullOrEmpty(tb.Text) && activeThumbprintBox.Text != defCertText)
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
            var pb = (PasswordBox)sender;
            if (!string.IsNullOrEmpty(pb.Password))
            {
                pb.SelectAll();
            }
        }
        private void inputPlainPasswordBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pb = (PasswordBox)sender;
            if (!pb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                pb.Focus();
            }
        }
        //private void PassBox_PasswordChanged(object sender, RoutedEventArgs e)
        //{
        //    PasswordBox pb = (PasswordBox)sender;
        //    if ((!string.IsNullOrEmpty(pb.Password) || !string.IsNullOrEmpty(inputPlainUserName.Text)) &&
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
            var tb = (TextBox)sender;
            if (!tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
            }
        }
        private void InputHash_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (!string.IsNullOrEmpty(tb.Text))
            {
                tb.SelectAll();
            }
        }

        //private void Input_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    TextBox tb = (TextBox)sender;
        //    if (!string.IsNullOrEmpty(tb.Text) && !string.IsNullOrEmpty(activeThumbprintBox.Text))
        //    {
        //        ChangeEncryptButtonStatus(EncryptButtonStatus.On);
        //    }
        //}

        #endregion

        #region Select Certificate Button Behavior
        private void selectCertBtn_Click(object sender, RoutedEventArgs e) => CertSelectorThread(false);

        internal void CertSelectorThread(bool machineContext)
        {
            var cThread = new Thread(new ParameterizedThreadStart(OpenCertSelector))
            {
                Name = "CertSelector",
                IsBackground = true
            };
            cThread.SetApartmentState(ApartmentState.STA);
            cThread.Start(machineContext);
        }

        #endregion

        #region CertBox Right-Click Menu
        private void findInstallablePfx_Click(object sender, RoutedEventArgs e)
        {
            X509Certificate2 cert = SharedPrompt.PfxPrompt(usingStore);
            if (cert != null)
            {
                CertificateText = cert.Thumbprint;
            }
        }

        internal void OpenCertSelector(object machineContext)
        {
            bool ctx = (bool)machineContext;
            var certSelector = new CertSelector(ctx);
            bool? result = certSelector.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Dispatcher.Invoke(() =>
                {
                    CertificateText = certSelector.SelectedCert.SHA1Thumbprint;
                });
                _enc.SetActiveCertificate(certSelector.SelectedCert);
                var st = certSelector.SelectedCert.StoreLocation;
                selectedCert = certSelector.SelectedCert;
                usingStore = st;
                _enc.workingStore = st;
            }
            else if (certSelector.Prompt)
            {
                Dispatcher.Invoke(() =>
                {
                    Prompt = true;
                    Application.Current.Shutdown();
                });
            }
            GC.Collect();
        }

        private void ChangeContextMenuItemAvailability(string name, bool isEnabled)
        {
            Grid g = certGridArea;
            ((ContextMenu)g.Resources["CertificateContextMenu"]).Items.OfType<MenuItem>().Single(
                x => x.Name == name
            ).IsEnabled = isEnabled;
        }

        private void clearCertificate_Click(object sender, RoutedEventArgs e) => CertificateText = string.Empty;
        public void ViewCertificate_Click(object sender, RoutedEventArgs e)
        {
            var cert = new CertListItem(_enc.GetActiveCertificate());
            cert.ViewCertificate();
        }

        #endregion

        #region HashBox Right-Click Menu
        private void selectAll_Click(object sender, RoutedEventArgs e)
        {
            var mi = (MenuItem)e.Source;
            var ctxMenu = (ContextMenu)mi.Parent;
            var tb = (TextBox)ctxMenu.PlacementTarget;
            tb.SelectAll();
        }

        #endregion

        #region Copy to Clipboard Button Behavior
        private void CopyBtnClip_Click(object sender, RoutedEventArgs e)
        {
            var but = (Button)sender;
            var getme = encAreaGrid.Children.OfType<UIElement>().ToList().Where(x => x.Uid.Contains("gridTextBox")).ToList();
            foreach (UIElement item in decAreaGrid.Children.OfType<UIElement>().ToList().Where(x => x.Uid.Contains("gridTextBox")))
            {
                getme.Add(item);
            }
            foreach (UIElement item in getme)
            {
                Type itemType = item.GetType();
                if (itemType == typeof(TextBox) && ((TextBox)item).Name == but.Uid)
                {
                    Clipboard.SetText(((TextBox)item).Text);
                    return;
                }
                else if (itemType == typeof(PasswordBox) && ((PasswordBox)item).Name == but.Uid)
                {
                    Clipboard.SetText(((PasswordBox)item).Password);
                    return;
                }
            }
        }
        private void passBoxCopyBtn_Click(object sender, RoutedEventArgs e)
        {
            var pb = (Button)sender;
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
            var ti = (TabItem)e.AddedItems[0];
            switch (ti.Name)
            {
                case "EncryptTab":
                    if (!string.IsNullOrEmpty(outputHashUserName.Text) || !string.IsNullOrEmpty(outputHashPassword.Text))
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
                    if (!string.IsNullOrEmpty(outputPlainUserName.Text) || !string.IsNullOrEmpty(outputPass.Password))
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
