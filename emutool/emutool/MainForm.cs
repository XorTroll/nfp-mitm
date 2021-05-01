using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using FluentFTP;

namespace emutool
{
    public partial class MainForm : Form
    {
        public static AmiiboAPI.AmiiboList Amiibos = null;

        public static List<string> AmiiboSeries = null;
        public static List<AmiiboAPI.Amiibo> CurrentSeriesAmiibos = null;

        private static string LastUsedPath = null;
        private static string DialogCaption = null;

        public static bool HasAmiibos()
        {
            if(Amiibos != null)
            {
                return Amiibos.GetAmiiboCount() > 0;
            }
            return false;
        }

        public MainForm()
        {
            InitializeComponent();
            DialogCaption = "emutool v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Text = DialogCaption + " - emuiibo's tool for virtual amiibo creation";
            Amiibos = AmiiboAPI.GetAllAmiibos();

            if(HasAmiibos())
            {
                toolStripStatusLabel1.Text = "AmiiboAPI was accessed - amiibo list was loaded.";
                AmiiboSeries = Amiibos.GetAmiiboSeries();

                if(AmiiboSeries.Any())
                {
                    foreach(var series in AmiiboSeries)
                    {
                        SeriesComboBox.Items.Add(series);
                    }
                    SeriesComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                toolStripStatusLabel1.Text = "Unable to download amiibo list from AmiiboAPI.";
                toolStripStatusLabel1.Image = Properties.Resources.ErrorIcon;
                groupBox1.Enabled = false;
                groupBox2.Enabled = false;
                groupBox3.Enabled = false;
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            AmiiboComboBox.Items.Clear();

            if(HasAmiibos())
            {
                var series = SeriesComboBox.Text;
                CurrentSeriesAmiibos = Amiibos.GetAmiibosBySeries(series);
                if(CurrentSeriesAmiibos.Any())
                {
                    foreach(var amiibo in CurrentSeriesAmiibos)
                    {
                        AmiiboComboBox.Items.Add(amiibo.AmiiboName);
                    }
                }
                AmiiboComboBox.SelectedIndex = 0;
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var cur_amiibo = CurrentSeriesAmiibos[AmiiboComboBox.SelectedIndex];
                AmiiboPictureBox.ImageLocation = cur_amiibo.ImageURL;
                AmiiboNameBox.Text = cur_amiibo.AmiiboName;
                CreateAllCheck.Checked = false;
            }
            catch(Exception ex)
            {
                ExceptionUtils.LogExceptionMessage(ex);
            }
        }

        private void ShowErrorBox(string msg)
        {
            MessageBox.Show(msg, DialogCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private string SelectDirectory()
        {
            string emuiibo_dir = "";
            if(DriveInfo.GetDrives().Any())
            {
                foreach(var drive in DriveInfo.GetDrives())
                {
                    if(drive.IsReady)
                    {
                        if(Directory.Exists(Path.Combine(drive.Name, Path.Combine("emuiibo", "amiibo"))))
                        {
                            emuiibo_dir = Path.Combine(drive.Name, Path.Combine("emuiibo", "amiibo"));
                        }
                        else if(Directory.Exists(Path.Combine(drive.Name, "emuiibo")))
                        {
                            Directory.CreateDirectory(Path.Combine(drive.Name, Path.Combine("emuiibo", "amiibo")));
                            emuiibo_dir = Path.Combine(drive.Name, Path.Combine("emuiibo", "amiibo"));
                        }
                        if(!string.IsNullOrEmpty(emuiibo_dir))
                        {
                            MessageBox.Show($"Emuiibo directory was found in drive '{drive.VolumeLabel}', so defaulting to that directory.", DialogCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            var dialog = new FolderBrowserDialog
            {
                Description = "Select root directory to generate the virtual amiibo on",
                ShowNewFolderButton = false,
                SelectedPath = emuiibo_dir,
            };
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            return null;
        }

        private void CreateAmiibo(string name, string dir_name, string base_dir, AmiiboAPI.Amiibo cur_amiibo)
        {
            string out_path;
            bool use_last_path = LastPathCheck.Checked;
            if (use_last_path)
            {
                if (string.IsNullOrEmpty(LastUsedPath))
                {
                    use_last_path = false;
                }
            }

            bool save_to_ftp = FtpSaveCheck.Checked;
            IPAddress ftp_ip = null;
            int ftp_port = 0;

            // For FTP, use a temp directory to save the resulting files from amiibo.Save() before transfer
            var ftp_tmp_path = Path.Combine(Environment.CurrentDirectory, "temp_ftp");
            var ftp_sd_folder = "/emuiibo/amiibo/";
            if (!string.IsNullOrEmpty(dir_name)) ftp_sd_folder += $"{dir_name}/";

            if (save_to_ftp)
            {
                // Prepare FTP path
                out_path = Path.Combine(ftp_tmp_path, dir_name);

                // Validate the FTP address
                if (!IPAddress.TryParse(FtpAddressBox.Text, out ftp_ip))
                {
                    ShowErrorBox("FTP address is invalid");
                    return;
                }

                if (!int.TryParse(FtpPortBox.Text, out ftp_port))
                {
                    ShowErrorBox("FTP port is invalid");
                    return;
                }
            }
            else
            {
                if (use_last_path)
                {
                    out_path = Path.Combine(LastUsedPath, dir_name);
                }
                else
                {
                    out_path = Path.Combine(base_dir, dir_name);
                }
            }

            // Actually save the amiibo
            var amiibo = AmiiboUtils.BuildAmiibo(cur_amiibo, name);
            amiibo.Save(out_path, RandomizeUuidCheck.Checked, SaveImageCheck.Checked);


            // Special handling for FTP
            if (save_to_ftp)
            {
                var success = true;
                using (var client = new FtpClient(ftp_ip.ToString(), ftp_port, new NetworkCredential("", "")))
                {
                    client.ConnectTimeout = 1000;
                    client.Connect();
                    foreach (var file in Directory.GetFiles(out_path))
                    {
                        var file_name = Path.GetFileName(file);
                        // Upload each file created, creating directories along the way
                        var status = client.UploadFile(file, ftp_sd_folder + file_name, createRemoteDir: true);
                        if (status != FtpStatus.Success)
                        {
                            success = false;
                            break;
                        }
                    }
                    client.Disconnect();
                }

                ExceptionUtils.Unless(success, "Error during FTP upload, please try again");

                // Clean the temp directory
                Directory.Delete(ftp_tmp_path, true);
            }
            else
            {
                if (!use_last_path)
                {
                    // Update last used path
                    LastUsedPath = base_dir;
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                string base_dir = SelectDirectory();
                if(base_dir == null)
                {
                    // User cancelled
                    return;
                }

                if(!CreateAllCheck.Checked)
                {
                    var cur_amiibo = CurrentSeriesAmiibos[AmiiboComboBox.SelectedIndex];
                    if (string.IsNullOrEmpty(AmiiboNameBox.Text))
                    {
                        ShowErrorBox("No amiibo name was specified.");
                        return;
                    }

                    bool use_name_as_dir = UseNameCheck.Checked;
                    if (!use_name_as_dir && string.IsNullOrEmpty(DirectoryNameBox.Text))
                    {
                        ShowErrorBox("No amiibo directory name was specified.");
                        return;
                    }

                    string name = AmiiboNameBox.Text;
                    string dir_name = name;
                    if (!use_name_as_dir)
                    {
                        dir_name = DirectoryNameBox.Text;
                    }

                    CreateAmiibo(name, dir_name, base_dir, cur_amiibo);
                    MessageBox.Show("The virtual amiibo was successfully created.", DialogCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var actual_base_dir = base_dir;
                    AmiiboSeries = Amiibos.GetAmiiboSeries();
                    if (AmiiboSeries.Any())
                    {
                        foreach (var series in AmiiboSeries)
                        {
                            base_dir = Path.Combine(actual_base_dir, series);
                            FsUtils.RecreateDirectory(base_dir);
                            CurrentSeriesAmiibos = Amiibos.GetAmiibosBySeries(series);
                            if (CurrentSeriesAmiibos.Any())
                            {
                                foreach (var amiibo in CurrentSeriesAmiibos)
                                {
                                    CreateAmiibo(amiibo.AmiiboName, amiibo.AmiiboName, base_dir, amiibo);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Ey, no amiibos");
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ey, no series");
                    }
                    MessageBox.Show("All virtual amiibos were successfully created.", DialogCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ExceptionUtils.LogExceptionMessage(ex);
            }

            LastPathLabel.Visible = LastPathCheck.Visible = !string.IsNullOrEmpty(LastUsedPath);
            if (LastPathLabel.Visible)
            {
                LastPathLabel.Text = "Last path: " + LastUsedPath;
            }
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (RandomizeUuidCheck.Checked)
            {
                if (MessageBox.Show("Please, keep in mind that the random UUID feature might cause in some cases (Smash Bros., for example) the amiibo not to be recognized.\n(for example, when saving data to the amiibo, it could not be recognized as the original one)\n\nWould you really like to enable this feature?", "emutool - Randomize UUID", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    RandomizeUuidCheck.Checked = false;
                }
            }
        }

        private void CheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            DirectoryNameBox.Enabled = !UseNameCheck.Checked;
            if (!UseNameCheck.Checked)
            {
                CreateAllCheck.Checked = false;
            }
        }

        private void chkFTP_CheckedChanged(object sender, EventArgs e)
        {
            FtpAddressBox.Enabled = FtpSaveCheck.Checked;
            FtpPortBox.Enabled = FtpSaveCheck.Checked;
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("For more information about emuiibo, check it's GitHub repository's README.", DialogCaption, MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
            {
                Process.Start("https://github.com/XorTroll/emuiibo");
            }
        }

        private void generateAllAmibosCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (!CreateAllCheck.Checked)
            {
                AmiiboNameBox.Text = AmiiboComboBox.Text;
            }
            LastUsedPath = null;
            LastPathLabel.Visible = false;
            LastPathCheck.Checked = false;
            UseNameCheck.Enabled = !CreateAllCheck.Checked;
            AmiiboNameBox.Enabled = !CreateAllCheck.Checked;
            DirectoryNameBox.Enabled = !CreateAllCheck.Checked && !UseNameCheck.Checked;
            SeriesComboBox.Enabled = !CreateAllCheck.Checked;
            AmiiboComboBox.Enabled = !CreateAllCheck.Checked;
            AmiiboPictureBox.Visible = !CreateAllCheck.Checked;
        }
    }
}
