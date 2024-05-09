using System;

public class SettingsManager
{
    private string _averageRatingSetting;
    private string _routesSetting;
    private int _payPerMileSetting;

    private string filePath = "scripts/UberMod.ini";

    public string AverageRatingSetting => _averageRatingSetting;

    public string RoutesSetting => _routesSetting;

    public int PayPerMileSetting => _payPerMileSetting;

    public SettingsManager()
    {
        var statsFromFile = LoadStatsFromFile();

        _averageRatingSetting = statsFromFile.averageRatingSetting;
        _routesSetting = statsFromFile.routesSetting;
        _payPerMileSetting = statsFromFile.payPerMileSetting;
    }

    private (string averageRatingSetting, string routesSetting, int payPerMileSetting) LoadStatsFromFile()
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith($"[SETTINGS]"))
                {

                    string[] averageRatingSetting = lines[i + 1].Split('=');
                    _averageRatingSetting = averageRatingSetting[1];

                    string[] routesSetting = lines[i + 2].Split('=');
                    _routesSetting = routesSetting[1];

                    string[] PayPerMileSetting = lines[i + 3].Split('=');
                    _payPerMileSetting = int.Parse(PayPerMileSetting[1]);

                    return (_averageRatingSetting, _routesSetting, _payPerMileSetting);
                }
            }
        }

        catch (Exception ex)
        {
            Log("Error on loading: " + ex.Message);
        }

        return ("Enabled", "City", 50);
    }

    public void SaveSettingsToFile()
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("[SETTINGS]"))
                {
                    lines[i + 1] = $"AverageRating={_averageRatingSetting}";
                    lines[i + 2] = $"Routes={_routesSetting}";

                    break;
                }
            }

            File.WriteAllLines(filePath, lines);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error on saving: " + ex.Message);
        }
    }

    public void SettingsHaveChanged(string currentAverageRating, string currentRoutesSetting)
    {
        if (currentAverageRating != _averageRatingSetting)
        {
            _averageRatingSetting = currentAverageRating;
            SaveSettingsToFile();
        }

        if (currentRoutesSetting != _routesSetting)
        {
            _routesSetting = currentRoutesSetting;
            SaveSettingsToFile();
        }
    }

    private void Log(string message)
    {
        GTA.UI.Notification.Show(message);
    }
}

