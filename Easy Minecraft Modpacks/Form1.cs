using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Libraries;

namespace Easy_Minecraft_Modpacks
{
    public partial class Form1 : Form
    {
        public class Configuration
        {
            public List<ModInfo> Mods = new List<ModInfo>();
        }

        public class ModInfo
        {
            public string Version;
            public string Name;
            public string DownloadLink;
        }

        public static ConfigLib<Configuration> Config;

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var popup = new OpenFileDialog();
            popup.Filter = "Modpack Config|*.Modpack.json";

            if (popup.ShowDialog() == DialogResult.OK)
            {
                Config = new ConfigLib<Configuration>(popup.FileName);
                
                dataGridView1.Rows.Clear();

                foreach (var mod in Config.InternalConfig.Mods)
                {
                    dataGridView1.Rows.Add(mod.Version, mod.Name, mod.DownloadLink);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var mods = new List<ModInfo>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["Version"].Style.BackColor == Color.Red || row.Cells["ModName"].Style.BackColor == Color.Red || row.Cells["Download"].Style.BackColor == Color.Red)
                {
                    MessageBox.Show("Please fix the errors in formatting before saving.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(row.Cells["Version"]?.Value?.ToString()) || string.IsNullOrWhiteSpace(row.Cells["ModName"]?.Value?.ToString()) || string.IsNullOrWhiteSpace(row.Cells["Download"]?.Value?.ToString()))
                {
                    continue;
                }

                var mod = new ModInfo { Version = row.Cells["Version"].Value.ToString(), Name = row.Cells["ModName"].Value.ToString(), DownloadLink = row.Cells["Download"].Value.ToString() };
                mods.Add(mod);
            }

            if (mods.Count == 0)
            {
                MessageBox.Show("Save what? Thin air?", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (Config == null)
            {
                using var popup = new SaveFileDialog();
                popup.Filter = "Modpack Config|*.Modpack.json";
                popup.FileName = "NoName.Modpack.json";

                if (popup.ShowDialog() == DialogResult.OK)
                {
                    Config = new ConfigLib<Configuration>(popup.FileName);
                }
                else
                {
                    return;
                }
            }

            Config.InternalConfig.Mods = mods;
            Config.SaveConfig();

            MessageBox.Show("Done!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // regex check when a cell's text is modified, assuming textbox type of cell, and if not matched, mark it as a "error" cell
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    var cell = dataGridView1.Rows[e.RowIndex].Cells[column.Index];

                    var value = cell.Value?.ToString();

                    switch (column.Name)
                    {
                        case "Version":
                        {
                            if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, @"\d\.\d"))
                            {
                                cell.Style.BackColor = Color.Red;
                                cell.Style.SelectionBackColor = Color.Red;
                            }
                            else
                            {
                                cell.Style.BackColor = dataGridView1.DefaultCellStyle.BackColor;
                                cell.Style.SelectionBackColor = dataGridView1.DefaultCellStyle.SelectionBackColor;
                            }

                            break;
                        }
                        case "ModName":
                        {
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                cell.Style.BackColor = Color.Red;
                                cell.Style.SelectionBackColor = Color.Red;
                            }
                            else
                            {
                                cell.Style.BackColor = dataGridView1.DefaultCellStyle.BackColor;
                                cell.Style.SelectionBackColor = dataGridView1.DefaultCellStyle.SelectionBackColor;
                            }

                            break;
                        }
                        case "Download":
                        {
                            if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, "https://*"))
                            {
                                cell.Style.BackColor = Color.Red;
                                cell.Style.SelectionBackColor = Color.Red;
                            }
                            else
                            {
                                cell.Style.BackColor = dataGridView1.DefaultCellStyle.BackColor;
                                cell.Style.SelectionBackColor = dataGridView1.DefaultCellStyle.SelectionBackColor;
                            }

                            break;
                        }
                    }
                }
            }
        }

        private void installToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Please select your Minecraft directory.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select your Minecraft directory";

            var mcdir =$"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\.minecraft";
            
            folderDialog.SelectedPath = Directory.Exists(mcdir) ? mcdir : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                var minecraftDirectory = folderDialog.SelectedPath;
                // Insert your code here to install the modpack to the selected Minecraft directory
                // this will be done by renaming the existing mods folder if any, so the user doesn't lose old mods, then downloading all mods into the mods folder we create
                var modsFolder = Path.Combine(minecraftDirectory, "mods");
                
                if (Directory.Exists(modsFolder))
                {
                    RetryNaming:
                    var backupFolder = Path.Combine(minecraftDirectory, "mods_backup") + "_" + Guid.NewGuid().ToString("N");
                    
                    if (Directory.Exists(backupFolder))
                    {
                        goto RetryNaming;
                    }
                    
                    Directory.Move(modsFolder, backupFolder);
                }
                
                Directory.CreateDirectory(modsFolder);
                
                foreach (var mod in Config.InternalConfig.Mods)
                {
                    var modFileName = mod.DownloadLink.EndsWith(".jar") ? Path.GetFileName(mod.DownloadLink) : $"{mod.Name}-{mod.Version}.jar";
                    var modFilePath = Path.Combine(modsFolder, modFileName);

                    using var client = new WebClient();
                    
                    client.DownloadFile(mod.DownloadLink, modFilePath);
                }
            }
        }
    }
}