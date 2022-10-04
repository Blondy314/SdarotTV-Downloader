using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SdarotTV_Downloader
{
    public class SeriesWebDriver
    {
        private bool _cancel;

        public readonly IWebDriver webDriver;

        public SeriesWebDriver(IWebDriver webDriver)
        {
            this.webDriver = webDriver;
            Task.Run(() =>
            {
                webDriver.Navigate().GoToUrl(Properties.Settings.Default.Url);
            });
        }

        public void Quit()
        {
            webDriver.Quit();
        }

        public SearchResult SearchSeries(string seriesName)
        {
            string seriesUrl = Properties.Settings.Default.Url + Consts.SEARCH_URL + seriesName;
            bool doubleBack = false;
            webDriver.Navigate().GoToUrl(seriesUrl);
            if (webDriver.Url.Contains(Consts.SEARCH_URL))
            {
                var results = webDriver.FindElements(By.CssSelector("div.col-lg-2.col-md-2.col-sm-4.col-xs-6"));
                if (results.Count > 0)
                {
                    SearchResultsForm resultsForm = new SearchResultsForm(results);
                    DialogResult dialogResult = resultsForm.ShowDialog();
                    if (dialogResult == DialogResult.OK)
                    {
                        int resultIndex = resultsForm.resultIndex;
                        webDriver.Navigate().GoToUrl(results[resultIndex].FindElement(By.TagName("a")).GetAttribute("href"));
                        doubleBack = true;
                    }
                    else
                    {
                        webDriver.Navigate().Back();
                        return SearchResult.SearchCanceled;
                    }
                }
                else
                {
                    webDriver.Navigate().Back();
                    return SearchResult.NotFound;
                }
            }
            if (webDriver.Url.Contains(Consts.SERIES_URL))
            {
                if (GetSeasonsAmount() > 0)
                {
                    return SearchResult.Found;
                }
                else
                {
                    webDriver.Navigate().Back();
                    if (doubleBack)
                    {
                        webDriver.Navigate().Back();
                    }
                    return SearchResult.NoEpisodes;
                }
            }
            webDriver.Navigate().Back();
            if (doubleBack)
            {
                webDriver.Navigate().Back();
            }
            return SearchResult.NotFound;
        }

        public string GetSeriesName()
        {
            return webDriver.FindElement(By.XPath("//*[@id=\"watchEpisode\"]/div[1]/div/h1/strong/span")).Text;
        }

        public async Task<bool> IsLoggedIn()
        {
            await NavigateAsync(Properties.Settings.Default.Url);
            var loginPanelButton = await FindElementAsync(By.XPath(Consts.MainPageLoginPanelButton));
            return loginPanelButton != null ? loginPanelButton.Text != Consts.LoginMessage : throw new Exception(nameof(loginPanelButton));
        }

        async Task<IWebElement> FindElementAsync(By by, int timeout = 2)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return new WebDriverWait(webDriver, TimeSpan.FromSeconds(timeout)).Until(ExpectedConditions.ElementIsVisible(by));
                }
                catch
                {
                    return null;
                }
            });
        }

        public async Task<bool> Login(string username, string password)
        {
            if (await IsLoggedIn())
            {
                return true;
            }

            var loginPanelButton = await FindElementAsync(By.XPath(Consts.MainPageLoginPanelButton));
            if (loginPanelButton is null)
            {
                throw new Exception(nameof(loginPanelButton));
            }

            loginPanelButton.Click();

            var usernameInput = await FindElementAsync(By.XPath(Consts.MainPageFormUsername));
            if (usernameInput is null)
            {
                throw new Exception(nameof(usernameInput));
            }
            
            var passwordInput = await FindElementAsync(By.XPath(Consts.MainPageFormPassword));
            if (passwordInput is null)
            {
                throw new Exception(nameof(passwordInput));
            }

            usernameInput.SendKeys(username);
            passwordInput.SendKeys(password);
            var loginButton = await FindElementAsync(By.XPath(Consts.MainPageLoginButton));
            if (loginButton is null)
            {
                throw new Exception(nameof(loginButton));
            }

            await Task.Delay(1000);
            loginButton.Click();

            return await IsLoggedIn();
        }

        async Task NavigateAsync(string url) => await Task.Run(() => webDriver.Navigate().GoToUrl(url));

        public string[] GetSeasonsNames()
        {
            List<string> names = new List<string>();
            IWebElement seasonsList = webDriver.FindElement(By.Id("season"));
            int i = 0;
            foreach (var season in seasonsList.FindElements(By.TagName("a")))
            {
                if (GetSeasonEpisodesAmount(i) > 0)
                {
                    names.Add(season.Text);
                }
                i++;
            }
            return names.ToArray();
        }

        private void NavigateToSeason(int seasonIndex)
        {
            var buttons = webDriver.FindElements(By.TagName("button"));
            var button = buttons.FirstOrDefault(b => b.Text.Contains("תן לי לצפות"));
            if (button != null)
            {
                button.Click();
            }

            webDriver.FindElement(By.Id("season")).FindElements(By.TagName("li"))[seasonIndex].Click();
        }

        public string[] GetSeasonEpisodesNames(int seasonIndex)
        {
            List<string> names = new List<string>();
            NavigateToSeason(seasonIndex);
            IWebElement episodesList = webDriver.FindElement(By.Id("episode"));
            foreach (var episode in episodesList.FindElements(By.TagName("a")))
            {
                names.Add(episode.Text);
            }
            return names.ToArray();
        }

        private int GetSeasonsAmount()
        {
            return GetSeasonsNames().Length;
        }

        public int GetSeasonEpisodesAmount(int seasonIndex)
        {
            return GetSeasonEpisodesNames(seasonIndex).Length;
        }

        public void NavigateToEpisode(Episode episode)
        {
            NavigateToSeason(episode.seasonIndex);
            webDriver.FindElement(By.Id("episode")).FindElements(By.TagName("li"))[episode.episodeIndex].Click();
        }

        public void DownloadEpisodes(int seasonIndex, int episodeIndex, int episodeAmount, string downloadLocation, string seasonName = "")
        {
            DownloadForm downloadForm = new DownloadForm(this, seasonIndex, episodeIndex, episodeAmount, downloadLocation, seasonName);
            downloadForm.FormClosed += DownloadForm_FormClosed;
            downloadForm.Show();
        }

        private void DownloadForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _cancel = true;
        }

        public void DownloadSeason(int seasonIndex, string downloadLocation, string seasonName)
        {
            DownloadEpisodes(seasonIndex, 0, GetSeasonEpisodesAmount(seasonIndex), downloadLocation, seasonName);
        }

        public void DownloadSeries(string downloadLocation)
        {
            string[] seasonNames = GetSeasonsNames();
            _cancel = false;

            for (int i = 0; i < seasonNames.Length; i++)
            {
                DownloadSeason(i, downloadLocation, seasonNames[i]);
                if (_cancel)
                {
                    break;
                }
            }
        }
    }
}
