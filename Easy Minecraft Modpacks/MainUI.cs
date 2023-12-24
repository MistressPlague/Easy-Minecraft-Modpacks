using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Libraries;
using Newtonsoft.Json;

namespace Easy_Minecraft_Modpacks
{
    public partial class MainUI : Form
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
        private string FileExt = "Modpack.json";

        private static WebClient client = new WebClient();

        public MainUI()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!DoUnsavedChangesCheck())
            {
                return;
            }

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
                popup.FileName = $"NoName.{FileExt}";

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
                            if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, @"\d+\.\d+(\.\d)?"))
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

                                if (value.Contains("curseforge.com/api/v1/mods") && !value.Contains("/download"))
                                {
                                    cell.Value = cell.Value + "/download";
                                    value = cell.Value.ToString();
                                }

                                if (string.IsNullOrEmpty(dataGridView1.Rows[e.RowIndex].Cells[0].Value?.ToString()) && string.IsNullOrEmpty(dataGridView1.Rows[e.RowIndex].Cells[1].Value?.ToString()))
                                {
                                    var filename = client.GetFileName(value);

                                    var name = Regex.Match(filename, @"[a-zA-Z _\-]*").Value;
                                
                                    var modName = (!string.IsNullOrWhiteSpace(name) ? name : "").Replace("-", " ").Trim();
                                
                                    modName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(modName).Replace("Api", "API").Replace("Fabric", "").Replace("Forge", "").Trim();

                                    var verMatches = Regex.Matches(filename, @"\d+\.\d+(\.\d)?");
                                    var ver = verMatches[verMatches.Count - 1].Value;

                                    var modVersion = dataGridView1.Rows.Count > 1 && e.RowIndex > 0 ? dataGridView1.Rows[e.RowIndex - 1].Cells[0].Value?.ToString() ?? ver : (!string.IsNullOrWhiteSpace(ver) ? ver : "");

                                    dataGridView1.Rows[e.RowIndex].Cells[0].Value = modVersion;
                                    dataGridView1.Rows[e.RowIndex].Cells[1].Value = modName;
                                }
                            }

                            break;
                        }
                    }
                }
            }
        }

        private void installToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            installToolStripMenuItem.Enabled = false;

            MessageBox.Show("Please select your Minecraft directory.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select your Minecraft directory";

            var mcdir = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\.minecraft";

            folderDialog.SelectedPath = Directory.Exists(mcdir) ? mcdir : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                var minecraftDirectory = folderDialog.SelectedPath;

                var modsFolder = Path.Combine(minecraftDirectory, "mods");

                if (Directory.Exists(modsFolder))
                {
                    if (File.Exists($"{modsFolder}\\dont_backup"))
                    {
                        Directory.Delete(modsFolder, true);
                    }
                    else
                    {
                        RetryNaming:
                        var backupFolder = Path.Combine(minecraftDirectory, "mods_backup") + "_" + Guid.NewGuid().ToString("N");

                        if (Directory.Exists(backupFolder))
                        {
                            goto RetryNaming;
                        }

                        Directory.Move(modsFolder, backupFolder);
                    }
                }

                File.WriteAllText($"{modsFolder}\\dont_backup", "");

                MessageBox.Show("Done!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            installToolStripMenuItem.Enabled = true;
        }

        private async void generateFromModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!DoUnsavedChangesCheck())
            {
                return;
            }
            
            MessageBox.Show("Please select your Minecraft directory.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select your Minecraft directory";

            var mcdir = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\.minecraft";

            folderDialog.SelectedPath = Directory.Exists(mcdir) ? mcdir : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                Enabled = false;

                var minecraftDirectory = folderDialog.SelectedPath;

                var modsFolder = Path.Combine(minecraftDirectory, "mods");

                var mods = Directory.GetFiles(modsFolder, "*.jar", SearchOption.TopDirectoryOnly);

                dataGridView1.Rows.Clear();

                foreach (var modFile in mods)
                {
                    var uploadedFile = await GoFileAPI.UploadFile(modFile);

                    var modFileName = Path.GetFileNameWithoutExtension(modFile);

                    var name = Regex.Match(modFileName, @"[a-zA-Z _]*").Value;
                    var modName = !string.IsNullOrWhiteSpace(name) ? name : modFileName;

                    var ver = Regex.Match(modFileName, @"\d+\.\d+(\.\d)?").Value;

                    var modVersion = !string.IsNullOrWhiteSpace(ver) ? ver : "";

                    dataGridView1.Rows.Add(modVersion, modName, uploadedFile.Item2);
                }

                Enabled = true;
            }
        }

        private bool DoUnsavedChangesCheck()
        {
            var currmods = new List<ModInfo>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["Version"].Style.BackColor == Color.Red || row.Cells["ModName"].Style.BackColor == Color.Red || row.Cells["Download"].Style.BackColor == Color.Red)
                {
                    currmods.Add(new ModInfo());
                }

                if (string.IsNullOrWhiteSpace(row.Cells["Version"]?.Value?.ToString()) || string.IsNullOrWhiteSpace(row.Cells["ModName"]?.Value?.ToString()) || string.IsNullOrWhiteSpace(row.Cells["Download"]?.Value?.ToString()))
                {
                    continue;
                }

                var mod = new ModInfo { Version = row.Cells["Version"].Value.ToString(), Name = row.Cells["ModName"].Value.ToString(), DownloadLink = row.Cells["Download"].Value.ToString() };
                currmods.Add(mod);
            }

            if (currmods.Count > 0 && (Config == null || JsonConvert.SerializeObject(currmods) != JsonConvert.SerializeObject(Config.InternalConfig.Mods)))
            {
                if (MessageBox.Show("Are you sure you want to do this? You have unsaved changes that will be lost!", "Alert", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button2) == DialogResult.No)
                {
                    return false;
                }
            }

            return true;
        }

        private void MainUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            var mods = new List<ModInfo>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["Version"].Style.BackColor == Color.Red || row.Cells["ModName"].Style.BackColor == Color.Red || row.Cells["Download"].Style.BackColor == Color.Red)
                {
                    mods.Add(new ModInfo());
                }

                if (string.IsNullOrWhiteSpace(row.Cells["Version"]?.Value?.ToString()) || string.IsNullOrWhiteSpace(row.Cells["ModName"]?.Value?.ToString()) || string.IsNullOrWhiteSpace(row.Cells["Download"]?.Value?.ToString()))
                {
                    continue;
                }

                var mod = new ModInfo { Version = row.Cells["Version"].Value.ToString(), Name = row.Cells["ModName"].Value.ToString(), DownloadLink = row.Cells["Download"].Value.ToString() };
                mods.Add(mod);
            }

            if (mods.Count > 0 && (Config == null || JsonConvert.SerializeObject(mods) != JsonConvert.SerializeObject(Config.InternalConfig.Mods)))
            {
                if (MessageBox.Show("Are you sure you want to quit? You have unsaved changes!", "Alert", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button2) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void UpdateRows()
        {
            var intyes = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var Name = row.Cells[1]?.Value?.ToString();

                if (Name == null) continue;
                if (Name.ToLower().Contains(" api") || Name.ToLower().Contains(" config") || Name.ToLower().Contains(" lib")) continue;

                intyes++;
            }

            label2.Text = $"{dataGridView1.Rows.Count} ({intyes})";

        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
           UpdateRows();
        }

        private void dataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            UpdateRows();
        }

        private void dataGridView1_DragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("Found JSON ?");
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (!files[0].EndsWith(FileExt)) return;
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                var file = files[0];
                if (file.EndsWith(FileExt))
                {
                    Config = new ConfigLib<Configuration>(file);
                    dataGridView1.Rows.Clear();
                    foreach (var mod in Config.InternalConfig.Mods)
                    {
                        dataGridView1.Rows.Add(mod.Version, mod.Name, mod.DownloadLink);
                    }
                }
            }
        }
    }

    public static class Extensions
    {
        /// <summary>
        /// Downloads a file from the specified URL and saves it to the target directory.
        /// </summary>
        /// <param name="client">The WebClient instance used to download the file.</param>
        /// <param name="url">The URL of the file to download.</param>
        /// <param name="targetDir">The target directory where the downloaded file will be saved.</param>
        /// <returns>The filename of the downloaded file.</returns>
        public static string BetterDownloadFile(this WebClient client, string url, string targetDir)
        {
            var data = client.DownloadData(url);

            var redir = Workarounds.GetRedirectedUrl(url);

            var disposition = client.ResponseHeaders["Content-Disposition"];
            
            var locationEnding = redir?.Substring(redir.LastIndexOf("/", StringComparison.Ordinal) + 1);
            
            var hasQuery = locationEnding?.IndexOf("?", StringComparison.Ordinal) ?? -1;

            if (hasQuery != -1)
            {
                locationEnding = locationEnding?.Substring(0, hasQuery);
            }
                
            var urlEnding = url.Substring(url.LastIndexOf("/", StringComparison.Ordinal) + 1);
            
            var filename = Workarounds.UrlDecode(disposition != null ? new ContentDisposition(disposition).FileName : (locationEnding != null ? locationEnding : urlEnding));

            File.WriteAllBytes($"{targetDir}\\{filename}", data);

            return filename;
        }

        /// <summary>
        /// Retrieves the filename from the response headers of the given URL.
        /// </summary>
        /// <param name="client">The WebClient instance used to make the request.</param>
        /// <param name="url">The URL from which to extract the filename.</param>
        /// <returns>
        /// The filename extracted from the response headers of the given URL.
        /// </returns>
        public static string GetFileName(this WebClient client, string url)
        {
            client.DownloadData(url);

            var redir = Workarounds.GetRedirectedUrl(url);

            var disposition = client.ResponseHeaders["Content-Disposition"];
            
            var locationEnding = redir?.Substring(redir.LastIndexOf("/", StringComparison.Ordinal) + 1);
            
            var hasQuery = locationEnding?.IndexOf("?", StringComparison.Ordinal) ?? -1;

            if (hasQuery != -1)
            {
                locationEnding = locationEnding?.Substring(0, hasQuery);
            }
                
            var urlEnding = url.Substring(url.LastIndexOf("/", StringComparison.Ordinal) + 1);
            
            var filename = Workarounds.UrlDecode(disposition != null ? new ContentDisposition(disposition).FileName : (locationEnding != null ? locationEnding : urlEnding));

            return filename;
        }
    }

    public class Workarounds
    {
        public static string UrlDecode(string url)
        {
            return Uri.UnescapeDataString(url);
        }
        
        public static string GetRedirectedUrl(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            webRequest.AllowAutoRedirect = false;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.Redirect
                        || response.StatusCode == HttpStatusCode.MovedPermanently
                        || response.StatusCode == HttpStatusCode.TemporaryRedirect
                       )
                    {
                        return response.Headers["Location"];
                    }
                }
            }
            catch(System.Net.WebException e)
            {
                if(e.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = (HttpWebResponse)e.Response;
                    if(response.StatusCode == HttpStatusCode.Redirect
                       || response.StatusCode == HttpStatusCode.MovedPermanently
                       || response.StatusCode == HttpStatusCode.TemporaryRedirect
                      )
                    {
                        return response.Headers["Location"];
                    }
                }

                // Handle other cases if needed
            }

            // No redirect, return the original url
            return url;
        }
    }
}