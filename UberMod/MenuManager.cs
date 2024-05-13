using NativeUI;

public class MenuManager
{
    private MenuPool _menuPool;

    private UIMenu _mainMenu;
    private UIMenu _statisticsMenu;
    private UIMenu _settingsMenu;

    public UIMenuListItem _statusItem;
    public UIMenuListItem _averageRatingItem;
    private UIMenuListItem _routesItem;

    public readonly string[] _statusOptions = { "Unavailable", "Available" };
    public readonly string[] _averageRatingOptions = { "Enabled", "Disabled" };
    private readonly string[] _routesOptions = { "Everywhere", "City", "City <-> Countryside", "Countryside" };

    private readonly string filePath = "scripts/UberMod.ini";

    public float averageRating;
    private int totalJobs;
    private int totalEarnings;

    private string averageRatingSetting;
    public string routesSetting;
    public int payPerMileSetting;

    public MenuManager()
    {
        // This class is handling all the menu functionality

        // Loading the data from the file on initialization
        LoadDataFromFile();

        // Creating the menus on initialization
        CreateMainMenu();
        CreateStatisticsMenu();
        CreateSettingsMenu();
    }
    private void LoadDataFromFile()
    {
        // This method is for loading all the needed data from the .ini file

        // Here we are loading the settings data
        string[] settings = File.ReadAllLines(filePath);

        for (int i = 0; i < settings.Length; i++)
        {
            if (settings[i].StartsWith($"[SETTINGS]"))
            {

                string[] _averageRatingSetting = settings[i + 1].Split('=');
                averageRatingSetting = _averageRatingSetting[1];

                string[] _routesSetting = settings[i + 2].Split('=');
                routesSetting = _routesSetting[1];

                string[] _PayPerMileSetting = settings[i + 3].Split('=');
                payPerMileSetting = int.Parse(_PayPerMileSetting[1]);

                break;
            }
        }

        // Here we are loading the stats data
        string[] stats = File.ReadAllLines(filePath);

        for (int i = 0; i < stats.Length; i++)
        {
            if (stats[i].StartsWith($"[STATS]"))
            {

                string[] _averageRating = stats[i + 1].Split('=');
                averageRating = float.Parse(_averageRating[1]);

                string[] _totalJobs = stats[i + 2].Split('=');
                totalJobs = int.Parse(_totalJobs[1]);

                string[] _totalEarnings = stats[i + 3].Split('=');
                totalEarnings = int.Parse(_totalEarnings[1]);
                    
                break;
            }
        }
    }

    public void SaveStatsToFile()
    {
        // This method is saving the stats to the file after every completed job
        try
        {
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("[STATS]"))
                {
                    lines[i + 1] = $"AverageRating={averageRating:F1}";
                    lines[i + 2] = $"TotalJobs={totalJobs}";
                    lines[i + 3] = $"TotalEarnings={totalEarnings}";

                    break;
                }
            }

            File.WriteAllLines(filePath, lines);
        }
        catch (Exception ex)
        {
            GTA.UI.Notification.Show("Error on saving: " + ex.Message);
        }
    }

    public void UpdateStats(int earnings, float rating)
    {
        // This method is updating the stats data after every job

        totalJobs++;
        totalEarnings += earnings;
        averageRating = ((averageRating * totalJobs) + rating) / (totalJobs + 1);
    }

    private void SaveSettingsToFile()
    {
        // This method is saving the settings to the file every time a change is made
        // Please note that the payment setting is not added in the menu, it should only be changed from the file

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("[SETTINGS]"))
                {
                    lines[i + 1] = $"AverageRating={averageRatingSetting}";
                    lines[i + 2] = $"Routes={routesSetting}";

                    break;
                }
            }

            File.WriteAllLines(filePath, lines);
        }
        catch (Exception ex)
        {
            GTA.UI.Notification.Show("Error on saving: " + ex.Message);
        }
    }

    public void SettingsHaveChanged()
    {
        // This method is called every frame to check if changes were made to the settings menu
        // If some changes were made, they are immediately saved to the file

        string currentAverageRating = _averageRatingOptions[_averageRatingItem.Index];
        string currentRoutesSetting = _routesOptions[_routesItem.Index];

        // Basically we are taking the current value from the menu and checking if the loaded value is the same
        // If it's not, we are setting the loaded value to be the current value and saving this to the file
        if (currentAverageRating != averageRatingSetting)
        {
            averageRatingSetting = currentAverageRating;
            SaveSettingsToFile();
        }

        if (currentRoutesSetting != routesSetting)
        {
            routesSetting = currentRoutesSetting;
            SaveSettingsToFile();
        }
    }

    public void UpdateStatsMenu()
    {
        // This method is updating the stats data in the menu, so when the player does a
        // job and check the stats tab in the menu they will be properly updated

        _statisticsMenu.MenuItems[0].SetRightLabel(averageRating.ToString("F1"));
        _statisticsMenu.MenuItems[1].SetRightLabel(totalJobs.ToString());
        _statisticsMenu.MenuItems[2].SetRightLabel(totalEarnings.ToString());
    }

    private void CreateMainMenu()
    {
        // This method is creating the main menu

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
        // This method is creating the Stats menu

        _statisticsMenu = new UIMenu("Statistics", "Check your stats.");

        _statisticsMenu.AddItem(new UIMenuItem("Average rating:", "Your average rating from all jobs."));
        _statisticsMenu.AddItem(new UIMenuItem("Total jobs:", "Total jobs done."));
        _statisticsMenu.AddItem(new UIMenuItem("Total earnings:", "Total money gained from all jobs."));

        _statisticsMenu.MenuItems[0].SetRightLabel(averageRating.ToString("F1"));
        _statisticsMenu.MenuItems[1].SetRightLabel(totalJobs.ToString());
        _statisticsMenu.MenuItems[2].SetRightLabel(totalEarnings.ToString());

        _statisticsMenu.ParentMenu = _mainMenu;
        _menuPool.Add(_statisticsMenu);
    }

    private void CreateSettingsMenu()
    {
        // This method is creating the settings menu

        _settingsMenu = new UIMenu("Settings", "Change settings.");

        List<dynamic> averageRatingOptions = new(_averageRatingOptions);
        List<dynamic> routesOptions = new(_routesOptions);

        var indexes = LoadIndexFromFile();
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

        _settingsMenu.ParentMenu = _mainMenu;

        _menuPool.Add(_settingsMenu);
        _settingsMenu.RefreshIndex();
    }

    private (int ratingIndex, int routeIndex) LoadIndexFromFile ()
    {
        // This method is setting the indexes for the settings menu after loading the data from the file
        // It basically gets the loaded data and determines which option should be preselected when the player opens the menu

        int ratingIndex;
        int routeIndex;

        if (averageRatingSetting == "Enabled") { ratingIndex = 0; }
        else { ratingIndex = 1; }

        if (routesSetting == "Everywhere") { routeIndex = 0; }
        else if (routesSetting == "City") { routeIndex = 1; }
        else if (routesSetting == "City <-> Countryside") { routeIndex = 2; }
        else { routeIndex = 3; }

        return (ratingIndex, routeIndex);
    }

    private void MainMenuItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
    {
        // This method is handlind the main menu selections

        // If the player selects the Stats tab, it will go to the stats
        if (selectedItem.Text == "Statistics")
        {
            _mainMenu.Visible = false;
            _statisticsMenu.Visible = true;
        }

        // If the player selects the Settings tab, it will go to the settings
        else if (selectedItem.Text == "Settings")
        {
            _mainMenu.Visible = false;
            _settingsMenu.Visible = true;
        }
    }

    public void ShowMainMenu()
    {
        // This method is showing the main menu, when the player calls the Uber contact from their phone

        _mainMenu.Visible = true;
    }

    public void ProcessMenus()
    {
        // This method is processing all the menus and updating them as needed

        _menuPool.ProcessMenus();
    }
}
