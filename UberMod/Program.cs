using GTA;
using GTA.Math;

public class UberMod : Script
{
    private PlayerManager _playerManager;
    private MenuManager _menuManager;
    private Notifications _notifications;
    private BlipManager _blipManager;
    private LocationManager _locationManager;
    private ContactManager _contactManager;
    private RouteManager _routeManager;
    private Customer _customer;
    private RideHandler _rideHandler;

    public UberMod()
    {
        // This is the main program class, which is loading every other class
        // Handling the OnTick and OnScriptAborted methods

        try
        {
            // Loading all classes here

            _playerManager = new PlayerManager();
            _menuManager = new MenuManager(); 
            _notifications = new Notifications();
            _blipManager = new BlipManager();

            _locationManager = new LocationManager(_menuManager);
            _contactManager = new ContactManager(_menuManager);
            _routeManager = new RouteManager(_blipManager);
            
            _customer = new Customer(
                _blipManager, 
                _playerManager, 
                _menuManager, 
                _notifications
                );

            _rideHandler = new RideHandler(
                _playerManager, 
                _menuManager, 
                _notifications,
                _blipManager,
                _routeManager,
                _locationManager,
                _customer
                );

            // Adding the Uber contact in the phone
            _contactManager.AddContact();

            // Subscribing to the OnTick and OnScriptAborted methods
            Tick += OnTick;
            Aborted += OnScriptAborted;

            // Showing a notification that the mod has loaded successfully
            GTA.UI.Notification.Show($"Uber Mod v1.0 by kukata0412 has loaded successfully.");
        }
        catch (Exception ex) 
        {
            GTA.UI.Notification.Show($"Error on loading - {ex}");
        }
        
    }

    private void OnTick(object sender, EventArgs e)
    {
        // TODO:

        // - color some texts
        // - format numbers with , for thousands

        // MONITOR:

        // LATER
        // - check how to get a license if providing the code to the mod

        // NOTES
        // - could not fix menu freeze <= 1 second on first open
        // - could not fix disappearing route - workaround with update every second which causes it to blink on the map
        // - when turning off the average rating, it will give you your current average rating every ride, which will impact
        //      your tips, you can always change the current average rating in the .ini file,
        //      it has to be 1-5 (eg. 1.5, 2.5, 3.7) or the script won't work properly
        // - sometimes distance calculation bugs and it says that the distance is 60 miles and wants to give a 3k payment
        //      it happens rarely and in this occasions, the distance is calculated with another method, which will 
        //      pay 30-60 $ less but it's better than getting 3k for 2 miles..

        try
        {
            // Updating the phone
            _contactManager.UpdateIFruit();

            // Processing the menus
            _menuManager.ProcessMenus();

            // Checking for changes in the settings and saving to file if needed.
            _menuManager.SettingsHaveChanged();

            // Getting player status/position and current vehicle
            string status = _menuManager._statusOptions[_menuManager._statusItem.Index];
            Vector3 playerPos = Game.Player.Character.Position;
            Vehicle playerVehicle = Game.Player.Character.CurrentVehicle;


            // Handle Logic when Player is available
            if (status == "Available")
            {
                _rideHandler.PlayAsUberDriver(playerPos, playerVehicle);
            }

            // Handle Logic when Player is not playing the mod
            if (status == "Unavailable")
            {
                _rideHandler.DeleteEverything();
            }

            // If the Player gets a wanted level or changes the player, the status is set to unavailable
            if (Game.Player.WantedLevel > 0 || _playerManager.PlayerHasChanged())
            {
                _menuManager._statusItem.Index = 0;
            }
        }

        catch (Exception ex)
        {
            _notifications.SideNotification($"Runtime Error - {ex}");
        }
    }

    private void OnScriptAborted(object sender, EventArgs e)
    {
        // This method is executed when the script is restarted

        _rideHandler.DeleteEverything();
    }

    public static void Main ()
    {
        // This method is needed by the script to run even though it's empty
    }
}