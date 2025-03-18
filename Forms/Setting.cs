using Convert_to_dcom.Class;
using Convert_to_dcom.Class.Helper;
using FileCopyer.Classes.Design_Patterns.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Convert_to_dcm.Model.DataSets;

namespace Convert_to_dcm
{
    public partial class Setting : Form
    {
        public Setting()
        {
            InitializeComponent();
            FillModalityComboBox();
        }

        private void FillModalityComboBox()
        {
            comboModality.DataSource = Enum.GetValues(typeof(Modality));
        }

        SettingsModel settingsModel = new SettingsModel();
        private void btnSaveSetings_Click(object sender, EventArgs e)
        {
            settingsModel.ServerAddress = ipservertxt.Text.Trim();
            settingsModel.ServerPort = int.Parse(portserver.Text.Trim());
            settingsModel.ServerTitle = titletxt.Text;
            settingsModel.ServerAET = destxt.Text;
            settingsModel.ServerUseTls = tlschk.Checked;
            settingsModel.Instance = txtInstance.Text;
            settingsModel.username = txtUsername.Text;
            settingsModel.password = txtPassword.Text;
            settingsModel.ServerModality = (Modality?)comboModality.SelectedItem ?? default(Modality);
            settingsModel.Catalog =txtCatalog.Text.Trim();
            settingsModel.ServerAET = txtAET.Text.Trim();


            if (!Serverhelper.IsValidIP(settingsModel.ServerAddress))
            {
                MessageBox.Show("آدرس سرور نامعتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Serverhelper.IsServerReachable(settingsModel))
            {
                MessageBox.Show("سرور قابل دسترسی نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SerializationHelper.SaveSettings(settingsModel);
            MessageBox.Show("تغییرات با موفقیت ذخیره شد", "اعلام", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadSettings();
        }

        private void Setting_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            settingsModel = SerializationHelper.LoadSettings();
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

        }
    }
}
