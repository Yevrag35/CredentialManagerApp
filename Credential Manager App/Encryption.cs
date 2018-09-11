using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;

namespace Credential_Manager_App
{
    public class Encryption
    {
        #region Properties/Fields
        private X509Certificate2 _cert;
        public StoreLocation workingStore = StoreLocation.CurrentUser;
        private byte[] workingContent;
        public string ActiveThumbprint => _cert == null || string.IsNullOrEmpty(_cert.Thumbprint) ? string.Empty : _cert.Thumbprint;

        #endregion

        #region Constructors
        internal Encryption() { }
        internal Encryption(StoreLocation inStore) => workingStore = inStore;

        #endregion

        #region Certificate Methods
        internal void SetActiveCertificate(string SHA1Thumbprint, int loc)
        {
            SetActiveCertificate(SHA1Thumbprint, (StoreLocation)loc);
        }
        internal void SetActiveCertificate(string SHA1Thumbprint, StoreLocation loc)
        {
            using (var store = new X509Store(loc))
            {
                store.Open(OpenFlags.OpenExistingOnly);
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, SHA1Thumbprint, false);
                if (certs.Count > 0)
                {
                    _cert = certs[0];
                }
            }
        }
        internal void SetActiveCertificate(CertListItem cli)
        {
            _cert = cli.GetCertificate();
        }

        internal CertListItem TextToCLI(string thumbprint, int loc)
        {
            return TextToCLI(thumbprint, (StoreLocation)loc);
        }
        internal CertListItem TextToCLI(string thumbprint, StoreLocation loc)
        {
            using (var store = new X509Store(loc))
            {
                store.Open(OpenFlags.OpenExistingOnly);
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (certs.Count > 0)
                {
                    return new CertListItem(certs[0]);
                }
                else
                {
                    return null;
                }
            }
        }

        internal X509Certificate2 GetActiveCertificate() => _cert;

        internal void ClearCertificate()
        {
            _cert.Dispose();
            _cert = null;
        }

        internal string GetCertificateThumbprint(string fileName)
        {
            var cert = new X509Certificate2(fileName);
            return cert.Thumbprint;
        }

        #endregion

        #region PKCS#12 Methods
        internal static X509Certificate2 InstallPfx(string pathToCert, SecureString securePass, StoreLocation loc)
        {
            var cert = new X509Certificate2(pathToCert, securePass);
            if (cert != null)
            {
                using (var store = new X509Store(StoreName.My, loc))
                {
                    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                    if (!store.Certificates.Contains(cert))
                    {
                        store.Add(cert);
                    }
                    GC.Collect();
                    return cert;
                }
            }
            return null;
        }

        #endregion

        #region Encryption Methods
        internal string EncryptStringToString(object plainString)
        {
            Type plainStringType = plainString.GetType();
            string baseString;
            if (plainStringType != typeof(string) && plainStringType != typeof(SecureString))
            {
                throw new ArgumentException("Parameter must be of a string value!");
            }
            else
            {
                baseString = plainStringType == typeof(SecureString) ? 
                    ConvertFromSecureToPlain((SecureString)plainString) : (string)plainString;
            }
            byte[] btContent = Encoding.UTF8.GetBytes(baseString);
            var content = new ContentInfo(btContent);
            var cms = new EnvelopedCms(content);
            var recipient = new CmsRecipient(_cert);
            cms.Encrypt(recipient);
            return Convert.ToBase64String(cms.Encode());
        }
        internal byte[] EncryptStringToBytes(object plainString)
        {
            string base64 = EncryptStringToString(plainString);
            return Encoding.UTF8.GetBytes(base64);
        }

        private string ConvertFromSecureToPlain(SecureString secStr)
        {
            IntPtr pPoint = Marshal.SecureStringToBSTR(secStr);
            string plain = Marshal.PtrToStringAuto(pPoint);
            Marshal.ZeroFreeBSTR(pPoint);
            return plain;
        }
        internal SecureString Decrypt(byte[] encBytes)
        {
            string plain = PlainDecrypt(encBytes);
            var ss = new SecureString();
            foreach (char c in plain)
            {
                ss.AppendChar(c);
            }
            return ss;
        }
        internal string PlainDecrypt(byte[] encBytes)
        {
            string base64 = Encoding.UTF8.GetString(encBytes);      // back to Base64 string
            return PlainDecrypt(base64);
        }
        internal string PlainDecrypt(string base64)
        {
            try
            {
                workingContent = Convert.FromBase64String(base64);
                var cms = new EnvelopedCms();
                cms.Decode(workingContent);
                cms.Decrypt();
                return Encoding.UTF8.GetString(cms.ContentInfo.Content);
            }
            catch (Exception e)
            {
                throw new CryptographicException(e.Message + Environment.NewLine + Environment.NewLine + base64);
            }
            finally
            {
                workingContent = null;
            }
        }

        #endregion

        #region Credential Methods
        internal PSCredential ToPSCredential(string userName, byte[] byteArray)
        {
            SecureString secPass = Decrypt(byteArray);
            return new PSCredential(userName, secPass);
        }

        internal static ObservableCollection<CertListItem> GetInstalledCerts(StoreLocation location)
        {
            using (var store = new X509Store(location))
            { 
                store.Open(OpenFlags.OpenExistingOnly);
                X509Certificate2Collection allCertsInStore = store.Certificates;
                var col = new ObservableCollection<CertListItem>();
                for (int i = 0; i < allCertsInStore.Count; i++)
                {
                    X509Certificate2 c = allCertsInStore[i];
                    col.Add(new CertListItem(c));
                }
                return col;
            }
        }

        #endregion
    }

    #region Viewable Cert Abstract Class
    public abstract class ViewableCertificate
    {
        internal const int CRYPTUI_DISABLE_ADDTOSTORE = 0x00000010;
        [DllImport("CryptUI.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool CryptUIDlgViewCertificate(
            ref CRYPTUI_VIEWCERTIFICATE_STRUCT pCertViewInfo,
            ref bool pfPropertiesChanged
        );
        internal struct CRYPTUI_VIEWCERTIFICATE_STRUCT
        {
            public int dwSize;
            public IntPtr hwndParent;
            public int dwFlags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string szTitle;
            public IntPtr pCertContext;
            public IntPtr rgszPurposes;
            public int cPurposes;
            public IntPtr pCryptProviderData; // or hWVTStateData
            public bool fpCryptProviderDataTrustedUsage;
            public int idxSigner;
            public int idxCert;
            public bool fCounterSigner;
            public int idxCounterSigner;
            public int cStores;
            public IntPtr rghStores;
            public int cPropSheetPages;
            public IntPtr rgPropSheetPages;
            public int nStartPage;
        }
    }

    #endregion

    #region CertListItem : ViewableCertificate, IEquatable<CertListItem>
    public class CertListItem : ViewableCertificate, IEquatable<CertListItem>
    {
        private X509Certificate2 _cert;
        private readonly StoreLocation _sl;

        public string SHA1Thumbprint => _cert.Thumbprint;
        public string Subject => _cert.Subject;
        public string Issuer => _cert.Issuer;
        public string FriendlyName => _cert.FriendlyName;
        public string Expires => _cert.NotAfter.ToString("M/d/yyyy");
        public string SerialNumber => _cert.SerialNumber;
        public StoreLocation StoreLocation => _sl;

        public CertListItem(X509Certificate2 cert)
        {
            _cert = cert;
            using (var store = new X509Store(StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.OpenExistingOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySerialNumber, _cert.SerialNumber, false);
                _sl = certs.Count != 0 ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
            }
        }

        public void ViewCertificate()
        {
            var certViewInfo = new CRYPTUI_VIEWCERTIFICATE_STRUCT();
            certViewInfo.dwSize = Marshal.SizeOf(certViewInfo);
            certViewInfo.pCertContext = _cert.Handle;
            certViewInfo.szTitle = "Certificate Info";
            certViewInfo.dwFlags = CRYPTUI_DISABLE_ADDTOSTORE;
            certViewInfo.nStartPage = 0;
            bool fPropertiesChanged = false;
            if (!CryptUIDlgViewCertificate(ref certViewInfo, ref fPropertiesChanged))
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 1223)
                {
                    MessageBox.Show("Showing the certificate errored with exit code " + error.ToString(), 
                        "ERROR!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        internal X509Certificate2 GetCertificate() => _cert;

        public bool Equals(CertListItem item)
        {
            var ceq = new CertEquality();
            return ceq.Equals(this, item) ? true : false;
        }
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => SHA1Thumbprint;
    }

    #endregion

    #region CertEquality
    internal class CertEquality : EqualityComparer<CertListItem>
    {
        public override bool Equals(CertListItem x, CertListItem y) => x.SerialNumber == y.SerialNumber ? true : false;
        public override int GetHashCode(CertListItem obj) => 0;
    }

    #endregion
}
