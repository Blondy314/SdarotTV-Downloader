﻿using System;
using System.Drawing;
using System.IO;

namespace SdarotTV_Downloader
{
    class Consts
    {
        public static string SERIES_NOT_FOUND = "Series not found";
        public static string SEARCH_CANCELED = "Search canceled";
        public static string SERIES_NO_EPISODES = "Series has no episodes available";

        public static string CHROME_REGISTRY_KEY = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe";
        public static string CHROME_DRIVERS_FOLDER = "chromedrivers\\";
        public static string CHROME_DRIVER_FILE = "chromedriver.exe";

        public static string SEARCH_URL = "search?term=";
        public static string SERIES_URL = "watch";
        public static string VIDEO_HTML_ID = "videojs_html5_api";
        public static string DEFAULT_DOWNLOAD_LOCATION = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        public static string APPDATA_LOCATION = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sdarot");
        public static string DOWNLOAD_LOCATION_FILE = Path.Combine(APPDATA_LOCATION, "DownloadLocation.txt");
        public static string MainPageLoginPanelButton = "//*[@id=\"slideText\"]/p/button";
        public static string MainPageFormUsername = "//*[@id=\"loginForm\"]/form/div[1]/div/input";
        public static string MainPageFormPassword = "//*[@id=\"loginForm\"]/form/div[2]/div/input";
        public static string MainPageLoginButton = "//*[@id=\"loginForm\"]/form/div[4]/button";
        public static string LoginMessage = "התחברות לאתר";

        public static int PB_FPS = 10;
        public static int PB_DURATION = 30;

        public static int MB = 1_000_000;
        public static int MAX_PATH_CHARS = 120;

        public static int IMAGE_WIDTH = 107;
        public static int IMAGE_HEIGHT = 158;
        public static int IMAGE_MARGIN = 10;

        public static bool HEADLESS_DRIVER = true;

        public static Color ERROR_COLOR = Color.FromArgb(254, 31, 34);
        public static Color INFO_COLOR = SystemColors.ControlText;
    }

    public enum SearchResult
    {
        NotFound = 0,
        Found = 1,
        NoEpisodes = 2,
        SearchCanceled = 3
    }
}
