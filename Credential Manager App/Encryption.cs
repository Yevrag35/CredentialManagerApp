using Ookii.Dialogs;
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
using System.Windows.Forms;

namespace Credential_Manager_App
{
    public class Encryption
    {
        private X509Certificate2 _cert;
        public string ActiveThumbprint
        {
            get
            {
                if (_cert == null || String.IsNullOrEmpty(_cert.Thumbprint))
                {
                    return String.Empty;
                }
                else
                {
                    return _cert.Thumbprint;
                }
            }
        }

        public Encryption() { }

        public void SetActiveCertificate(string SHA1Thumbprint)
        {
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.MaxAllowed);
            X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, SHA1Thumbprint, false);
            if (certs.Count > 0)
            {
                _cert = certs[0];
            }
            store.Close();
            store.Dispose();
        }

        public void ClearCertificate()
        {
            _cert.Dispose();
            _cert = null;
        }

        public string GetCertificateThumbprint(string fileName)
        {
            X509Certificate2 cert = new X509Certificate2(fileName);
            return cert.Thumbprint;
        }

        public byte[] EncryptString(string plainString)
        {
            byte[] btContent = Encoding.UTF8.GetBytes(plainString);
            ContentInfo content = new ContentInfo(btContent);
            EnvelopedCms cms = new EnvelopedCms(content);
            CmsRecipient recipient = new CmsRecipient(_cert);
            cms.Encrypt(recipient);
            string base64 = Convert.ToBase64String(cms.Encode());
            return Encoding.UTF8.GetBytes(base64);
        }
        //public Dictionary<string, object> HashEncrypt()
        //{
        //    CredentialDialog credDiag = new CredentialDialog()
        //    {
        //        MainInstruction = "Type the username and password to store:",
        //        Target = "Credential_Manager_App",
        //        ShowSaveCheckBox = true,
        //        ShowUIForSavedCredentials = true,
        //        WindowTitle = "Credential Manager App"
        //    };
        //    Dictionary<string, object> result = new Dictionary<string, object>();
        //    SecureString ss = new SecureString();
        //    if (credDiag.ShowDialog() == DialogResult.OK)
        //    {
        //        foreach (char c in credDiag.Password)
        //        {
        //            ss.AppendChar(c);
        //        }
        //        result.Add("Username", credDiag.UserName);
        //        result.Add("Password", ss);
        //    }
        //    return result;
        //}

        private string ConvertFromSecureToPlain(SecureString secStr)
        {
            IntPtr pPoint = Marshal.SecureStringToBSTR(secStr);
            string plain = Marshal.PtrToStringAuto(pPoint);
            Marshal.ZeroFreeBSTR(pPoint);
            return plain;
        }
        public SecureString Decrypt(byte[] byteArray)
        {
            string base64 = Encoding.UTF8.GetString(byteArray);     // back to Base64 string
            byte[] content = Convert.FromBase64String(base64);      // get byte[] of Base64 string
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(content);
            cms.Decrypt();
            SecureString ss = new SecureString();
            foreach (char c in Encoding.UTF8.GetString(cms.ContentInfo.Content))
            {
                ss.AppendChar(c);
            }
            return ss;
        }
        public PSCredential ToPSCredential(string userName, byte[] byteArray)
        {
            SecureString secPass = Decrypt(byteArray);
            return new PSCredential(userName, secPass);
        }

        public static ObservableCollection<CertListItem> GetInstalledCerts(StoreLocation location)
        {
            X509Store store = new X509Store(location);
            store.Open(OpenFlags.MaxAllowed);
            X509Certificate2Collection allCertsInStore = store.Certificates;
            ObservableCollection<CertListItem> col = new ObservableCollection<CertListItem>();
            for (int i = 0; i < allCertsInStore.Count; i++)
            {
                X509Certificate2 c = allCertsInStore[i];
                col.Add(new CertListItem(c));
            }
            return col;
        }
    }

    public abstract class ViewableCertificate
    {
        public const int CRYPTUI_DISABLE_ADDTOSTORE = 0x00000010;
        [DllImport("CryptUI.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CryptUIDlgViewCertificate(
            ref CRYPTUI_VIEWCERTIFICATE_STRUCT pCertViewInfo,
            ref bool pfPropertiesChanged
        );
        public struct CRYPTUI_VIEWCERTIFICATE_STRUCT
        {
            public int dwSize;
            public IntPtr hwndParent;
            public int dwFlags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String szTitle;
            public IntPtr pCertContext;
            public IntPtr rgszPurposes;
            public int cPurposes;
            public IntPtr pCryptProviderData; // or hWVTStateData
            public Boolean fpCryptProviderDataTrustedUsage;
            public int idxSigner;
            public int idxCert;
            public Boolean fCounterSigner;
            public int idxCounterSigner;
            public int cStores;
            public IntPtr rghStores;
            public int cPropSheetPages;
            public IntPtr rgPropSheetPages;
            public int nStartPage;
        }
    }

    public class CertListItem : ViewableCertificate, IEquatable<CertListItem>
    {
        private X509Certificate2 _cert;

        public string SHA1Thumbprint { get { return _cert.Thumbprint; } }
        public string Subject { get { return _cert.Subject; } }
        public string Issuer { get { return _cert.Issuer; } }
        public string FriendlyName { get { return _cert.FriendlyName; } }
        public string Expires { get { return _cert.NotAfter.ToString("M/d/yyyy"); } }
        public string SerialNumber { get { return _cert.SerialNumber; } }
        
        public CertListItem(X509Certificate2 cert)
        {
            _cert = cert;
        }

        public void ViewCertificate()
        {
            CRYPTUI_VIEWCERTIFICATE_STRUCT certViewInfo = new CRYPTUI_VIEWCERTIFICATE_STRUCT();
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
                    System.Windows.MessageBox.Show(error.ToString());
                }
            }
        }

        public X509Certificate2 GetCertificate()
        {
            return _cert;
        }

        public bool Equals(CertListItem item)
        {
            CertEquality ceq = new CertEquality();
            if (ceq.Equals(this, item))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override string ToString()
        {
            return SHA1Thumbprint;
        }
    }

    public class CertEquality : EqualityComparer<CertListItem>
    {
        public override bool Equals(CertListItem x, CertListItem y)
        {
            if (x.SerialNumber == y.SerialNumber)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode(CertListItem obj)
        {
            return 0;
        }
    }
}
