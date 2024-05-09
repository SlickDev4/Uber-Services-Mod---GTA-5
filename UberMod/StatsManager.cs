using System;
using System.IO;
using GTA;

public class StatsManager
{
    private float _averageRating;
    private int _totalJobs;
    private int _totalEarnings;

    private string filePath = "scripts/UberMod.ini";

    public float AverageRating => _averageRating;
    public int TotalJobs => _totalJobs;
    public int TotalEarnings => _totalEarnings;

    public StatsManager()
    {
        var statsFromFile = LoadStatsFromFile();

        _averageRating = statsFromFile.averageRating;
        _totalJobs = statsFromFile.totalJobs;
        _totalEarnings = statsFromFile.totalEarnings;
    }

    private (float averageRating, int totalJobs, int totalEarnings) LoadStatsFromFile()
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith($"[STATS]"))
                {

                    string[] averageRating = lines[i + 1].Split('=');
                    _averageRating = float.Parse(averageRating[1]);

                    string[] totalJobs = lines[i + 2].Split('=');
                    _totalJobs = int.Parse(totalJobs[1]);

                    string[] totalEarnings = lines[i + 3].Split('=');
                    _totalEarnings = int.Parse(totalEarnings[1]);

                    return (_averageRating, _totalJobs, _totalEarnings);
                }
            }
        }

        catch (Exception ex)
        {
            Log("Error on loading: " + ex.Message);
        }

        return (0.0f, 0, 0);
    }

    public void SaveStatsToFile()
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("[STATS]"))
                {
                    lines[i + 1] = $"AverageRating={_averageRating:F1}";
                    lines[i + 2] = $"TotalJobs={_totalJobs}";
                    lines[i + 3] = $"TotalEarnings={_totalEarnings}";

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

    public void UpdateStats(int earnings, float rating)
    {
        _totalJobs++;
        _totalEarnings += earnings;
        UpdateAverageRating(rating);
    }

    private void UpdateAverageRating(float newRating)
    {
        // Calculate the new average rating based on the existing average and the new rating
        _averageRating = ((_averageRating * _totalJobs) + newRating) / (_totalJobs + 1);
    }

    private void Log(string message)
    {
        GTA.UI.Notification.Show(message);
    }
}

