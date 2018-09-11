using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Forms;

namespace Credential_Manager_App
{
    public partial class MainWindow : Window
    {
        private protected readonly Dictionary<string, object> defArgs = new Dictionary<string, object>()
        {
            { "Subject", string.Empty },
            { "FriendlyName", string.Empty },
            { "SavedAlgorithm", 0 },
            { "SavedKeyLength", 0 },
            { "SavedExpiration", null }
        };

        private void newCertBtn_Click(object sender, RoutedEventArgs e) => CallNewCert(defArgs);

        internal void CallNewCert(Dictionary<string, object> argDict)
        {
            if (argDict == null)
            {
                argDict = defArgs;
            }
            var newCertThread = new Thread(new ParameterizedThreadStart(OpenNewCertWindow))
            {
                Name = "NewCertificateGenerator",
                IsBackground = true
            };
            newCertThread.SetApartmentState(ApartmentState.STA);
            newCertThread.Start(argDict);
        }

        private void OpenNewCertWindow(object o)
        {
            var argDict = (Dictionary<string, object>)o;
            var arg1 = (string)argDict["Subject"];
            var arg2 = (string)argDict["FriendlyName"];
            var arg3 = Convert.ToInt32(argDict["SavedAlgorithm"]);
            var arg4 = Convert.ToInt32(argDict["SavedKeyLength"]);
            var arg5 = (string)argDict["SavedExpiration"];
            var newCertWindow = new NewCertWindow(arg1, arg2, arg3, arg4, arg5);
            bool? result = newCertWindow.ShowDialog();
            if (result.HasValue && result.Value)
            {
                var answer = System.Windows.MessageBox.Show("Would you like to replace the current certificate with the newly created one?",
                    "Use the New Certificate?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (answer == MessageBoxResult.Yes)
                {
                    Dispatcher.Invoke(() =>
                    {
                        CertificateText = newCertWindow.NewCert.SHA1Thumbprint;
                    });
                    _enc.SetActiveCertificate(newCertWindow.NewCert);
                    selectedCert = newCertWindow.NewCert;
                    usingStore = newCertWindow.NewCert.StoreLocation;
                    _enc.workingStore = newCertWindow.NewCert.StoreLocation;
                }
            }
            else if (newCertWindow.Prompt)
            {
                Dispatcher.Invoke(() =>
                {
                    Prompt = true;
                    System.Windows.Application.Current.Shutdown();
                });
            }
            GC.Collect();
        }
    }

    /// <summary>
    /// Interaction logic for NewCertWindow.xaml
    /// </summary>
    public partial class NewCertWindow : Window
    {
        private protected readonly CustomItem userCtx = new CustomItem("Current User", null);

        internal DateTime? ExpirationDate { get; set; }
        internal CertListItem NewCert { get; set; }
        private NewCertificate nc;

        public string SubText { get; set; }
        public string FriendName { get; set; }
        public int SavedAlgorithm { get; set; }
        public int SavedKeyLength { get; set; }

        public CertSelector.CertContext ctx { get; set; }

        internal bool Prompt = false;

        public NewCertWindow(object subject, object friendlyName, object savedAlgorithm, object savedKeyLength, object validUntil)
        {
            SubText = ((string)subject).Replace("%20", " ");
            FriendName = ((string)friendlyName).Replace("%20", " ");
            SavedAlgorithm = (int)savedAlgorithm;
            SavedKeyLength = (int)savedKeyLength;

            InitializeComponent();

            var fileTime = (string)validUntil;
            if (!string.IsNullOrEmpty(fileTime))
            {
                DateTime? vd = DateTime.FromFileTime(Convert.ToInt64(fileTime));
                ExpirationDate = vd;
                FlipDateTimeButton(DateTimeButtonOptions.Off, vd);
            }

            string path = null;
            if (!Credential_Manager_App.MainWindow.IsAdministrator())
            {
                path = "Media\\uac_shield.png";
            }
            var items = new CustomItem[2]
            {
                userCtx,
                new CustomItem("Local Machine  ", path)
            };
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                storeComboBox.Items.Add(item);
            }
        }

        private void NewCertWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Prompt)
            {
                DialogResult = NewCert != null ? true : false;
            }
            else
            {
                string exe = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;
                string longTime = null;
                if (ExpirationDate.HasValue)
                {
                    longTime = Convert.ToString(ExpirationDate.Value.ToFileTimeUtc());
                }

                var procInfo = new ProcessStartInfo(exe,
                    "/LaunchNewCert:" + SubText.Replace(" ", "%20") + "," +
                    FriendName.Replace(" ", "%20") + "," +
                    SavedAlgorithm + "," + SavedKeyLength + "," + longTime)
                {
                    Verb = "RunAs",
                    CreateNoWindow = false
                };
                try
                {
                    var proc = Process.Start(procInfo);
                }
                catch (Win32Exception ex)
                {
                    Prompt = false;
                    storeComboBox.SelectedIndex = 0;
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

        private void createBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(subjTextBox.Text) && !string.IsNullOrEmpty(fNameTextBox.Text) && ExpirationDate.HasValue)
            {
                NewCertificate.HashAlgorithm hashAl = NewCertificate.GetHashFromString(algoComboBox.SelectedValue);
                int kLen = Convert.ToInt32(((System.Windows.Controls.ComboBoxItem)keyLenComboBox.SelectedValue).Content);
                var strCtx = (StoreLocation)storeComboBox.SelectedIndex + 1;

                nc = new NewCertificate(subjTextBox.Text, fNameTextBox.Text,
                        ExpirationDate.Value, hashAl,
                        (NewCertificate.KeyLengths)kLen, strCtx);

                try
                {
                    X509Certificate2 cert = nc.GenerateCertificate();
                    NewCert = new CertListItem(cert);
                    System.Windows.MessageBox.Show("A new self-signed certificate has been successfully created!", 
                        "SUCCESS", MessageBoxButton.OK, MessageBoxImage.None);
                    Close();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "ERROR!",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    FlipDateTimeButton(DateTimeButtonOptions.On);
                    return;
                }
            }
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e) => Close();

        private DateTime? ShowDatePicker()
        {
            // Create the Form
            var font = new Font("Arial", 13);
            var form = new Form()
            {
                Text = "Select a 'ValidUntil' time:",
                Font = font,
                ForeColor = Color.White,
                BackColor = Color.Black,
                Width = 300,
                Height = 165,
                TopMost = true,
                StartPosition = FormStartPosition.CenterScreen
            };

            // DatePicker Label
            var dtLabel = new System.Windows.Forms.Label()
            {
                Text = "Date",
                Location = new System.Drawing.Point(15, 10),
                Height = 22,
                Width = 90
            };
            form.Controls.Add(dtLabel);

            // DatePicker
            var dtPicker = new DateTimePicker()
            {
                Location = new System.Drawing.Point(110, 7),
                Width = 150,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MM/dd/yyyy"
            };
            form.Controls.Add(dtPicker);

            // MaxTimePicker Label
            var mtLabel = new System.Windows.Forms.Label()
            {
                Text = "Time",
                Location = new System.Drawing.Point(15, 55),
                Height = 22,
                Width = 90
            };
            form.Controls.Add(mtLabel);

            // MaxTimePicker
            var mtPicker = new DateTimePicker()
            {
                Location = new System.Drawing.Point(110, 52),
                Width = 150,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "hh:mm:ss tt",
                ShowUpDown = true
            };
            form.Controls.Add(mtPicker);

            // OK Button
            var okBtn = new System.Windows.Forms.Button()
            {
                Location = new System.Drawing.Point(15, 95),
                ForeColor = Color.Black,
                BackColor = Color.White,
                Text = "OK",
                DialogResult = System.Windows.Forms.DialogResult.OK,
            };
            form.AcceptButton = okBtn;
            form.Controls.Add(okBtn);

            // Show and Return the result
            if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return null;
            }

            DateTime date = dtPicker.Value;
            DateTime time = mtPicker.Value;

            var combined = date.ToShortDateString() + " " + time.ToShortTimeString();
            return DateTime.Parse(combined);
        }

        private void setExpirationBtn_Click(object sender, RoutedEventArgs e)
        {
            DateTime? result = ShowDatePicker();
            if (result.HasValue)
            {
                ExpirationDate = result;
                FlipDateTimeButton(DateTimeButtonOptions.Off, result);
            }
        }

        internal enum DateTimeButtonOptions : int
        {
            On = 1,
            Off = 2
        }

        private void FlipDateTimeButton(DateTimeButtonOptions onOff, DateTime? dateShown = null)
        {
            switch (onOff)
            {
                case DateTimeButtonOptions.On:
                    expirationTime.Visibility = Visibility.Hidden;
                    expirationTime.IsEnabled = false;
                    setExpirationBtn.IsEnabled = true;
                    setExpirationBtn.Visibility = Visibility.Visible;
                    break;
                case DateTimeButtonOptions.Off:
                    setExpirationBtn.Visibility = Visibility.Hidden;
                    setExpirationBtn.IsEnabled = false;
                    expirationTime.IsEnabled = true;
                    expirationTime.Content = dateShown.Value.ToString();
                    expirationTime.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void storeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (System.Windows.Controls.ComboBox)sender;
            var seldItem = e.AddedItems[0] as CustomItem;
            if (seldItem.Text.StartsWith("Local Machine") && seldItem.Image.UriSource != null)
            {
                Prompt = true;
                Close();
            }
        }
    }

    public class CustomItem
    {
        public string Text { get; set; }
        public BitmapImage Image { get; set; }

        public CustomItem(string text, string imagePath)
        {
            Text = text;

            if (imagePath != null)
            {
                var src = new BitmapImage();
                src.BeginInit();
                src.UriSource = new Uri(imagePath, UriKind.Relative);
                src.EndInit();
                Image = src;
            }
            else
            {
                Image = new BitmapImage();
            }
        }
    }
}
