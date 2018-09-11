using System;
using System.Windows.Forms;

namespace Credential_Manager_App
{
    public partial class Help: Form
    {
        public Help()
        {
            InitializeComponent();
            okBtn.Focus();
        }

        private void okBtn_Click(object sender, EventArgs e) => Close();
    }
}
