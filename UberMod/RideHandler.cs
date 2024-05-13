using GTA;
using GTA.Math;

public class RideHandler
{
    private PlayerManager _playerManager;
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

    private bool dropOffTimer = true;
    private DateTime lastDropOffTime;

    private bool needPositionAssignment = true;

    private const float maxDistanceToMarker = 200.0f;
    private const float markerDistanceActivate = 3.0f;

    private bool spawnCustomer = false;

    private int smallCollisions = 0;
    private int mediumCollisions = 0;
    private int bigCollisions = 0;

    private bool isCollisionCooldownActive = false;
    private float timeSinceLastCollision = 0f;

    public RideHandler(
        PlayerManager playerManager,
        MenuManager menuManager, 
        Notifications notifications,
        BlipManager blipManager,
        RouteManager routeManager,
        LocationManager locationManager,
        Customer customer
        )
	{
        // This is the class that handles the ride in general

        _playerManager = playerManager;
        _menuManager = menuManager;
        _notifications = notifications;
        _blipManager = blipManager;
        _routeManager = routeManager;
        _locationManager = locationManager;
        _customer = customer;
    }

    public void PlayAsUberDriver(Vector3 playerPos, Vehicle playerVehicle)
    {
        // This is the main method called in the OnTick in Program.cs when the player status is available

        // These methods are called if the customer is spawned
        if (_customer.customer != null)
        {
            HandleDeadCustomer();
            SetupConditions(playerPos, playerVehicle);

            _customer.GetInPlayerVehicle(canEnterVehicle, playerVehicle);
            _customer.GiveDropOffDestination(canGiveCoordinates, dropOffMarkerPos, playerVehicle);
            _customer.LeavePlayerVehicle(canLeaveVehicle, pickUpMarkerPos, dropOffMarkerPos, smallCollisions, mediumCollisions, bigCollisions);
            _customer.BrakeVehicle(canBrakeVehicle, playerVehicle);

            _routeManager.UpdateRoute(_customer.destinationBlip, _customer.customerBlip, carBlip);

            DrawMarkers(playerPos, playerVehicle);
            HandleCarBlip(playerVehicle);

            CheckForCollisions(playerVehicle);
        }

        // These methods are called all the time - they have variable protection so they are not actually called all the time
        // The script just checks the statements every frame

        AssignPositions(playerPos);
        _customer.SpawnCustomer(customerPos, customerHeading, spawnCustomer);
        ResetRide();
        ShowNotifications();
    }

    private void AssignPositions(Vector3 playerPos)
    {
        // This method is used to calculate and assign positions when we need it

        // Here we are checking if we need position assignment and if the calculations are done
        if (needPositionAssignment && _locationManager.calculationsReady)
        {
                (
                Vector3 customerPos,
                Vector3 pickUpPos,
                Vector3 dropOffPos,
                float customerHeading

            )? location = _locationManager.GetCustomerLocation();

            // Assigning the data to the variables if everything is ok
            customerPos = location.Value.customerPos;
            customerHeading = location.Value.customerHeading;
            pickUpMarkerPos = location.Value.pickUpPos;
            dropOffMarkerPos = location.Value.dropOffPos;

            // Indicating the script that we assigned the positions and we need to spawn the customer
            needPositionAssignment = false;
            spawnCustomer = true;
        }

        // Checking if we need position assignments but the calculations are not ready
        // Then we call the method that performs the calculations from LocationManager.cs
        if (needPositionAssignment && !_locationManager.calculationsReady)
        {
            _locationManager.CalculatePositions(playerPos);
        }
    }

    private void SetupConditions(Vector3 playerPos, Vehicle playerVehicle)
    {
        // This method is taking care of all the variables that our script checks

        // Here we are constantly calculating the distance between the player and the pickup/drop off markers
        float pickUpDistance = Vector3.Distance(playerPos, pickUpMarkerPos);
        float dropOffDistance = Vector3.Distance(playerPos, dropOffMarkerPos); 

        // Here we are checkinf if the player is at the markers position
        isAtPickUpMarker = pickUpDistance < markerDistanceActivate;
        isAtDropOffMarker = dropOffDistance < markerDistanceActivate;

        // Here we are checking if the player is in a vehicle and is not moving
        bool isInVehicle = Game.Player.Character.IsInVehicle() && playerVehicle != null && playerVehicle.Speed == 0f;
        // Here we are checking if the player is in a car specifically
        bool vehicleIsCar = playerVehicle != null && playerVehicle.Model.IsCar;
        // Here we are checking if the player is in a vehicle and at the pickup marker
        bool isInVehicleAtMarker = isInVehicle && isAtPickUpMarker;

        // Here we are checking if the player is in the same car as the customer
        bool isInVehicleWithCustomer = Game.Player.Character.IsInVehicle() && _customer.currentVehicle == playerVehicle;
        // Here we are checking if the player is in a vehicle at the drop off marker
        bool isAtMarkerWithVehicle = isAtDropOffMarker && isInVehicle;
        // Here we are checking if the customer is in the car
        bool customerIsInCar = _customer.hasEnteredCar && !_customer.wasDroppedOff;

        // Here we are checking if we can apply the brakes on the vehicle at the pickup/drop off locations
        bool brakeAtPickUp = isAtPickUpMarker && playerVehicle != null && !_customer.hasEnteredCar && vehicleIsCar;
        bool brakeAtDropOff = isAtDropOffMarker && playerVehicle != null && _customer.currentVehicle == playerVehicle;

        // Here we are checking if the customer can enter the vehicle, can give destination point and if they can leave the vehicle
        canEnterVehicle = isInVehicleAtMarker && !_customer.enteringVehicle && vehicleIsCar;
        canGiveCoordinates = isInVehicleAtMarker && !_customer.hasEnteredCar;
        canLeaveVehicle = isInVehicleWithCustomer && isAtMarkerWithVehicle && customerIsInCar;

        // Checking if the vehicle can be actually braked
        canBrakeVehicle = brakeAtPickUp || brakeAtDropOff;

        // Determining if we can check for collisions - only when the customer is in the same car as the player
        canCheckForCollisions = customerIsInCar && isInVehicleWithCustomer;

        // Here we are checking when to display the subtitle notifications
        enterCarNotification = playerVehicle == null || !vehicleIsCar;
        getBackInCarNotification = 
            carBlip != null &&_customer.currentVehicle != null && 
            _customer.currentVehicle != playerVehicle && _customer.hasEnteredCar;
    }

    private void ShowNotifications()
    {
        // This method is showing 2 notifications
        // - when the player should get in a car before the customer has entered it
        // - when the player should get back in the car, which the customer has already entered

        _notifications.SubtitleNotification("Enter a car.", 0, enterCarNotification);
        _notifications.SubtitleNotification("Get back in the car", 0, getBackInCarNotification);
    }

    private void HandleCarBlip(Vehicle playerVehicle)
    {
        // This method is handling when the car blip should be drawn

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

        // If the customer is in the car and the player is not, the blip should be created
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

        // If the player is in the car with the customer, the blip should be deleted
        if (carBlipNeedsToBeDeleted)
        {
            _blipManager.DeleteBlip(ref carBlip);
            return;
        }
    }

    private void DrawMarkers(Vector3 playerPos, Vehicle playerVehicle)
    {
        // This method is handling the drawing of the markers

        // Calculating distance from player to markers
        float distance = Vector3.Distance(playerPos, pickUpMarkerPos);
        float distance2 = Vector3.Distance(playerPos, dropOffMarkerPos);

        // Conditions to draw the markers
        bool canDrawPickupMarker = distance < maxDistanceToMarker && !_customer.enteringVehicle;
        bool canDrawDropOffMarker = distance2 < maxDistanceToMarker && _customer.hasEnteredCar && !_customer.wasDroppedOff;

        // Checking conditions for drawing the pickup marker
        if (canDrawPickupMarker)
        {
            // Removing parked vehicles from the pickup marker if any
            RemoveVehiclesFromMarker(pickUpMarkerPos, playerVehicle);

            // Marker components
            var markerType = MarkerType.VerticalCylinder;
            var markerPos = new Vector3(pickUpMarkerPos.X, pickUpMarkerPos.Y, pickUpMarkerPos.Z);
            var markerDirection = Vector3.Zero;
            var markerRotation = Vector3.Zero;
            var markerScale = new Vector3(1f, 1f, 1f);
            var markerColor = Color.Yellow;

            // Drawing the marker
            World.DrawMarker(markerType, markerPos, markerDirection, markerRotation, markerScale, markerColor);
        }

        // Checking conditions for drawing the drop off marker
        if (canDrawDropOffMarker)
        {
            // Removing parked vehicles from the drop off marker if any
            RemoveVehiclesFromMarker(dropOffMarkerPos, playerVehicle);

            var markerType = MarkerType.VerticalCylinder;
            var markerPos = new Vector3(dropOffMarkerPos.X, dropOffMarkerPos.Y, dropOffMarkerPos.Z);
            var markerDirection = Vector3.Zero;
            var markerRotation = Vector3.Zero;
            var markerScale = new Vector3(1f, 1f, 1f);
            var markerColor = Color.Yellow;

            // Drawing the marker
            World.DrawMarker(markerType, markerPos, markerDirection, markerRotation, markerScale, markerColor);
        }
    }

    private void RemoveVehiclesFromMarker(Vector3 markerPos, Vehicle playerVehicle)
    {
        // This method is removing parked vehicle that are on the marker

        Vehicle[] nearbyVehicles = World.GetNearbyVehicles(markerPos, 3f);

        // Checking nearby vehicle based on the marker position
        foreach (Vehicle vehicle in nearbyVehicles)
        {
            if (vehicle.Position.DistanceTo(markerPos) <= 3f)
            {
                // Checking if the vehicle is empty/not moving and not the player vehicle
                bool notPlayerVehicle = vehicle != playerVehicle;
                bool vehicleIsNotMoving = vehicle.Speed == 0f;
                bool vehicleIsEmpty = vehicle.Occupants.Count() == 0;
                
                bool vehicleCanBeDeleted = notPlayerVehicle && vehicleIsNotMoving && vehicleIsEmpty;

                // Deleting the vehicle if the conditions are met
                if (vehicleCanBeDeleted)
                {
                    vehicle.Delete();
                }
            }
        }
    }

    private void CheckForCollisions(Vehicle playerVehicle)
    {
        // This method is checking for collisions while the customer is in the same car as the player

        // If the collision cooldown has expired and the customer is in the same car as the player, we check for collisions
        if (canCheckForCollisions && !isCollisionCooldownActive)
        {
            // Setting up collision conditions based on the vehicle speed
            bool smallCollisionsCondition = playerVehicle.Speed >= 0.5f && playerVehicle.Speed <= 15f && playerVehicle.HasCollided;
            bool mediumCollisionsCondition = playerVehicle.Speed > 15f && playerVehicle.Speed <= 30f && playerVehicle.HasCollided;
            bool bigCollisionsCondition = playerVehicle.Speed > 30f && playerVehicle.HasCollided;

            if (smallCollisionsCondition)
            {
                smallCollisions++;
                ResetCollisionsCooldown();
                _notifications.CrashNotifications("small");
            }

            if (mediumCollisionsCondition)
            {
                mediumCollisions++;
                ResetCollisionsCooldown();
                _notifications.CrashNotifications("medium");
            }

            if (bigCollisionsCondition)
            {
                bigCollisions++;
                ResetCollisionsCooldown();
                _notifications.CrashNotifications("big");
            }
        }

        // Running the collisions timer so that we can have 1 collision every 3 seconds
        RunCollisionsTimer();
    }

    private void RunCollisionsTimer()
    {
        // This method is handling the collisions cooldown - we can have 1 collision every 3 seconds

        // Checking if the cooldown is active
        if (isCollisionCooldownActive)
        {
            // Getting the current time of the frame
            timeSinceLastCollision += Game.LastFrameTime;

            // If the time since last collision is more than 3 seconds, we reset the cooldown
            if (timeSinceLastCollision >= 3f)
            {
                isCollisionCooldownActive = false;
                timeSinceLastCollision = 0f;
            }
        }
    }

    private void ResetCollisionsCooldown()
    {
        // This method is resetting the collision cooldown

        isCollisionCooldownActive = true;
        timeSinceLastCollision = 0f;
    }

    private void HandleDeadCustomer()
    {
        // This method is handling a dead customer scenario - basically deleting everything and providing a new customer

        if (!_customer.customer.IsAlive)
        {
            DeleteEverything();
        }
    }

    private void DeleteAllBlips()
    {
        // This method is deleting all blips - used for DeleteEverything method

        _blipManager.DeleteBlip(ref _customer.customerBlip);
        _blipManager.DeleteBlip(ref _customer.destinationBlip);
        _blipManager.DeleteBlip(ref carBlip);
    }

    private void ResetRide()
    {
        // This method is resetting the ride variables when the customer is dropped off

        // Checking if the customer was dropped off
        if (_customer.wasDroppedOff)
        {
            // Setting a timer so the new customer is not immediatelly provided
            if (dropOffTimer)
            {
                lastDropOffTime = DateTime.UtcNow;
            }

            dropOffTimer = false;

            TimeSpan elapsed = DateTime.UtcNow - lastDropOffTime;

            // If the timer reaches 3 seconds, the player controls are enabled
            // Otherwise, they are disabled under 3 seconds so the customer can freely get off the vehicle
            if (elapsed.TotalSeconds <= 3)
            {
                _playerManager.DisableVehicleControls();
            }

            // After 5 seconds have passed, the variables are reset and the new customer is provided
            if (elapsed.TotalSeconds >= 5)
            {
                ResetVariables();
            }
        }
    }

    private void ResetVariables()
    {
        // This method is resetting all the variables so a new ride can be initiated

        _customer.currentVehicle = null;

        _customer.enteringVehicle = false;
        _customer.hasEnteredCar = false;
        _customer.wasDroppedOff = false;

        dropOffTimer = true;
        needPositionAssignment = true;
        spawnCustomer = false;

        smallCollisions = 0;
        mediumCollisions = 0;
        bigCollisions = 0;

        lastDropOffTime = DateTime.MinValue;

        _routeManager.ResetRouteVariables();
        _locationManager.ResetCalculationVariables();
    }

    public void DeleteEverything()
    {
        // This method is deleting and resetting everything

        DeleteAllBlips();
        _customer.DeleteCustomer();
        _customer.DeletePreviousCustomer();
        ResetVariables();
    }
}
