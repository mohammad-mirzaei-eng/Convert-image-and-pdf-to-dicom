namespace Convert_to_dcm
{
    public class NumericText : TextBox
    {
        public NumericText()
        {
            this.KeyPress += new KeyPressEventHandler(NumericText_KeyPress);
        }

        private void NumericText_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Check if the pressed key is a digit or a control key (like backspace)
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true; // Ignore the key press
            }
        }
    }
}
