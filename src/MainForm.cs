using Microsoft.WindowsAPICodePack.Dialogs;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace SdarotTV_Downloader
{
    public partial class MainForm : Form
    {
        private string _lastEpisode;
        private SeriesWebDriver seriesDriver;
        private static string downloadLocation;

        public MainForm()
        {
            try
            {
                InitializeComponent();

                if (!Directory.Exists(Consts.APPDATA_LOCATION))
                {
                    Directory.CreateDirectory(Consts.APPDATA_LOCATION);
                }

                downloadLocation = Consts.DEFAULT_DOWNLOAD_LOCATION;

                if (File.Exists(Consts.DOWNLOAD_LOCATION_FILE))
                {
                    downloadLocation = File.ReadAllText(Consts.DOWNLOAD_LOCATION_FILE);
                }

                DownloadLocation_Label.Text = downloadLocation;

                VersionTitle_Label.Text = "v1.0.2";

                Load += MainForm_Load;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                FindSuitableDriver();

                seriesDriver = CreateChromeDriver();

                var series = GetAllSeries();
                if (series == null)
                {
                    return;
                }

                lstSeries.Items.AddRange(series);

                lstSeries.SelectedIndexChanged += (s, _) => Search_TextBox.Text = (string)lstSeries.SelectedItem;

                await LoginToWebsite(seriesDriver);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        private static void FindSuitableDriver()
        {
            var dir = Path.GetDirectoryName(Application.ExecutablePath);
            string chromeVersion = Utils.GetExecutableBaseVersion(Utils.GetChromePath());
            var driverFile = Path.Combine(dir, Consts.CHROME_DRIVER_FILE);
            if (!File.Exists(driverFile))
            {
                string srcDriver = Path.Combine(dir, Consts.CHROME_DRIVERS_FOLDER + chromeVersion + ".exe");
                File.Copy(srcDriver, driverFile, true);
            }
        }

        private static SeriesWebDriver CreateChromeDriver()
        {
            ChromeDriverService driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            ChromeOptions chromeOptions = new ChromeOptions();
            if (Consts.HEADLESS_DRIVER)
            {
                chromeOptions.AddArgument("headless");
            }
            IWebDriver chromeDriver = new ChromeDriver(driverService, chromeOptions);
            return new SeriesWebDriver(chromeDriver);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                seriesDriver?.Quit();
            }
            catch
            {

            }
        }

        private async Task LoginToWebsite(SeriesWebDriver driver)
        {
            if (await driver.IsLoggedIn())
            {
                return;
            }

            lblLogin.Text = "Not logged in";

            var username = Properties.Settings.Default.User;
            var password = Properties.Settings.Default.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return;
            }

            lblLogin.Text = "Logging in as " + username;

            if (!await driver.Login(username, password))
            {
                throw new Exception("Login failed");
            }

            lblLogin.Text = "Logged in as " + username;
        }

        private void Search_Button_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Search_TextBox.Text))
            {
                Error("Enter search string");
                return;
            }

            Task.Run(() =>
            {
                SearchForSeries();
            });
        }

        private void SearchForSeries()
        {
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    Search_Button.Enabled = false;
                    Info($"Searching for {Search_TextBox.Text}..");
                });

                _lastEpisode = null;

                SearchResult sr = seriesDriver.SearchSeries(Search_TextBox.Text);

                Invoke((MethodInvoker)delegate
                {
                    switch (sr)
                    {
                        case SearchResult.NotFound:
                            Error(Consts.SERIES_NOT_FOUND);
                            break;

                        case SearchResult.Found:
                            Info("Loading series..");
                            break;

                        case SearchResult.NoEpisodes:
                            Error(Consts.SERIES_NO_EPISODES);
                            break;

                        case SearchResult.SearchCanceled:
                            Info(Consts.SEARCH_CANCELED);
                            break;

                        default:
                            Info(sr.ToString());
                            break;
                    }
                });

                if (sr == SearchResult.Found)
                {
                    SeriesFound();
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
            finally
            {
                Invoke((MethodInvoker)delegate
                {
                    Search_Button.Enabled = true;
                });
            }
        }

        private void SeriesFound()
        {
            string seriesName = seriesDriver.GetSeriesName();
            string[] seasonsNames = seriesDriver.GetSeasonsNames();

            _lastEpisode = GetLastEpisode(seriesName);

            Invoke((MethodInvoker)delegate
            {
                SeriesName_Label.Text = seriesName;
                FirstEpisodeSeason_ComboBox.Items.Clear();
                FirstEpisodeSeason_ComboBox.Items.AddRange(seasonsNames);
                FirstEpisodeSeason_ComboBox.SelectedIndex = 0;
                DownloadEpisodes_RadioButton.Checked = true;
                EpisodesAmount_NumericUpDown.Value = 1;
                Download_Panel.Enabled = true;

                if (_lastEpisode != null)
                {
                    try
                    {
                        var season = Convert.ToInt32(_lastEpisode.Split(' ')[1].Split('E')[0].Replace("S", ""));
                        FirstEpisodeSeason_ComboBox.SelectedIndex = season - 1;
                    }
                    catch
                    {

                    }
                }

                Info("");
            });
        }

        private void FirstEpisodeSeason_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            DownloadMethod_GroupBox.Enabled = false;
            FirstEpisode_GroupBox.Enabled = false;
            Task.Run(() =>
            {
                ReloadEpisodesList();
                ReloadEpisodesAmount();
                Invoke((MethodInvoker)delegate
                {
                    FirstEpisode_GroupBox.Enabled = true;
                    DownloadMethod_GroupBox.Enabled = true;
                });
            });
        }

        private void ReloadEpisodesAmount()
        {
            if (DownloadSeason_RadioButton.Checked)
            {
                int seasonIndex = 0;
                Invoke((MethodInvoker)delegate
                {
                    seasonIndex = FirstEpisodeSeason_ComboBox.SelectedIndex;
                });
                int episodesAmount = seriesDriver.GetSeasonEpisodesAmount(seasonIndex);
                Invoke((MethodInvoker)delegate
                {
                    EpisodesAmount_NumericUpDown.Maximum = episodesAmount;
                    EpisodesAmount_NumericUpDown.Value = episodesAmount;
                });
            }
            else if (DownloadEpisodes_RadioButton.Checked)
            {
                Invoke((MethodInvoker)delegate
                {
                    EpisodesAmount_NumericUpDown.Maximum = 100;
                    EpisodesAmount_NumericUpDown.Value = 1;
                });
            }
            else if (DownloadSeries_RadioButton.Checked)
            {
                Invoke((MethodInvoker)delegate
                {
                    EpisodesAmount_NumericUpDown.Maximum = 100;
                    EpisodesAmount_NumericUpDown.Value = 1;
                });
            }
        }

        private void ReloadEpisodesList()
        {
            int seasonIndex = 0;
            Invoke((MethodInvoker)delegate
            {
                seasonIndex = FirstEpisodeSeason_ComboBox.SelectedIndex;
            });
            string[] episodesNames = seriesDriver.GetSeasonEpisodesNames(seasonIndex);
            Invoke((MethodInvoker)delegate
            {
                FirstEpisodeEpisode_ComboBox.Items.Clear();
                FirstEpisodeEpisode_ComboBox.Items.AddRange(episodesNames);
                FirstEpisodeEpisode_ComboBox.SelectedIndex = 0;

                if (_lastEpisode != null)
                {
                    try
                    {
                        var episode = Convert.ToInt32(_lastEpisode.Split(' ')[1].Split('E')[1]);
                        FirstEpisodeEpisode_ComboBox.SelectedIndex = episode - 1;
                    }
                    catch
                    {

                    }
                }
            });
        }

        private void DownloadEpisodes_RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (DownloadEpisodes_RadioButton.Checked)
            {
                DownloadMethod_GroupBox.Enabled = false;
                FirstEpisode_GroupBox.Enabled = false;
                Task.Run(() =>
                {
                    ReloadEpisodesList();
                    ReloadEpisodesAmount();
                    Invoke((MethodInvoker)delegate
                    {
                        FirstEpisodeSeason_ComboBox.Enabled = true;
                        FirstEpisodeEpisode_ComboBox.Enabled = true;
                        EpisodesAmount_NumericUpDown.Enabled = true;
                        FirstEpisode_GroupBox.Enabled = true;
                        DownloadMethod_GroupBox.Enabled = true;
                    });
                });
            }
        }

        private void DownloadSeason_RadioNutton_CheckedChanged(object sender, EventArgs e)
        {
            if (DownloadSeason_RadioButton.Checked)
            {
                DownloadMethod_GroupBox.Enabled = false;
                FirstEpisode_GroupBox.Enabled = false;
                EpisodesAmount_NumericUpDown.Enabled = false;
                Task.Run(() =>
                {
                    ReloadEpisodesList();
                    ReloadEpisodesAmount();
                    Invoke((MethodInvoker)delegate
                    {
                        FirstEpisodeSeason_ComboBox.Enabled = true;
                        FirstEpisodeEpisode_ComboBox.Enabled = false;
                        FirstEpisode_GroupBox.Enabled = true;
                        DownloadMethod_GroupBox.Enabled = true;
                    });
                });
            }
        }

        private void DownloadSeries_RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (DownloadSeries_RadioButton.Checked)
            {
                DownloadMethod_GroupBox.Enabled = false;
                FirstEpisode_GroupBox.Enabled = false;
                EpisodesAmount_NumericUpDown.Enabled = false;
                Task.Run(() =>
                {
                    ReloadEpisodesList();
                    ReloadEpisodesAmount();
                    Invoke((MethodInvoker)delegate
                    {
                        FirstEpisodeSeason_ComboBox.SelectedIndex = 0;
                        FirstEpisodeSeason_ComboBox.Enabled = false;
                        FirstEpisodeEpisode_ComboBox.Enabled = false;
                        FirstEpisode_GroupBox.Enabled = true;
                        DownloadMethod_GroupBox.Enabled = true;
                    });
                });
            }
        }

        private string[] GetAllSeries()
        {
            var path = downloadLocation;
            if (!Directory.Exists(path))
            {
                return null;
            }

            return Directory.EnumerateDirectories(path).Select(d => Path.GetFileName(d)).ToArray();
        }

        private string GetLastEpisode(string series)
        {
            var path = Path.Combine(downloadLocation, Utils.SanitizePath(series));
            if (!Directory.Exists(path))
            {
                return null;
            }

            var season = Directory.EnumerateDirectories(path).Max();
            path = Path.Combine(path, season);
            if (!Directory.Exists(path))
            {
                return null;
            }

            return Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(path).Max());
        }

        private void Download_Button_Click(object sender, EventArgs e)
        {
            if (DownloadEpisodes_RadioButton.Checked)
            {
                seriesDriver.DownloadEpisodes(FirstEpisodeSeason_ComboBox.SelectedIndex, FirstEpisodeEpisode_ComboBox.SelectedIndex, Convert.ToInt32(EpisodesAmount_NumericUpDown.Value), downloadLocation);
                return;
            }

            if (DownloadSeason_RadioButton.Checked)
            {
                seriesDriver.DownloadSeason(FirstEpisodeSeason_ComboBox.SelectedIndex, downloadLocation, seriesDriver.GetSeasonsNames()[FirstEpisodeSeason_ComboBox.SelectedIndex]);
                return;
            }

            if (DownloadSeries_RadioButton.Checked)
            {
                seriesDriver.DownloadSeries(downloadLocation);
            }
        }

        private void ChangeDirectory_Button_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                downloadLocation = dialog.FileName;

                File.WriteAllText(Consts.DOWNLOAD_LOCATION_FILE, downloadLocation);

                DownloadLocation_Label.Text = Utils.TruncateString(downloadLocation, Consts.MAX_PATH_CHARS);
            }
        }

        private void Search_TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)System.Windows.Forms.Keys.Enter)
            {
                Task.Run(() =>
                {
                    SearchForSeries();
                });
            }
        }

        private void Info(string info)
        {
            InfoMessage_Label.ForeColor = Consts.INFO_COLOR;
            InfoMessage_Label.Text = info;
        }

        private void Error(string error)
        {
            InfoMessage_Label.ForeColor = Consts.ERROR_COLOR;
            InfoMessage_Label.Text = error.Length > 100 ? error.Substring(0, 100) : error;
        }
    }
}
