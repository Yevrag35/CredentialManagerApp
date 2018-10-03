# Credential Manager App


[Download Link -- v1.1.2.0]( https://github.com/Yevrag35/CredentialManagerApp/raw/master/Credential%20Manager%20App/Msi/Credential%20Manager%20App_1.1.2.0.msi)

## v1.1.2

### Bug Fixes:

* Using a certificate created from the app did not allow for decryption of content.

## v1.1.1

### Changes:

- Added support for LocalMachine certificates. _(Requires admin rights)_
- The option to __create a new self-signed certificate__.
- Added color transitions when the following happen:
  1. A new certificate is chosen or cleared; blinks blue  (it also happens during app launch).
  1. When the Encrypt/Decyrpt button is pressed when fields are missing; blinks red in the boxes that are missing.

## v1.1.0

### Changes:

- **Certificate Logic**
  - Certificates must now be PKCS#12 files *(\*.pfx)*.
  - The default beahvior for the "Browse" button is to open the CertSelector.
  - In the CertSelector window, there is a option to "Import PFX".  When the certificate is chosen, a credential prompt will ask for the file's password.  It will then be installed into the CurrentUser=>Personal repository.
- A new copy icon for each 'Copy' button.
- Changed some code to 'internal'.
- The **CertSelector** window now has a proper title.
- Some of the buttons have had their fonts changed.
- This README will be updated with the project's Change History.

## v1.0.2

### Bugs fixes:

- When *decrypting* input, the output was not being displayed in the proper 'output' text boxes.

## v1.0.1

### Changes:

- The Encrypt/Decrypt button will no longer be disabled when input is not present.
  - An error message now displays when fields are left blank when the Encrypt/Decrypt button is pressed.
- Removed default 'UesrNameBox' text.
- Window title is now using a dynamic version number query.


### Bugs fixes:

- When you switch tabs (either tab) and only 1 text box is populated (either username or password), upon switching back to that tab, the Encrypt button shows "Reset" properly, however disables the button.  This forces you to enter text in the other textbox, or change the existing text to re-enable the button.
- The Encrypt/Decrypt will keep its "RESET" status when switching to a tab that contains already specified input.

---

## About This App

### **What is it?**

It's an application that allows you quickly encrypt and/or decrypt sensitive text with a click of a button.  All you have to do is specify a X509 certificate file or choose from one that's installed into your Personal certificate store.

The reason for the name "Credential Manager App" is that I personally use this application for encrypting website passwords and usernames for safe storage.

### **How does it work?**

By selecting a encryption certificate from a certificate file...

![EncryptDecryptDemo1](https://images.yevrag35.com/EncryptDecryptDemo(1).gif)

... or choosing from an already installed certificate on your PC...

![EncryptDecryptDemo2](https://images.yevrag35.com/EncryptDecryptDemo(2).gif)

...typing in a username and/or password in the provided fields, and clicking the **ENCRYPT/DECRYPT** button.

![EncryptDecryptDemo2](https://images.yevrag35.com/EncryptDecryptDemo(3).gif)