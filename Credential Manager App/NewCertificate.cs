﻿using CERTENROLLLib;
using MG;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Controls;

namespace Credential_Manager_App
{
    public class NewCertificate
    {
        private protected const string provName = "Microsoft Enhanced RSA and AES Cryptographic Provider";
        private protected readonly string[] EnhancedUsages = new string[2] { "Client Authentication", "Encrypting File System" };

        private List<CX509Extension> ExtensionsToAdd = new List<CX509Extension>();
        public string SubjectName { get; set; }
        public string FriendlyName { get; set; }
        public DateTime ValidUntil { get; }
        public string Algorithm { get; }
        public int KeyLength { get; }
        public StoreLocation Store { get; set; }
        public bool MachineContext => Store == StoreLocation.LocalMachine;

        #region Constructors
        public NewCertificate(string subject, string friendlyName, DateTime validUntil, HashAlgorithm hash, KeyLengths keyLength, 
            StoreLocation location)
        {
            SubjectName = subject;
            FriendlyName = friendlyName;
            ValidUntil = validUntil.ToUniversalTime();
            Algorithm = hash.ToString();
            KeyLength = (int)keyLength;
            Store = location;
        }

        #endregion

        #region Enums
        public enum HashAlgorithm : int
        {
            SHA256 = 0,
            SHA384 = 1,
            SHA512 = 2
        }
        public enum KeyLengths : int
        {
            Two048 = 2048,
            Four096 = 4096,
            Eight192 = 8192,
            Sixteen384 = 16384
        }

        #endregion

        #region Methods

        internal static HashAlgorithm GetHashFromString(object inStr)
        {
            var real = (ComboBoxItem)inStr;
            Array arrHash = typeof(HashAlgorithm).GetEnumValues();
            for (int i = 0; i < arrHash.Length; i++)
            {
                var h = (HashAlgorithm)arrHash.GetValue(i);
                if (real.Content.Equals(h.ToString()))
                {
                    return h;
                }
            }
            throw new Exception("What the fuck?");
        }

        public X509Certificate2 GenerateCertificate()
        {
            SetEnhancedUsages();
            SetKeyUsages();
            SetBasicConstraints();

            CX509CertificateRequestCertificate certReq = CreateRequest();
            certReq = FinalizeRequest(certReq);
            X509Certificate2 cert = CreateNewCertificate(certReq);
            return cert;
        }

        private void SetEnhancedUsages()
        {
            var oids = new CObjectIds();
            
            for (int i = 0; i < EnhancedUsages.Length; i++)
            {
                var s = EnhancedUsages[i];
                var oid = new CObjectId();
                var eu = Oid.FromFriendlyName(s, OidGroup.EnhancedKeyUsage);
                oid.InitializeFromValue(eu.Value);
                oids.Add(oid);
            }
            var eku = new CX509ExtensionEnhancedKeyUsage();
            eku.InitializeEncode(oids);
            ExtensionsToAdd.Add((CX509Extension)eku);
        }

        private void SetKeyUsages()
        {
            var ku = new CX509ExtensionKeyUsage
            {
                Critical = true
            };
            ku.InitializeEncode(
                CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE | 
                CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE | 
                CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DATA_ENCIPHERMENT_KEY_USAGE);
            ExtensionsToAdd.Add((CX509Extension)ku);
        }

        private void SetBasicConstraints()
        {
            var bc = new CX509ExtensionBasicConstraints();
            bc.InitializeEncode(false, -1);
            bc.Critical = true;
            ExtensionsToAdd.Add((CX509Extension)bc);
        }

        private CX509CertificateRequestCertificate CreateRequest()
        {
            var pk = new CX509PrivateKey
            {
                ProviderName = provName
            };
            var algId = new CObjectId();
            var algVal = Oid.FromFriendlyName("RSA", OidGroup.PublicKeyAlgorithm);
            algId.InitializeFromValue(algVal.Value);
            pk.Algorithm = algId;
            pk.KeySpec = X509KeySpec.XCN_AT_KEYEXCHANGE;
            pk.Length = KeyLength;
            pk.MachineContext = MachineContext;
            pk.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;
            pk.Create();

            var req = new CX509CertificateRequestCertificate();
            var useCtx = (X509CertificateEnrollmentContext)Store;
            req.InitializeFromPrivateKey(useCtx, pk, string.Empty);
            return req;
        }

        private CX509CertificateRequestCertificate FinalizeRequest(CX509CertificateRequestCertificate cert)
        {
            var subDN = new CX500DistinguishedName();
            subDN.Encode("CN=" + SubjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);
            cert.Subject = subDN;
            cert.Issuer = cert.Subject;
            cert.NotBefore = DateTime.Now;
            cert.NotAfter = ValidUntil;
            for (int i = 0; i < ExtensionsToAdd.Count; i++)
            {
                var ext = ExtensionsToAdd[i];
                cert.X509Extensions.Add(ext);
            }
            var sigId = new CObjectId();
            var hash = Oid.FromFriendlyName(Algorithm, OidGroup.HashAlgorithm);
            sigId.InitializeFromValue(hash.Value);
            cert.SignatureInformation.HashAlgorithm = sigId;

            // Complete it
            cert.Encode();
            ExtensionsToAdd.Clear();
            return cert;
        }

        private X509Certificate2 CreateNewCertificate(CX509CertificateRequestCertificate cert)
        {
            var enr = new CX509Enrollment
            {
                CertificateFriendlyName = FriendlyName
            };
            enr.InitializeFromRequest(cert);
            string endCert = enr.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64);
            enr.InstallResponse(InstallResponseRestrictionFlags.AllowUntrustedCertificate, endCert, EncodingType.XCN_CRYPT_STRING_BASE64, string.Empty);

            byte[] certBytes = Convert.FromBase64String(endCert);
            return new X509Certificate2(certBytes);
        }

        #endregion
    }
}
