using Ookii.Dialogs;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Credential_Manager_App
{
    internal static class SharedPrompt
    {
        internal static X509Certificate2 PfxPrompt()
        {
            VistaOpenFileDialog fileDiag = new VistaOpenFileDialog()
            {
                ShowHelp = true,
                Filter = "PFX certificates (*.pfx)|*.pfx",
                InitialDirectory = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop",
                Multiselect = false,
                Title = "Select a password protected pfx file"
            };
            if (fileDiag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CredentialDialog pfxPass = new CredentialDialog
                {
                    ShowSaveCheckBox = false,
                    MainInstruction = "Enter the password for the protected pfx file.",
                    Content = "The USERNAME does not matter.",
                    Target = "MainWindow",
                    ShowUIForSavedCredentials = false,
                    UseApplicationInstanceCredentialCache = false
                };
                if (pfxPass.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    X509Certificate2 cert = Encryption.InstallPfx(fileDiag.FileName, pfxPass.Credentials.SecurePassword);
                    return cert;
                }
            }
            return null;
        }

    }
}
