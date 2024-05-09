using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Security.Cryptography.X509Certificates;
using NativeUI;

public class MenuManager
{
    private MenuPool _menuPool;
    private StatsManager _statsManager;
    private SettingsManager _settingsManager;

    private UIMenu _mainMenu;
    private UIMenu _statisticsMenu;
    private UIMenu _settingsMenu;

    private UIMenuListItem _statusItem;
    private UIMenuListItem _averageRatingItem;
    private UIMenuListItem _routesItem;

    private string[] _statusOptions = { "Unavailable", "Available" };
    private string[] _averageRatingOptions = { "Enabled", "Disabled" };
    private string[] _routesOptions = { "City", "City <-> Countryside", "Countryside" };

    public MenuManager(StatsManager statsManager, SettingsManager settingsManager)
    {
        _statsManager = statsManager;
        _settingsManager = settingsManager;

        CreateMainMenu();
        CreateStatisticsMenu();
        CreateSettingsMenu();
    }

    public string GetStatus()
    {
        return _statusOptions[_statusItem.Index];
    }

    public void UpdateStatus(int index)
    {
        _statusItem.Index = index;
    }

    public string GetAverateRatingSetting()
    {
        return _averageRatingOptions[_averageRatingItem.Index];
    }

    public string GetRoutesSetting()
    {
        return _routesOptions[_routesItem.Index];
    }

    public void UpdateStatsMenu()
    {
        _statisticsMenu.MenuItems[0].SetRightLabel(_statsManager.AverageRating.ToString("F1"));
        _statisticsMenu.MenuItems[1].SetRightLabel(_statsManager.TotalJobs.ToString());
        _statisticsMenu.MenuItems[2].SetRightLabel(_statsManager.TotalEarnings.ToString());
    }

    private void CreateMainMenu()
    {
        _menuPool = new MenuPool();
        _mainMenu = new UIMenu("Uber Driver", "Welcome to Uber Driver!");

        List<dynamic> statusOptions = new List<dynamic>(_statusOptions);
        _statusItem = new UIMenuListItem("Status", statusOptions, 0, "Change your status.");

        var statisticsItem = new UIMenuItem("Statistics", "Check your stats.");
        var settingsItem = new UIMenuItem("Settings", "Change settings.");

        _mainMenu.AddItem(_statusItem);
        _mainMenu.AddItem(statisticsItem);
        _mainMenu.AddItem(settingsItem);

        statisticsItem.SetRightLabel(">>>");
        settingsItem.SetRightLabel(">>>");

        _mainMenu.OnItemSelect += MainMenuItemSelected;
        _menuPool.Add(_mainMenu);
        _mainMenu.RefreshIndex();
    }

    private void CreateStatisticsMenu()
    {
        _statisticsMenu = new UIMenu("Statistics", "Check your stats.");

        _statisticsMenu.AddItem(new UIMenuItem("Average rating:", "Your average rating from all jobs."));
        _statisticsMenu.AddItem(new UIMenuItem("Total jobs:", "Total jobs done."));
        _statisticsMenu.AddItem(new UIMenuItem("Total earnings:", "Total money gained from all jobs."));

        _statisticsMenu.MenuItems[0].SetRightLabel(_statsManager.AverageRating.ToString("F1"));
        _statisticsMenu.MenuItems[1].SetRightLabel(_statsManager.TotalJobs.ToString());
        _statisticsMenu.MenuItems[2].SetRightLabel(_statsManager.TotalEarnings.ToString());

        _statisticsMenu.ParentMenu = _mainMenu;
        _menuPool.Add(_statisticsMenu);
    }

    private void CreateSettingsMenu()
    {
        _settingsMenu = new UIMenu("Settings", "Change settings.");

        List<dynamic> averageRatingOptions = new(_averageRatingOptions);
        List<dynamic> routesOptions = new(_routesOptions);

        var indexes = LoadSettingsFromFile();
        int averageRatingIndex = indexes.ratingIndex;
        int routesIndex = indexes.routeIndex;

        _averageRatingItem = new UIMenuListItem(
            "Average Rating", 
            averageRatingOptions, 
            averageRatingIndex, 
            "Enable/Disable Averate Rating system."
            );

        _routesItem = new UIMenuListItem(
            "Routes", 
            routesOptions,
            routesIndex, 
            "Choose the routes you will be making."
            );

        _settingsMenu.AddItem(_averageRatingItem);
        _settingsMenu.AddItem(_routesItem);

        _settingsMenu.OnItemSelect += SettingsMenuItemSelected; // delete if not needed
        _settingsMenu.ParentMenu = _mainMenu;

        _menuPool.Add(_settingsMenu);
        _settingsMenu.RefreshIndex();
    }

    private (int ratingIndex, int routeIndex) LoadSettingsFromFile ()
    {
        int ratingIndex;
        int routeIndex;

        if (_settingsManager.AverageRatingSetting == "Enabled") { ratingIndex = 0; }
        else { ratingIndex = 1; }

        if (_settingsManager.RoutesSetting == "City") { routeIndex = 0; }
        else if (_settingsManager.RoutesSetting == "City <-> Countryside") { routeIndex = 1; }
        else { routeIndex = 2; }

        return (ratingIndex, routeIndex);
    }

    private void MainMenuItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
    {
        if (selectedItem.Text == "Statistics")
        {
            _mainMenu.Visible = false;
            _statisticsMenu.Visible = true;
        }

        else if (selectedItem.Text == "Settings")
        {
            _mainMenu.Visible = false;
            _settingsMenu.Visible = true;
        }
    }

    private void SettingsMenuItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
    {
        if (selectedItem.Text == "Option 1")
        {
            Log("Option 1 selected");
            // Handle option 1 selection
        }
        else if (selectedItem.Text == "Option 2")
        {
            Log("Option 2 selected");
            // Handle option 2 selection
        }
    }

    public void ShowMainMenu()
    {
        _mainMenu.Visible = true;
    }

    public void ProcessMenus()
    {
        _menuPool.ProcessMenus();
    }

    private void Log(string message)
    {
        GTA.UI.Notification.Show(message);
    }
}
