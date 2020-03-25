using MaterialSkin;
using MaterialSkin.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace emuGUIibo
{
    public partial class mainForm : MaterialForm
    {
        public mainForm()
        {
            InitializeComponent();

            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;

            if (AmiiboAPI.GetAllAmiibos())
            {
                materialLabel3.Text = "Amiibo API was accessed. Amiibo list was loaded.";
                pictureBox2.Image = Properties.Resources.accept;

                if (AmiiboAPI.AmiiboSeries.Any())
                {
                    foreach (AmiiboSeries amiiboSerie in AmiiboAPI.AmiiboSeries)
                    {
                        comboBox1.Items.Add(amiiboSerie);
                    }

                    comboBox1.SelectedIndex = 0;
                }
            }
            else
            {
                materialLabel3.Text  = "Unable to download amiibo list from amiibo API.";
                pictureBox2.Image = Properties.Resources.cancel;
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();

            if (AmiiboAPI.AmiiboSeries.Any())
            {
                foreach (Amiibo amiibo in AmiiboAPI.AmiiboSeries[comboBox1.SelectedIndex].Amiibos)
                {
                    comboBox2.Items.Add(amiibo.name);
                }

                comboBox2.SelectedIndex = 0;
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            pictureBox1.ImageLocation = AmiiboAPI.AmiiboSeries[comboBox1.SelectedIndex].Amiibos[comboBox2.SelectedIndex].image;
            materialSingleLineTextField1.Text = AmiiboAPI.AmiiboSeries[comboBox1.SelectedIndex].Amiibos[comboBox2.SelectedIndex].name;
        }

        private void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(materialSingleLineTextField1.Text))
            {
                if (MessageBox.Show("No amiibo name was specified, if you press ok amiibo will use default name.", Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.OK)
                    materialSingleLineTextField1.Text = AmiiboAPI.AmiiboSeries[comboBox1.SelectedIndex].Amiibos[comboBox2.SelectedIndex].name;
                else
                {
                    CodeAction = true;
                    materialTabControl1.SelectedIndex--;
                    return;
                }
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
                Description = "Select root directory to generate the virtual amiibo on",
                ShowNewFolderButton = false,
                SelectedPath = emuiiboDir
            };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string amiiboDir = Path.Combine(folderBrowserDialog.SelectedPath, materialSingleLineTextField1.Text);

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

                    if (materialCheckBox1.Checked)
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
                        ["amiiboId"] = AmiiboAPI.AmiiboSeries[comboBox1.SelectedIndex].Amiibos[comboBox2.SelectedIndex].ID
                    };

                    File.WriteAllText(Path.Combine(amiiboDir, "model.json"), model.ToString());

                    string dateTime = DateTime.Now.ToString("yyyy-MM-dd");

                    JObject register = new JObject()
                    {
                        ["name"] = materialSingleLineTextField1.Text,
                        ["firstWriteDate"] = dateTime,
                        ["miiCharInfo"] = "mii-charinfo.bin"
                    };

                    File.WriteAllText(Path.Combine(amiiboDir, "register.json"), register.ToString());

                    JObject common = new JObject()
                    {
                        ["lastWriteDate"] = dateTime,
                        ["writeCounter"] = 0,
                        ["version"] = 0
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

        private bool CodeAction = false;
        private void materialTabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (!CodeAction)
            {
                e.Cancel = true;
                CodeAction = false;
            }
                
        }

        private void NextPage(object sender, EventArgs e)
        {
            CodeAction = true;
            materialTabControl1.SelectedIndex++;
        }
        private void PreviousPage(object sender, EventArgs e)
        {
            CodeAction = true;
            materialTabControl1.SelectedIndex--;
        }
        private void FirstPage(object sender, EventArgs e)
        {
            CodeAction = true;
            materialTabControl1.SelectedIndex = 0;
        }
    }
}