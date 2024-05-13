using GTA;
using GTA.Math;
using GTA.Native;


public class Customer
{
    private BlipManager _blipManager;
    private PlayerManager _playerManager;
    private MenuManager _menuManager;
    private Notifications _notifications;

    public Ped? customer;
    private Ped? droppedOffCustomer;

    public Blip? customerBlip;
    public Blip? destinationBlip;

    public Vehicle? currentVehicle;

    public bool enteringVehicle = false;
    public bool hasEnteredCar = false;
    public bool wasDroppedOff = false;

    private static readonly Random random = new Random();

    public Customer(
        BlipManager blipManager, 
        PlayerManager playerManager,
        MenuManager menuManager,
        Notifications notifications
        )
	{
        // This class is managing the customer actions

        _blipManager = blipManager;
        _playerManager = playerManager;
        _menuManager = menuManager;
        _notifications = notifications;
	}

    public void SpawnCustomer(Vector3 customerPos, float customerHeading, bool spawnCustomer)
    {
        // This method is spawning the customer if they are not already spawned

        if (customer == null && !wasDroppedOff && spawnCustomer)
        {
            // Getting all pedestrians in the world
            Ped[] pedestrians = World.GetAllPeds();

            // Checking if the pedestrians list is not empty - return if it is
            if (pedestrians == null || pedestrians.Count() == 0) { return; }

            // Here we take a random index to get a pedestrian from the list and to determine if they will be talking on the phone
            int index = random.Next(0, pedestrians.Count());
            int usePhone = random.Next(0, 2);
            Ped pedestrian = pedestrians[index];

            // Checking if the pedestrian is assigned and if it's not the current player character as well as if they are human
            bool pedInvalid = pedestrian == null || pedestrian == Game.Player.Character || !pedestrian.IsHuman;
            if (pedInvalid) { return; }

            // Assigning the pedestrian to a customer variable that we will use
            customer = World.CreatePed(pedestrian.Model.Hash, customerPos, customerHeading);
            customer.Heading = customerHeading;

            // Assigning the talk on the phone task
            if (usePhone == 1) { customer.Task.UseMobilePhone(); }

            // Making it impossible to drag out the pedestrian from the car as the player might do that if they are trying
            // to enter the car from the passenger side
            customer.CanBeDraggedOutOfVehicle = false;

            // Adding a blip to the customer so they are visible on the map
            customerBlip = customer.AddBlip();
            customerBlip.Sprite = BlipSprite.Friend;
            customerBlip.Color = BlipColor.Blue;
            customerBlip.Name = "Customer";
            customerBlip.ShowRoute = true;

            // Removing the customer's flee and scare components because if they get scared, they will start running and not be 
            // able to get in the car
            Function.Call(Hash.TASK_SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, customer, true);
            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, customer, 0, 0);
        }
    }

    public void GetInPlayerVehicle(bool canEnterVehicle, Vehicle playerVehicle)
    {
        // This method is for the customer to get in the car

        // Checking if the player is on the marked with a valid vehicle to pick up the customer
        // - this is checked in SetupConditions() in RideHandler.cs
        if (canEnterVehicle)
        {
            // Assigning the task to enter the vehicle to the customer
            // If they are unable to find a path to enter the vehicle, they will teleport in it after 15 seconds
            var customerTask = Hash.TASK_ENTER_VEHICLE;
            var customerHandle = customer.Handle;
            var vehicleHandle = playerVehicle.Handle;
            var timeToWarp = 15000; // milliseconds = 15 seconds
            var vehicleSeat = (int)VehicleSeat.Passenger;

            // Making it impossible for the player to get dragged out of the vehicle because if the customer does not
            // have a path to the passenger seat but has a path to the driver's seat, they will drag out the player
            Game.Player.Character.CanBeDraggedOutOfVehicle = false;

            // Setting the customer to be invincible while entering the car because some random events might kill them
            // and this will crash the code
            customer.IsInvincible = true;

            // Actual task to enter the vehicle with the variables above
            Function.Call(customerTask, customerHandle, vehicleHandle, timeToWarp, vehicleSeat, 1f, 1, 0);
            enteringVehicle = true;
        }
    }

    public void GiveDropOffDestination(bool canGiveCoordinates, Vector3 dropOffPos, Vehicle playerVehicle)
    {
        // This method is for the customer to provide the drop off destination, once they have entered the car

        // Checking if the customer has entered the car - SetupConditions() in RideHandler.cs
        if (canGiveCoordinates)
        {
            // Looping through the car passengers to check if the customer is in it
            foreach (Ped occupant in playerVehicle.Occupants)
            {
                if (occupant == customer)
                {
                    // Once the customer has entered the car, we are deleting their blip
                    _blipManager.DeleteBlip(ref customerBlip);

                    // When we drop off a customer at the destination we are transferring them to another variable so it is
                    // not deleted in front of the player, so when we pick up a new customer, this is when we actually delete 
                    // the previous one
                    DeletePreviousCustomer();

                    // Creating the destination blip after the customer gives us the location
                    destinationBlip = _blipManager.CreateBlip
                        (
                        dropOffPos,
                        BlipSprite.Standard,
                        BlipColor.Yellow,
                        "Destination",
                        true
                        );

                    // Assigning the current vehicle used for the mission
                    currentVehicle = playerVehicle;
                    hasEnteredCar = true;

                    // Here we are resetting the customer's invincibility
                    // and resetting the player to be able to get dragged out of the vehicle
                    Game.Player.Character.CanBeDraggedOutOfVehicle = true;
                    customer.IsInvincible = false;

                    break;
                }
            }
        }
    }

    public void LeavePlayerVehicle(bool canLeaveVehicle, Vector3 pickUpPos, Vector3 dropOffPos, int smallCollisions, int mediumCollisions, int bigCollisions)
    {
        // This method is for the customer to leave the vehicle when at drop off point

        // Checking if the player is at the drop off marker and the customer can leave - SetupConditions() in RideHandler.cs
        if (canLeaveVehicle)
        {
            // Creating variables for the rating, payment and tip
            float averageRating = CalculateAverageRating(smallCollisions, mediumCollisions, bigCollisions);
            int payment = CalculatePayment(pickUpPos, dropOffPos);
            int tip = CalculateTip(averageRating, payment);

            // Calculating the total payment
            int totalPayment = payment + tip;
            
            // Paying the ride
            PayRide(totalPayment);


            // Assigning the task for the customer to leave the vehicle
            customer?.Task.LeaveVehicle();

            // Deleting the destination blip
            _blipManager.DeleteBlip(ref destinationBlip);

            // Update and save the stats to the .ini file and the menu
            _menuManager.UpdateStats(totalPayment, averageRating);
            _menuManager.SaveStatsToFile();
            _menuManager.UpdateStatsMenu();

            // Resetting the current vehicle and telling the script that the customer was dropped off
            currentVehicle = null;
            wasDroppedOff = true;

            // Transferring the customer to another variable and deleting the current customer
            TransferCustomer();
            DeleteCustomer();

            // Notification for rating, payment and tip
            _notifications.SideNotification($"Rating: {averageRating:F1} / 5.0\nPayment: {payment} $\nTip: {tip} $");
        }
    }

    private float CalculateAverageRating(int smallCollisions, int mediumCollisions, int bigCollisions)
    {
        // This method is for calculating the rating based on the collisions from the ride duration

        // Max rating is 5
        float averageRating = 5f;

        // Here we are calculating the rating based on the collisions
        averageRating -= smallCollisions * 0.1f;
        averageRating -= mediumCollisions * 0.2f;
        averageRating -= bigCollisions * 0.5f;

        // If the rating is less than 1, we set it to 1 since that is the minimum
        if (averageRating < 1) { averageRating = 1; }

        // Checking if the user has enabled or disabled the average rating setting from the menu
        string averageRatingSetting = _menuManager._averageRatingOptions[_menuManager._averageRatingItem.Index];

        // If the setting is enabled we calculate the rating
        if (averageRatingSetting == "Enabled")
        {
            return averageRating;
        }

        // If the setting is disabled, we return the current average rating
        return _menuManager.averageRating;
    }

    private int CalculatePayment(Vector3 pickUpPos, Vector3 dropOffPos)
    {
        // This method is for calculating the payment

        int payment;
        float paymentDistance;

        // Here we are calculating the distance from the pickup point to the drop off point
        float travelDistance = World.CalculateTravelDistance(pickUpPos, dropOffPos) / 1609.34f;
        float getDistance = World.GetDistance(pickUpPos, dropOffPos) / 1609.34f;

        // The CalculateTravelDistance is not very accurate but is more accurate than GetDistance, however it bugs sometimes
        // if not all path nodes are loaded in game. I tried to overcome that but was unable so basically here we are checking
        // if the CalculateTravelDistance method is bugged or not. If it is bugged it will give 62 miles as distance, which is
        // far from the truth, so in these cases we use the GetDistance method, which is calculating the distance in 1 straight line
        if (travelDistance - getDistance <= 10f)
        {
            paymentDistance = travelDistance;
        }
        else
        {
            paymentDistance = getDistance;
        }

        // Rounding the payment to be an integer since the money in GTA is not a float
        payment = (int)Math.Round(paymentDistance * _menuManager.payPerMileSetting);

        return payment;
    }

    private int CalculateTip(float averageRating, int payment)
    {
        // This method is for calculating the tip, which is based on the rating from the customer

        float tip = 0f;
        int result;

        // If the rating is between 4.5 and 5.0, the customer will give the max tip of 40%
        if (averageRating >= 4.5f)
        {
            tip = 0.4f * payment;
        }

        // If the rating is between 3.5 and 4.5, the customer will give 30% tip
        if (averageRating >= 3.5f && averageRating < 4.5f)
        {
            tip = 0.3f * payment;
        }

        // If the rating is between 2.5 and 3.5, the customer will give 20% tip
        if (averageRating >= 2.5f && averageRating < 3.5f)
        {
            tip = 0.2f * payment;
        }

        // If the rating is between 1.5 and 2.5, the customer will give 10% tip
        if (averageRating >= 1.5f && averageRating < 2.5f)
        {
            tip = 0.1f * payment;
        }

        // If the rating is below 1.5, the customer will not give any tip
        if (averageRating < 1.5f)
        {
            tip = 0;
        }

        // Rounding the tip to be an integer as the money in GTA is not a float
        result = (int)Math.Round(tip);

        return result;
    }

    private void PayRide(int amount)
    {
        // This method is paying the ride, called a method to increase the player's money from the PlayerManager.cs

        _playerManager.IncreaseMoney(amount);
    }

    public void BrakeVehicle(bool canBrakeVehicle, Vehicle playerVehicle)
    {
        // This method is slowing down the vehicle, when it is on the markers

        // Checking if the player is on the marker and applying brakes based on the current speed
        // If the player is driving too fast, they will slow down a bit but will still fly through the marker
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
                // Once the player has stopped on the marker, we are disabling their controls until the customer gets in the car
                playerVehicle.Speed = 0f;
                _playerManager.DisableVehicleControls();
            }
        }
    }

    private void TransferCustomer()
    {
        // This method is transferring the customer to another variable when they are dropped off
        // The idea is to assign them a task to wander around so it looks more real
        // We null the customer variable so that we can create a new customer and later we delete the previous one

        if (droppedOffCustomer == null)
        {
            droppedOffCustomer = customer;
            droppedOffCustomer?.Task.WanderAround();
            customer = null;
        }
    }

    public void DeleteCustomer()
    {
        // This method is deleting the customer

        customer?.Delete();
        customer = null;
    }

    public void DeletePreviousCustomer()
    {
        // This method is deleting the dropped off customer when we pick up a new one

        droppedOffCustomer?.Delete();
        droppedOffCustomer = null;
    }
}
