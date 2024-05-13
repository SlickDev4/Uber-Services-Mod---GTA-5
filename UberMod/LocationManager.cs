using GTA;
using GTA.Math;

public class LocationManager
{
    private MenuManager _menuManager;
    public List<LocationPosition> LocationPositions { get; } = LocationPosition.Locations;
    public List<MarkerPosition> MarkerPositions { get; } = MarkerPosition.Markers;

    private static readonly Random random = new Random();

    // delete when done with coordinates
    private List<Blip> _blips = new List<Blip>();
    private bool blipsCreated = false;

    private readonly float minDistanceForPickup = 0.1f;
    private readonly float maxDistanceForPickup = 0.4f;
    private readonly float minDistanceForDropOff = 0.5f;

    private int calculationsPerFrame = 20;
    public int calculationsProgress = 0;
    public bool calculationsReady = false;

    private bool pickUpCalculated = false;
    private bool pickUpReady = false;

    private bool dropOffCalculated = false;
    private bool dropOffReady = false;

    private List<int> possiblePickupIndex = new();
    private List<int> possibleDropOffIndex = new();

    private int pickupIdx;
    private int dropOffIdx;

    private Vector3 pickUpPosition;
    private float closestPickUpDistance = 10000000f;
    private int closestPickUpIdx;


    public LocationManager(MenuManager menuManager)
    {
        // This class is managing the locations, it gets random pickup and customer location based on some conditions
        // and it gets a random drop off location again based on some conditions

        _menuManager = menuManager;
    }

    public (
        Vector3 customerPos, 
        Vector3 pickUpPos, 
        Vector3 dropOffPos, 
        float customerHeading
        )? GetCustomerLocation()
    {
        // This method is getting the generated locations and provides them to AssignPositions() in RideHandler.cs

        // Getting the customer location, pickup and dropoff markers from the hardcoded lists
        LocationPosition location = LocationPositions[pickupIdx];
        MarkerPosition pickUpMarker = MarkerPositions[pickupIdx];

        MarkerPosition dropOffMarker = MarkerPositions[dropOffIdx];
        
        // Providing them in variables for the script to assign them
        Vector3 customerPos = new(location.X, location.Y, location.Z);
        Vector3 pickUpPos = new(pickUpMarker.X, pickUpMarker.Y, pickUpMarker.Z);
        Vector3 dropOffPos = new(dropOffMarker.X, dropOffMarker.Y, dropOffMarker.Z);
        float customerHeading = location.Heading;
        
        return (customerPos, pickUpPos, dropOffPos, customerHeading);
    }

    public void CalculatePositions(Vector3 playerPos)
    {
        // This method is calculating the pickup and drop off locations randomly
        // Since the lists have 1080 locations, we made it to calculate 20 records per frame so it does not freeze the game

        // Here we are checking if the pickup is calculated
        if (!pickUpCalculated)
        {
            // If it's not calculated we loop through 20 records every frame
            for (int i = 0; i < calculationsPerFrame; i++)
            {
                // We are taking the current location from the list
                var currentLocation = LocationPositions[calculationsProgress];

                // We are taking the current position and calculating the distance from the player's position
                var currentPosition = new Vector3(currentLocation.X, currentLocation.Y, currentLocation.Z);
                var distance = World.CalculateTravelDistance(playerPos, currentPosition) / 1609.34f;
                
                // Here we are getting the closest position to the player in case
                // we don't find any pickup positions matching our conditions below
                if (distance < closestPickUpDistance)
                {
                    closestPickUpDistance = distance;
                    closestPickUpIdx = calculationsProgress;
                }

                // Here we are checking if the distance is more than the minimum and less than the maximum
                // If it is, we are adding the possible pickup locations in a list
                if (distance >= minDistanceForPickup && distance <= maxDistanceForPickup)
                {
                    possiblePickupIndex.Add(calculationsProgress);
                }

                // This is basically our loop counter that is saving the progress throughout the frames
                calculationsProgress++;

                // If the calculation progress reaches the final record, we break the loop
                if (calculationsProgress == LocationPositions.Count) { break; }

                // If the current index reaches the calculation per frame limit, we also break the loop
                if (i == calculationsPerFrame) { break; }
            }
        }

        // Here we are checking if we looped through all the pickup records and if the pickup is calculated
        if (calculationsProgress == LocationPositions.Count && !pickUpCalculated) 
        { 
            // Once we looped through everything, we indicate the script that the calculations are ready
            // and we reset the calculations progress
            pickUpCalculated = true; 
            calculationsProgress = 0;
        }

        // Here we are actually getting a pickup index, once the calculations are ready
        if (pickUpCalculated && !pickUpReady)
        {
            
            // If the list with possible indexes is empty, we are assigning the index to the closest pickup position
            if (possiblePickupIndex.Count == 0)
            {
                pickupIdx = closestPickUpIdx;
            }
            // Else we are getting a random index from the list and we are assigning it to the pickup
            else
            {
                var randomPickUpIndex = random.Next(0, possiblePickupIndex.Count);
                pickupIdx = possiblePickupIndex[randomPickUpIndex];
            }

            // Here we are getting the pickup position for later use in the drop off calculations
            pickUpPosition = new(LocationPositions[pickupIdx].X, LocationPositions[pickupIdx].Y, LocationPositions[pickupIdx].Z);

            // We are indicating that the pickup is ready so the script stops entering this part of the code
            pickUpReady = true;
        }
        
        // Here we are checking if the drop off is calculated and if the pickup calculations are ready
        // The script will enter this part of the code only when the pickup is calculated because we need to have
        // the pickup index before starting these calculations
        if (!dropOffCalculated && pickUpReady)
        {
            // If the pickup is ready, we start looping through the drop off list, again 20 records per frame
            for (int i = 0; i < calculationsPerFrame; i++)
            {
                // Taking the current location
                var currentLocation = MarkerPositions[calculationsProgress];
                // Taking the current position and calculating the distance - this time between the pickup position and the current one
                var currentPosition = new Vector3(currentLocation.X, currentLocation.Y, currentLocation.Z);
                var distance = World.CalculateTravelDistance(pickUpPosition, currentPosition) / 1609.34f;

                // Here we are checking if the distance is more than the minimum and if the index is not the same as the pickup index
                if (distance >= minDistanceForDropOff && calculationsProgress != pickupIdx)
                {
                    // Checking if the setting for routes is everywhere and adding all the possible indexes
                    if (_menuManager.routesSetting == "Everywhere")
                    {
                        possibleDropOffIndex.Add(calculationsProgress);
                    }
                    // If the routes setting is City, we only add indexes from the city
                    else if (_menuManager.routesSetting == "City" && calculationsProgress < 820)
                    {
                        possibleDropOffIndex.Add(calculationsProgress);
                    }
                    // If the routes setting is Countryside, we only add indexes from the countryside
                    else if (_menuManager.routesSetting == "Countryside" && calculationsProgress >= 820)
                    {
                        possibleDropOffIndex.Add(calculationsProgress);
                    }
                    // Here is a little bit more complex, we are checking if the route setting is City <-> Countryside
                    else if (_menuManager.routesSetting == "City <-> Countryside")
                    {
                        // If the pickup index is in the city, we only add locations from the countryside
                        if (pickupIdx < 820 && calculationsProgress >= 820)
                        {
                            possibleDropOffIndex.Add(calculationsProgress);
                        }
                        // If the pickup index is in the countryside, we only add locations from the city
                        else if (pickupIdx >= 820 && calculationsProgress < 820)
                        {
                            possibleDropOffIndex.Add(calculationsProgress);
                        }
                    }  
                }

                // Saving the progress throughout the frames
                calculationsProgress++;

                // If the whole drop off list is looped, we break
                if (calculationsProgress == MarkerPositions.Count) { break; }

                // If the current index reaches the calculations per frame limit, we break
                if (i == calculationsPerFrame) { break; }
            }
        }

        // Here we are checking if the drop off was calculated and indicate the script, also resetting the progress to 0
        if (calculationsProgress == MarkerPositions.Count && !dropOffCalculated)
        {
            dropOffCalculated = true;
            calculationsProgress = 0;
        }

        // Here we are checking if the drop off index was already taken after the calculations
        if (dropOffCalculated && !dropOffReady)
        {
            // If the drop off index list is empty, we are assigning a random index from the whole list
            // I don't think this will ever happen
            if (possibleDropOffIndex.Count == 0)
            {
                dropOffIdx = random.Next(0, MarkerPositions.Count);
            }
            // Else we are taking a random index from the possible drop offs and we are assigning it to the drop off variable
            else
            {
                var randomDropOffIndex = random.Next(0, possibleDropOffIndex.Count);
                dropOffIdx = possibleDropOffIndex[randomDropOffIndex];
            }

            // This is a protection to make sure that the pickup and drop off indexes will never be the same
            while (pickupIdx == dropOffIdx)
            {
                dropOffIdx = random.Next(0, MarkerPositions.Count);
            }

            // Indicating that the drop off index is ready
            dropOffReady = true;
        }

        // Here we are checking if both pickup and drop off indexes are taken
        // When they are, we indicate that to the script, so we can assign them in AssignPositions() in RideHandler.cs
        if (pickUpReady && dropOffReady)
        {
            calculationsReady = true;
        }  
    }

    public void ResetCalculationVariables()
    {
        // This method is resetting the calculation variables so we can perform the calculations again for the next customer

        calculationsReady = false;

        pickUpCalculated = false;
        pickUpReady = false;

        dropOffCalculated = false;
        dropOffReady = false;

        possiblePickupIndex.Clear();
        possibleDropOffIndex.Clear();

        closestPickUpDistance = 10000000f;
}
}
