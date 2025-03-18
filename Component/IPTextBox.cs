using System.Net;

namespace Convert_to_dcm
{
    public class IPTextBox : TextBox
    {
        public IPTextBox()
        {
            this.MaxLength = 15; // Maximum length for an IP address
            this.Text = "000.000.000.000"; // Default IP format
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            FormatText();
        }

        private bool IsValidIP(string serverAddress)
        {
            return IPAddress.TryParse(serverAddress, out _);
        }

        protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
        {
            base.OnValidating(e);
            if (!IsValidIP(this.Text))
            {
                e.Cancel = true;
                MessageBox.Show("Invalid IP address format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatText()
        {
            string text = this.Text.Replace(".", string.Empty);
            if (text.Length > 3)
            {
                text = text.Insert(3, ".");
            }
            if (text.Length > 7)
            {
                text = text.Insert(7, ".");
            }
            if (text.Length > 11)
            {
                text = text.Insert(11, ".");
            }
            this.Text = text;
            this.SelectionStart = this.Text.Length;
        }
    }
}
