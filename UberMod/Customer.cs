
using GTA;
using GTA.Math;
using GTA.Native;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;


public class Customer
{
    private BlipManager _blipManager;
    private PlayerManager _playerManager;
    private StatsManager _statsManager;
    private SettingsManager _settingsManager;
    private MenuManager _menuManager;
    private Notifications _notifications;
    
    private float distanceForPayment;

    public Ped? customer;
    private Ped? droppedOffCustomer;

    public Blip? customerBlip;
    public Blip? destinationBlip;

    public Vehicle? currentVehicle;

    public bool enteringVehicle = false;
    public bool hasEnteredCar = false;
    public bool wasDroppedOff = false;

    private int smallCollisions = 0;
    private int mediumCollisions = 0;
    private int bigCollisions = 0;

    private bool isCollisionCooldownActive = false;
    private float timeSinceLastCollision = 0f;

    private static readonly Random random = new Random();

    public Customer(
        BlipManager blipManager, 
        PlayerManager playerManager, 
        StatsManager statsManager, 
        SettingsManager settingsManager,
        MenuManager menuManager,
        Notifications notifications
        )
	{
        _blipManager = blipManager;
        _playerManager = playerManager;
        _statsManager = statsManager;
        _settingsManager = settingsManager;
        _menuManager = menuManager;
        _notifications = notifications;
	}

    public void SpawnCustomer(Vector3 customerPos, float customerHeading)
    {
        if (customer == null && !wasDroppedOff)
        {
            Ped[] pedestrians = World.GetAllPeds();

            if (pedestrians == null || pedestrians.Count() == 0) { return; }

            int index = random.Next(0, pedestrians.Count());
            int usePhone = random.Next(0, 2);
            Ped pedestrian = pedestrians[index];
            bool pedInvalid = pedestrian == null || pedestrian == Game.Player.Character || !pedestrian.IsHuman;

            if (pedInvalid) { return; }

            customer = World.CreatePed(pedestrian.Model.Hash, customerPos, customerHeading);
            customer.Heading = customerHeading;

            //Log($"Customer - {customer == null}"); // here is the problem

            if (usePhone == 1) { customer.Task.UseMobilePhone(); }

            customer.CanBeDraggedOutOfVehicle = false;

            customerBlip = customer.AddBlip();
            customerBlip.Sprite = BlipSprite.Friend;
            customerBlip.Color = BlipColor.Blue;
            customerBlip.Name = "Customer";
            customerBlip.ShowRoute = true;

            Function.Call(Hash.TASK_SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, customer, true);
            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, customer, 0, 0);
        }
    }

    public void GetInPlayerVehicle(bool canEnterVehicle, Vehicle playerVehicle)
    {
        if (canEnterVehicle)
        {
            var customerTask = Hash.TASK_ENTER_VEHICLE;
            var customerHandle = customer.Handle;
            var vehicleHandle = playerVehicle.Handle;
            var timeToWarp = 10000; // milliseconds
            var vehicleSeat = (int)VehicleSeat.Passenger;

            Game.Player.Character.CanBeDraggedOutOfVehicle = false;

            // assigning task to enter the vehicle
            Function.Call(customerTask, customerHandle, vehicleHandle, timeToWarp, vehicleSeat, 1f, 1, 0);
            enteringVehicle = true;
        }
    }

    public void GiveDropOffDestination(bool canGiveCoordinates, Vector3 pickUpPos, Vector3 dropOffPos, Vehicle playerVehicle)
    {
        if (canGiveCoordinates)
        {
            foreach (Ped occupant in playerVehicle.Occupants)
            {
                if (occupant == customer)
                {
                    _blipManager.DeleteBlip(ref customerBlip);
                    DeletePreviousCustomer();

                    // delete after testing - delete pickUpPos from params too!
                    var distance = World.CalculateTravelDistance(pickUpPos, dropOffPos) / 1609.34f;
                    var payment = (int)Math.Round(distance * _settingsManager.PayPerMileSetting);
                    _notifications.SideNotification($"Distance: {distance:F2} miles\nPayment: {payment} $");

                    destinationBlip = _blipManager.CreateBlip
                        (
                        dropOffPos,
                        BlipSprite.Standard,
                        BlipColor.Yellow,
                        "Destination",
                        true
                        );

                    currentVehicle = playerVehicle;
                    hasEnteredCar = true;

                    Game.Player.Character.CanBeDraggedOutOfVehicle = true;
                    break;
                }
            }
        }
    }

    public void LeavePlayerVehicle(bool canLeaveVehicle, Vector3 pickUpPos, Vector3 dropOffPos)
    {
        if (canLeaveVehicle)
        {
            float averageRating = CalculateAverageRating();
            int payment = CalculatePayment(pickUpPos, dropOffPos);
            int tip = CalculateTip(averageRating, payment);
            
            PayRide(payment + tip);

            customer?.Task.LeaveVehicle();
            _blipManager.DeleteBlip(ref destinationBlip);

            _statsManager.UpdateStats(payment, averageRating);
            _statsManager.SaveStatsToFile();
            _menuManager.UpdateStatsMenu();

            currentVehicle = null;
            wasDroppedOff = true;

            TransferCustomer();
            DeleteCustomer();

            _notifications.SideNotification($"Rating: {averageRating:F1} / 5.0\nPayment: {payment} $\nTip: {tip} $");
        }
    }

    public void CheckForCollisions(Vehicle playerVehicle, bool canCheckForCollisions)
    {
        if (canCheckForCollisions && !isCollisionCooldownActive)
        {
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

        RunCollisionsTimer();
    }

    private void RunCollisionsTimer()
    {
        if (isCollisionCooldownActive)
        {
            timeSinceLastCollision += Game.LastFrameTime;
            if (timeSinceLastCollision >= 3f)
            {
                isCollisionCooldownActive = false;
                timeSinceLastCollision = 0f;
            }
        }
    }

    private void ResetCollisionsCooldown()
    {
        isCollisionCooldownActive = true;
        timeSinceLastCollision = 0f;
    }

    private float CalculateAverageRating()
    {
        float averageRating = 5f;

        averageRating -= smallCollisions * 0.1f;
        averageRating -= mediumCollisions * 0.2f;
        averageRating -= bigCollisions * 0.5f;

        if (averageRating < 1) { averageRating = 1; }

        smallCollisions = 0;
        mediumCollisions = 0;
        bigCollisions = 0;

        var averageRatingSetting = _menuManager.GetAverateRatingSetting();

        if (averageRatingSetting == "Enabled")
        {
            return averageRating;
        }

        return _statsManager.AverageRating;
    }

    private int CalculatePayment(Vector3 pickUpPos, Vector3 dropOffPos)
    {
        int payment;
        float paymentDistance;

        // test for protecting vs 62 miles distance
        float travelDistance = World.CalculateTravelDistance(pickUpPos, dropOffPos) / 1609.34f;
        float getDistance = World.GetDistance(pickUpPos, dropOffPos) / 1609.34f;

        if (travelDistance - getDistance <= 5f)
        {
            paymentDistance = travelDistance;
            _notifications.SideNotification("Paying with TRAVEL Distance");
        }
        else
        {
            paymentDistance = getDistance;
            _notifications.SideNotification("Paying with GET Distance");
        }

        payment = (int)Math.Round(paymentDistance * _settingsManager.PayPerMileSetting);

        return payment;
    }

    private int CalculateTip(float averageRating, int payment)
    {
        float tip = 0f;
        int result;

        if (averageRating >= 4.5f)
        {
            tip = 0.4f * payment;
        }

        if (averageRating >= 3.5f && averageRating < 4.5f)
        {
            tip = 0.3f * payment;
        }

        if (averageRating >= 2.5f && averageRating < 3.5f)
        {
            tip = 0.2f * payment;
        }

        if (averageRating >= 1.5f && averageRating < 2.5f)
        {
            tip = 0.1f * payment;
        }

        if (averageRating < 1.5f)
        {
            tip = 0;
        }

        result = (int)Math.Round(tip);

        return result;
    }

    private void PayRide(int amount)
    {
        _playerManager.IncreaseMoney(amount);
    }

    public void BrakeVehicle(bool canBrakeVehicle, Vehicle playerVehicle)
    {
        if (canBrakeVehicle)
        {
            if (playerVehicle.Speed >= 25f)
            {
                playerVehicle.Speed -= 2f;
            }

            else if (playerVehicle.Speed >= 1f && playerVehicle.Speed < 25f)
            {
                playerVehicle.Speed -= 1f;
            }

            else
            {
                playerVehicle.Speed = 0f;
                _playerManager.DisableVehicleControls();
            }
        }
    }

    private void TransferCustomer()
    {
        if (droppedOffCustomer == null)
        {
            droppedOffCustomer = customer;
            droppedOffCustomer?.Task.WanderAround();
            customer = null;
        }
    }

    public void DeleteCustomer()
    {
        customer?.Delete();
        customer = null;
    }

    public void DeletePreviousCustomer()
    {
        droppedOffCustomer?.Delete();
        droppedOffCustomer = null;
    }
}
