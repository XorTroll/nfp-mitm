using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace emuGUIibo
{
    public partial class mainForm : Form
    {
        public mainForm()
        {
            InitializeComponent();

            if (AmiiboAPI.GetAllAmiibos())
            {
                toolStripStatusLabel1.Text = "Amiibo API was accessed. Amiibo list was loaded.";

                if (AmiiboAPI.AmiiboSeries.Any())
                {
                    foreach (string amiiboSerie in AmiiboAPI.AmiiboSeries)
                    {
                        comboBox1.Items.Add(amiiboSerie);
                    }

                    comboBox1.SelectedIndex = 0;
                }
            }
            else
            {
                toolStripStatusLabel1.Text  = "Unable to download amiibo list from amiibo API.";
                toolStripStatusLabel1.Image = Properties.Resources.cancel;

                groupBox1.Enabled = false;
                groupBox2.Enabled = false;
                groupBox3.Enabled = false;
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();

            if (AmiiboAPI.AllAmiibo.Any())
            {
                foreach (Amiibo amiibo in AmiiboAPI.AllAmiibo)
                {
                    if (amiibo.SeriesName == comboBox1.Text)
                    {
                        comboBox2.Items.Add(amiibo.AmiiboName);
                    }
                }

                comboBox2.SelectedIndex = 0;
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            pictureBox1.ImageLocation = AmiiboAPI.AllAmiibo.Where(amiibo => amiibo.SeriesName == comboBox1.Text && amiibo.AmiiboName == comboBox2.Text).SingleOrDefault().ImageURL;
            textBox1.Text             = AmiiboAPI.AllAmiibo.Where(amiibo => amiibo.SeriesName == comboBox1.Text && amiibo.AmiiboName == comboBox2.Text).SingleOrDefault().AmiiboName;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("No amiibo name was specified.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }

            string emuiiboDir = "";

            if (DriveInfo.GetDrives().Any())
            {
                foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
                {
                    if (driveInfo.IsReady)
                    {
                        if (Directory.Exists(Path.Combine(driveInfo.Name, Path.Combine("emuiibo", "amiibo"))))
                        {
                            emuiiboDir = Path.Combine(driveInfo.Name, Path.Combine("emuiibo", "amiibo"));
                        }
                        else if (Directory.Exists(Path.Combine(driveInfo.Name, "emuiibo")))
                        {
                            Directory.CreateDirectory(Path.Combine(driveInfo.Name, Path.Combine("emuiibo", "amiibo")));

                            emuiiboDir = Path.Combine(driveInfo.Name, Path.Combine("emuiibo", "amiibo"));
                        }

                        if (!string.IsNullOrEmpty(emuiiboDir))
                        {
                            MessageBox.Show($"Emuiibo directory was found in drive '{driveInfo.VolumeLabel}', so defaulting to that directory.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description         = "Select root directory to generate the virtual amiibo on",
                ShowNewFolderButton = false,
                SelectedPath        = emuiiboDir
            };

            folderBrowserDialog.SelectedPath = Properties.Settings.Default.Folder_Path;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {

                Properties.Settings.Default.Folder_Path = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();

                string amiiboDir = Path.Combine(folderBrowserDialog.SelectedPath, textBox1.Text);

                if (MessageBox.Show($"Virtual amiibo will be created in '{amiiboDir}'.{Environment.NewLine + Environment.NewLine}The directory will be deleted if it already exists.{Environment.NewLine + Environment.NewLine}Proceed with amiibo creation?", Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                {
                    return;
                }

                if (Directory.Exists(amiiboDir))
                {
                    Directory.Delete(amiiboDir, true);
                }

                try
                {
                    Directory.CreateDirectory(amiiboDir);

                    JObject tag = new JObject();

                    if (checkBox1.Checked)
                    {
                        tag["randomUuid"] = true;
                    }
                    else
                    {
                        tag["uuid"] = AmiiboAPI.MakeRandomHexString(18);
                    }

                    File.WriteAllText(Path.Combine(amiiboDir, "tag.json"), tag.ToString());

                    JObject model = new JObject()
                    {
                        ["amiiboId"] = AmiiboAPI.AllAmiibo.Where(amiibo => amiibo.SeriesName == comboBox1.Text && amiibo.AmiiboName == comboBox2.Text).SingleOrDefault().AmiiboId
                    };

                    File.WriteAllText(Path.Combine(amiiboDir, "model.json"), model.ToString());

                    string dateTime = DateTime.Now.ToString("yyyy-MM-dd");

                    JObject register = new JObject()
                    {
                        ["name"]           = textBox1.Text,
                        ["firstWriteDate"] = dateTime,
                        ["miiCharInfo"]    = "mii-charinfo.bin"
                    };

                    File.WriteAllText(Path.Combine(amiiboDir, "register.json"), register.ToString());

                    JObject common = new JObject()
                    {
                        ["lastWriteDate"] = dateTime,
                        ["writeCounter"]  = 0,
                        ["version"]       = 0
                    };

                    File.WriteAllText(Path.Combine(amiiboDir, "common.json"), common.ToString());

                    MessageBox.Show("Virtual amiibo was successfully created.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch
                {
                    MessageBox.Show("An error ocurred attempting to create the virtual amiibo.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}