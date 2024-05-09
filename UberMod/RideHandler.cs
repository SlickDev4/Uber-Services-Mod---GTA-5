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

public class RideHandler
{
    private PlayerManager _playerManager;
    private StatsManager _statsManager;
    private SettingsManager _settingsManager;
    private MenuManager _menuManager;
    private BlipManager _blipManager;
    private RouteManager _routeManager;
    private Customer _customer;
    private LocationManager _locationManager;
    private Notifications _notifications;

    private Blip? carBlip;

    private Vector3 customerPos;
    private float customerHeading;
    private Vector3 dropOffMarkerPos;
    private Vector3 pickUpMarkerPos;

    private bool isAtPickUpMarker;
    private bool isAtDropOffMarker;
    private bool canEnterVehicle;
    private bool canGiveCoordinates;
    private bool canLeaveVehicle;
    private bool canBrakeVehicle;
    private bool canCheckForCollisions;

    private bool enterCarNotification;
    private bool getBackInCarNotification;

    private bool carHasDamage;

    private bool dropOffTimer = true;
    private DateTime lastDropOffTime;

    private bool needPositionAssignment = true;

    private const float maxDistanceToMarker = 200.0f;
    private const float markerDistanceActivate = 3.0f;

    private bool spawnCustomer = false;

    public RideHandler(
        PlayerManager playerManager, 
        StatsManager statsManager,
        SettingsManager settingsManager,
        MenuManager menuManager, 
        Notifications notifications
        )
	{
        _playerManager = playerManager;
        _statsManager = statsManager;
        _settingsManager = settingsManager;
        _menuManager = menuManager;
        _notifications = notifications;

        _blipManager = new BlipManager();
        _routeManager = new RouteManager(_blipManager);
        _customer = new Customer(_blipManager, _playerManager, _statsManager, _settingsManager, _menuManager, _notifications);
        _locationManager = new LocationManager();
    }

    public void PlayAsUberDriver(Vector3 playerPos, Vehicle playerVehicle)
    {
        if (_customer.customer != null)
        {
            HandleDeadCustomer();
            SetupConditions(playerPos, playerVehicle);

            _customer.GetInPlayerVehicle(canEnterVehicle, playerVehicle);
            _customer.GiveDropOffDestination(canGiveCoordinates, pickUpMarkerPos, dropOffMarkerPos, playerVehicle);
            _customer.LeavePlayerVehicle(canLeaveVehicle, pickUpMarkerPos, dropOffMarkerPos);
            _customer.BrakeVehicle(canBrakeVehicle, playerVehicle);

            _routeManager.UpdateRoute(_customer.destinationBlip, _customer.customerBlip, carBlip);

            DrawMarkers(playerPos, playerVehicle);
            HandleCarBlip(playerVehicle);

            _customer.CheckForCollisions(playerVehicle, canCheckForCollisions);
        }

        AssignPositions(playerPos);

        if (spawnCustomer)
        {
            _customer.SpawnCustomer(customerPos, customerHeading);
        }

        ResetRide();
        ShowNotifications();
    }

    private void AssignPositions(Vector3 playerPos)
    {
        if (needPositionAssignment && _locationManager.calculationsReady)
        {
                (
                Vector3 customerPos,
                Vector3 pickUpPos,
                Vector3 dropOffPos,
                float customerHeading

            )? location = _locationManager.GetCustomerLocation();

            customerPos = location.Value.customerPos;
            customerHeading = location.Value.customerHeading;
            pickUpMarkerPos = location.Value.pickUpPos;
            dropOffMarkerPos = location.Value.dropOffPos;

            needPositionAssignment = false;
            spawnCustomer = true;
        }

        if (needPositionAssignment && !_locationManager.calculationsReady)
        {
            _locationManager.CalculatePositions(playerPos);
        }
    }

    private void SetupConditions(Vector3 playerPos, Vehicle playerVehicle)
    {
        float pickUpDistance = Vector3.Distance(playerPos, pickUpMarkerPos);
        float dropOffDistance = Vector3.Distance(playerPos, dropOffMarkerPos); 

        isAtPickUpMarker = pickUpDistance < markerDistanceActivate;
        isAtDropOffMarker = dropOffDistance < markerDistanceActivate;

        bool isInVehicle = Game.Player.Character.IsInVehicle() && playerVehicle != null && playerVehicle.Speed == 0f;
        bool vehicleIsCar = playerVehicle != null && playerVehicle.Model.IsCar;
        bool isInVehicleAtMarker = isInVehicle && isAtPickUpMarker;

        bool isInVehicleWithCustomer = Game.Player.Character.IsInVehicle() && _customer.currentVehicle == playerVehicle;
        bool isAtMarkerWithVehicle = isAtDropOffMarker && isInVehicle;
        bool customerIsInCar = _customer.hasEnteredCar && !_customer.wasDroppedOff;

        bool brakeAtPickUp = isAtPickUpMarker && playerVehicle != null && !_customer.hasEnteredCar && vehicleIsCar;
        bool brakeAtDropOff = isAtDropOffMarker && playerVehicle != null && _customer.currentVehicle == playerVehicle;

        canEnterVehicle = isInVehicleAtMarker && !_customer.enteringVehicle && vehicleIsCar && !carHasDamage;
        canGiveCoordinates = isInVehicleAtMarker && !_customer.hasEnteredCar;
        canLeaveVehicle = isInVehicleWithCustomer && isAtMarkerWithVehicle && customerIsInCar;

        canBrakeVehicle = brakeAtPickUp || brakeAtDropOff;

        canCheckForCollisions = customerIsInCar && isInVehicleWithCustomer;

        enterCarNotification = playerVehicle == null || !vehicleIsCar;
        getBackInCarNotification = 
            carBlip != null &&_customer.currentVehicle != null && 
            _customer.currentVehicle != playerVehicle && _customer.hasEnteredCar;
    }

    private void ShowNotifications()
    {
        _notifications.SubtitleNotification("Enter a car.", 0, enterCarNotification);
        _notifications.SubtitleNotification("Get back in the car", 0, getBackInCarNotification);
    }

    private void HandleCarBlip(Vehicle playerVehicle)
    {
        bool carBlipNeedsToBeCreated =
            carBlip == null &&
            _customer.currentVehicle != null &&
            _customer.currentVehicle != playerVehicle &&
            _customer.hasEnteredCar;

        bool carBlipNeedsToBeDeleted =
            carBlip != null &&
            _customer.currentVehicle != null &&
            _customer.currentVehicle == playerVehicle &&
            _customer.hasEnteredCar;

        if (carBlipNeedsToBeCreated)
        {
            carBlip = _blipManager.CreateBlip
                (
                _customer.currentVehicle.Position, 
                BlipSprite.PersonalVehicleCar, 
                BlipColor.Blue, 
                "Car", 
                false
                );
            return;
        }

        if (carBlipNeedsToBeDeleted)
        {
            _blipManager.DeleteBlip(ref carBlip);
            return;
        }
    }

    private void DrawMarkers(Vector3 playerPos, Vehicle playerVehicle)
    {
        // calculating distance from player to markers
        float distance = Vector3.Distance(playerPos, pickUpMarkerPos);
        float distance2 = Vector3.Distance(playerPos, dropOffMarkerPos);

        // conditions to draw the markers
        bool canDrawPickupMarker = distance < maxDistanceToMarker && !_customer.enteringVehicle;
        bool canDrawDropOffMarker = distance2 < maxDistanceToMarker && _customer.hasEnteredCar && !_customer.wasDroppedOff;

        // checking conditions for drawing the pickup marker
        if (canDrawPickupMarker)
        {
            // removing parked vehicles from the pickup marker if any
            RemoveVehiclesFromMarker(pickUpMarkerPos, playerVehicle);

            // marker components
            var markerType = MarkerType.VerticalCylinder;
            var markerPos = new Vector3(pickUpMarkerPos.X, pickUpMarkerPos.Y, pickUpMarkerPos.Z);
            var markerDirection = Vector3.Zero;
            var markerRotation = Vector3.Zero;
            var markerScale = new Vector3(1f, 1f, 1f);
            var markerColor = Color.Yellow;

            // drawing the marker
            World.DrawMarker(markerType, markerPos, markerDirection, markerRotation, markerScale, markerColor);
        }

        // checking conditions for drawing the drop off marker
        if (canDrawDropOffMarker)
        {
            // removing parked vehicles from the drop off marker if any
            RemoveVehiclesFromMarker(dropOffMarkerPos, playerVehicle);

            var markerType = MarkerType.VerticalCylinder;
            var markerPos = new Vector3(dropOffMarkerPos.X, dropOffMarkerPos.Y, dropOffMarkerPos.Z);
            var markerDirection = Vector3.Zero;
            var markerRotation = Vector3.Zero;
            var markerScale = new Vector3(1f, 1f, 1f);
            var markerColor = Color.Yellow;

            // drawing the marker
            World.DrawMarker(markerType, markerPos, markerDirection, markerRotation, markerScale, markerColor);
        }
    }

    private void RemoveVehiclesFromMarker(Vector3 markerPos, Vehicle playerVehicle)
    {
        Vehicle[] nearbyVehicles = World.GetNearbyVehicles(markerPos, 3f);

        foreach (Vehicle vehicle in nearbyVehicles)
        {
            if (vehicle.Position.DistanceTo(markerPos) <= 3f)
            {
                bool notPlayerVehicle = vehicle != playerVehicle;
                bool vehicleIsNotMoving = vehicle.Speed == 0f;
                bool vehicleIsEmpty = vehicle.Occupants.Count() == 0;
                
                bool vehicleCanBeDeleted = notPlayerVehicle && vehicleIsNotMoving && vehicleIsEmpty;

                if (vehicleCanBeDeleted)
                {
                    vehicle.Delete();
                }
            }
        }
    }

    private void HandleDeadCustomer()
    {
        if (!_customer.customer.IsAlive)
        {
            DeleteEverything();
        }
    }

    private void DeleteAllBlips()
    {
        _blipManager.DeleteBlip(ref _customer.customerBlip);
        _blipManager.DeleteBlip(ref _customer.destinationBlip);
        _blipManager.DeleteBlip(ref carBlip);
    }

    private void ResetRide()
    {
        if (_customer.wasDroppedOff)
        {
            if (dropOffTimer)
            {
                lastDropOffTime = DateTime.UtcNow;
            }

            dropOffTimer = false;

            TimeSpan elapsed = DateTime.UtcNow - lastDropOffTime;

            if (elapsed.TotalSeconds <= 3)
            {
                _playerManager.DisableVehicleControls();
            }

            if (elapsed.TotalSeconds >= 5)
            {
                ResetVariables();
            }
        }
    }

    private void ResetVariables()
    {
        _customer.currentVehicle = null;

        _customer.enteringVehicle = false;
        _customer.hasEnteredCar = false;
        _customer.wasDroppedOff = false;

        dropOffTimer = true;
        needPositionAssignment = true;
        spawnCustomer = false;

        lastDropOffTime = DateTime.MinValue;

        _routeManager.ResetRouteVariables();
        _locationManager.ResetCalculationVariables();
    }

    public void DeleteEverything()
    {
        DeleteAllBlips();
        _customer.DeleteCustomer();
        _customer.DeletePreviousCustomer();
        ResetVariables();
    }
}
