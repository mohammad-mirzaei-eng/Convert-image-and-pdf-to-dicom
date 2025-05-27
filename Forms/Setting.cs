using Convert_to_dcm.Helper;
using Convert_to_dcom.Class;
using Convert_to_dcom.Class.Helper;
using FileCopyer.Classes.Design_Patterns.Helper;
using System.Drawing.Imaging;
using static Convert_to_dcm.Model.DataSets;

namespace Convert_to_dcm
{
    public partial class Setting : Form
    {
        public Setting()
        {
            try
            {
                InitializeComponent();
                FillModalityComboBox();
            }
            catch (Exception ex)
            {
                ErrHelper.Instance.LogError("Error handling occurred during initialization ", ex).Wait();
                MessageBox.Show($"An error occurred during initialization: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void FillModalityComboBox()
        {
            try
            {
                comboModality.DataSource = Enum.GetValues(typeof(Modality));
                cbPixelFormat.DataSource = Enum.GetValues(typeof(PixelFormat));
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling Fill Modality ComboBox ", ex);
                MessageBox.Show($"An error occurred while filling the modality combo box: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        SettingsModel settingsModel = new SettingsModel();

        private async void btnSaveSetings_Click(object sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(portserver.Text.Trim(), out int serverPort))
                {
                    await ErrHelper.Instance.LogError("Invalid server port", new Exception("Invalid server port"));
                    MessageBox.Show("Invalid server port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                settingsModel.ServerAddress = ipservertxt.Text.Trim();
                settingsModel.ServerPort = serverPort;
                settingsModel.ServerTitle = titletxt.Text;
                settingsModel.ServerAET = destxt.Text;
                settingsModel.ServerUseTls = tlschk.Checked;
                settingsModel.Instance = txtInstance.Text;
                settingsModel.username = txtUsername.Text;
                settingsModel.password = txtPassword.Text;
                settingsModel.ServerModality = (Modality?)comboModality.SelectedItem ?? default(Modality);
                settingsModel.PixelFormat = (PixelFormat?)cbPixelFormat.SelectedItem ?? default(PixelFormat);
                settingsModel.Catalog = txtCatalog.Text.Trim();
                settingsModel.ServerAET = txtAET.Text.Trim();
                settingsModel.AlwaysCheckPatientExist=chkpatientexist.Checked;

                if (!Serverhelper.IsValidIP(settingsModel.ServerAddress))
                {
                    MessageBox.Show("Invalid server address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!await Task.Run(() => Serverhelper.IsServerReachable(settingsModel)))
                {
                    MessageBox.Show("Server is not reachable", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await SerializationHelper.SaveSettings(settingsModel);
                MessageBox.Show("Settings saved successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSettings();
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling btn Save Setings ", ex);
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Setting_Load(object sender, EventArgs e)
        {
            try
            {
                LoadSettings();
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling Setting Load ", ex);
                MessageBox.Show($"An error occurred while loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LoadSettings()
        {
            try
            {
                settingsModel =await SerializationHelper.LoadSettings();
                ipservertxt.Text = settingsModel.ServerAddress;
                portserver.Text = settingsModel.ServerPort.ToString();
                titletxt.Text = settingsModel.ServerTitle;
                destxt.Text = settingsModel.ServerAET;
                tlschk.Checked = settingsModel.ServerUseTls;
                txtInstance.Text = settingsModel.Instance;
                txtUsername.Text = settingsModel.username;
                txtPassword.Text = settingsModel.password;
                txtCatalog.Text = settingsModel.Catalog;
                txtAET.Text = settingsModel.ServerAET;
                comboModality.SelectedItem = settingsModel.ServerModality;
                cbPixelFormat.SelectedItem = settingsModel.PixelFormat;
                chkpatientexist.Checked = settingsModel.AlwaysCheckPatientExist;
            }
            catch (Exception ex)
            {
                await ErrHelper.Instance.LogError("Error handling Load Settings ", ex);
                MessageBox.Show($"An error occurred while loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
