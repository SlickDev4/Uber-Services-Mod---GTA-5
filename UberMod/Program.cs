using System;
using System.Drawing.Drawing2D;
using GTA;
using GTA.Math;
using GTA.UI;
using GTA.Native;
using GTA.NaturalMotion;
using System.Windows.Forms;
using iFruitAddon2;
using System.Diagnostics.Contracts;
using System.Drawing.Text;
using NativeUI;
using System.Drawing;
using System.Diagnostics;

public class UberMod : Script
{
    private MenuManager _menuManager;
    private ContactManager _contactManager;
    private StatsManager _statsManager;
    private SettingsManager _settingsManager;
    private PlayerManager _playerManager;
    private RideHandler _rideHandler;
    private Notifications _notifications;

    // delete when done with coordinates
    private LocationManager _locationManager;

    public UberMod()
    {
        try
        {
            _statsManager = new StatsManager();
            _settingsManager = new SettingsManager();

            _menuManager = new MenuManager(_statsManager, _settingsManager);
            _contactManager = new ContactManager(_menuManager);
            _playerManager = new PlayerManager(_menuManager);
            _notifications = new Notifications();

            _rideHandler = new RideHandler(_playerManager, _statsManager, _settingsManager, _menuManager, _notifications);

            // delete when done with coordinates
            _locationManager = new LocationManager();

            _contactManager.AddContact();

            Tick += OnTick;
            Aborted += OnScriptAborted;
        }
        catch (Exception ex) 
        {
            GTA.UI.Notification.Show($"Error on loading - {ex}");
        }
        
    }

    private void OnTick(object sender, EventArgs e)
    {
        // TODO:

        // - create coordinates - settings menu choose between city and countryside

        // MONITOR:

        // - 

        // LATER
        // - put comments in the code and also clean it up for better readability - at the end after all is done!
        // - check how to rename all occurences of something in the code / rename classes a bit - too much managers :D
        // - check how to get a license if providing the code to the mod
        // - remove Log methods from StatsManager and MenuManager and replace them with notification class
        // - optimize code with returns where possible
        // - add more customer reactions on crash, edit with more realistic ones
        // - simplify if else statements if they are only 1 if and 1 else and where possible
        // - fix all classes to load in program.cs and then pass what is needed to other classes

        // NOTES
        // - could not fix menu freeze <= 1 second on first open
        // - could not fix disappearing route - workaround with update every second which causes it to blink on the map
        // - when turning off the average rating, it will give you your current average rating every ride, which will impact
        //      your tips, you can always change the current average rating in the .ini file,
        //      it has to be 1-5 (eg. 1.5, 2.5, 3.7) or the script won't work properly
        // - sometimes distance calculation bugs and it says that the distance 60 miles and wants to give a 3k payment
        //      it happens rarely and in this occasions, the distance is calculated with another method, which will 
        //      pay 30-60 $ less but it's better than getting 3k for 2 miles..

        try
        {
            _contactManager.UpdateIFruit();
            _menuManager.ProcessMenus();

            // checking for changes in the settings and saving to file if needed.
            var currentAverageRating = _menuManager.GetAverateRatingSetting();
            var currentRoutesSetting = _menuManager.GetRoutesSetting();

            _settingsManager.SettingsHaveChanged(currentAverageRating, currentRoutesSetting);

            string status = _playerManager.getPlayerStatus();
            Vector3 playerPos = _playerManager.getPlayerPosition();

            // this should be under available status after testing period
            Vehicle playerVehicle = _playerManager.GetPlayerVehicle();

            // delete after testing is done
            Testing(playerPos, playerVehicle);

            // Handle Logic when Player is available
            if (status == "available")
            {

                _rideHandler.PlayAsUberDriver(playerPos, playerVehicle);


                // delete when done with coordinates
                _locationManager.RemoveAllBlips();
            }

            // Handle Logic when Player is not playing the mod
            if (status == "unavailable")
            {
                _rideHandler.DeleteEverything();

                // delete when done with coordinates
                _locationManager.CreateBlipsForMarkers();
            }

            // If the Player gets a wanted level sets or changes the player the status to unavailable
            if (Game.Player.WantedLevel > 0 || _playerManager.PlayerHasChanged())
            {
                _menuManager.UpdateStatus(0);
            }
        }

        catch (Exception ex)
        {
            _notifications.SideNotification($"Runtime Error - {ex}");
        }
    }

    private void OnScriptAborted(object sender, EventArgs e)
    {
        _rideHandler.DeleteEverything();

        // delete when done with coordinates
        _locationManager.RemoveAllBlips();
    }

    private void TestingCoords(string message, string start)
    {
        try
        {
            string[] lines = File.ReadAllLines("scripts/ulog.txt");

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(start))
                {
                    lines[i] = message;
                    break;
                }
            }

            File.WriteAllLines("scripts/ulog.txt", lines);
        }
        catch (Exception ex)
        {
            _notifications.SideNotification($"Error on saving: {ex}");
        }
    }

    private void Testing(Vector3 playerPos, Vehicle playerVehicle)
    {
        var X = playerPos.X;
        var Y = playerPos.Y;
        var T = playerPos.Z;
        var Z = World.GetGroundHeight(new Vector2(X, Y));
        var H = Game.Player.Character.Heading;

        _notifications.SubtitleNotification(
            $"X:{X:F2}, Y:{Y:F2}, Z:{T:F2}, GHZ:{Z:F2},   {H:F2}", 0, true);

        //if (playerVehicle != null) { _notifications.SubtitleNotification($"{playerVehicle.Speed}", 0, true); }



        if (Game.IsKeyPressed(Keys.G))
        {
            TestingCoords($"PED - new({X:F2}f, {Y:F2}f, {Z:F2}f, {H:F2}f),", "PED");

            //_locationManager.TestDistances();
        }

        if (Game.IsKeyPressed(Keys.H))
        {
            TestingCoords($"MARK - new({X:F2}f, {Y:F2}f, {Z:F2}f),", "MARK");
        }

        if (Game.IsKeyPressed(Keys.J))
        {
            _menuManager.UpdateStatus(1);
        }

        if (Game.IsKeyPressed(Keys.K))
        {
            _menuManager.UpdateStatus(0);
        }
    }

    public static void Main ()
    {

    }
}