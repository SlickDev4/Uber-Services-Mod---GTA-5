using GTA;


public class RouteManager
{
    private BlipManager _blipManager;

    private bool routeTimer = true;
    private DateTime lastResetRouteTime;
    private const float RouteUpdateIntervalSeconds = 1.0f;

    public RouteManager(BlipManager blipManager)
	{
        _blipManager = blipManager;
	}

    public void UpdateRoute(Blip? destination, Blip? customer, Blip? car)
    {
        bool allBlipsAreNull = destination == null && customer == null && car == null;

        bool updateDestination = destination != null && car == null && customer == null;
        bool updateCustomer = destination == null && car == null && customer != null;
        bool updateCar = destination != null && car != null && customer == null;

        if (allBlipsAreNull) { return; }

        if (routeTimer)
        {
            lastResetRouteTime = DateTime.UtcNow;
            routeTimer = false;
        }

        TimeSpan elapsed = DateTime.UtcNow - lastResetRouteTime;

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
            
            ResetRouteVariables();
        }
    }

    public void ResetRouteVariables()
    {
        routeTimer = true;
        lastResetRouteTime = DateTime.MinValue;
    }
}
