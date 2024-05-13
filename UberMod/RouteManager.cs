using GTA;

public class RouteManager
{
    private BlipManager _blipManager;

    private bool routeTimer = true;
    private DateTime lastResetRouteTime;
    private const float RouteUpdateIntervalSeconds = 1.0f;

    public RouteManager(BlipManager blipManager)
	{
        // This class is handling the route updates - there was an issue with disappearing routes
        // so we are updating them every 1 seconds - you can see a little blinking on the map due to that

        _blipManager = blipManager;
	}

    public void UpdateRoute(Blip? destination, Blip? customer, Blip? car)
    {
        // This method is updating the route every 1 seconds

        // Checking if all the blips are null
        bool allBlipsAreNull = destination == null && customer == null && car == null;

        // Checking which route needs an update
        bool updateDestination = destination != null && car == null && customer == null;
        bool updateCustomer = destination == null && car == null && customer != null;
        bool updateCar = destination != null && car != null && customer == null;

        // If the blips are null we return as we don't need an update now
        if (allBlipsAreNull) { return; }

        // This is the update timer
        if (routeTimer)
        {
            lastResetRouteTime = DateTime.UtcNow;
            routeTimer = false;
        }

        TimeSpan elapsed = DateTime.UtcNow - lastResetRouteTime;

        // If 1 seconds has passed, we update the needed route
        if (elapsed.TotalSeconds >= RouteUpdateIntervalSeconds)
        {
            if (updateDestination)
            {
                _blipManager.UpdateBlip(destination);
            }
            else if (updateCustomer)
            {
                _blipManager.UpdateBlip(customer);
            }
            else if (updateCar)
            {
                _blipManager.UpdateBlip(car);
            }
            
            // When the update has been performed, we reset the timer
            ResetRouteVariables();
        }
    }

    public void ResetRouteVariables()
    {
        // This method is used to reset the route timer

        routeTimer = true;
        lastResetRouteTime = DateTime.MinValue;
    }
}
