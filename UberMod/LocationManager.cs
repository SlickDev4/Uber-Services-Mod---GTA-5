using System;
using System.Collections.Generic;
using System.Diagnostics;
using GTA;
using GTA.Math;

public class LocationManager
{
    public List<LocationPosition> LocationPositions { get; } = LocationPosition.Locations;
    public List<MarkerPosition> MarkerPositions { get; } = MarkerPosition.Markers;

    private static readonly Random random = new Random();

    // delete when done with coordinates
    private List<Blip> _blips = new List<Blip>();
    private bool blipsCreated = false;

    private readonly float minDistanceForPickup = 0.1f;
    private readonly float maxDistanceForPickup = 0.4f;
    private readonly float minDistanceForDropOff = 0.5f;

    // delete after testing
    private Stopwatch stopwatch = new Stopwatch();

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

    private string pickUpLocation;
    private string dropOffLocation;


    public LocationManager()
    {
    }

    public void CreateBlipsForMarkers()
    {
        if (!blipsCreated)
        {
            for (int i = 820; i < MarkerPositions.Count; i++)
            {

                // CITY
                // 0 - 371 -> first batch
                // 372 - 619 -> second batch
                // 620 - 780 -> third batch
                // 781 - 819 -> forth batch

                // COUNTRYSIDE
                // 820 - last - fifth batch

                var marker = MarkerPositions[i];

                Blip blip = World.CreateBlip(new Vector3(marker.X, marker.Y, marker.Z));
                blip.Sprite = BlipSprite.Standard;
                blip.Color = BlipColor.Green;
                blip.Name = "Marker";
                
                _blips.Add(blip);

                //if (i == 371) { break; }
            }
        }
        blipsCreated = true;
    }

    public void RemoveAllBlips()
    {
        foreach (Blip blip in _blips)
        {
            blip.Delete();
        }
        _blips.Clear();
        blipsCreated = false;
    }

    public (
        Vector3 customerPos, 
        Vector3 pickUpPos, 
        Vector3 dropOffPos, 
        float customerHeading
        )? GetCustomerLocation()
    {
        int pup = 821;
        int dop = 54;

        LocationPosition location = LocationPositions[pup];
        MarkerPosition pickUpMarker = MarkerPositions[pup];

        MarkerPosition dropOffMarker = MarkerPositions[dop];
        
        Vector3 customerPos = new(location.X, location.Y, location.Z);
        Vector3 pickUpPos = new(pickUpMarker.X, pickUpMarker.Y, pickUpMarker.Z);
        Vector3 dropOffPos = new(dropOffMarker.X, dropOffMarker.Y, dropOffMarker.Z);
        float customerHeading = location.Heading;
        
        return (customerPos, pickUpPos, dropOffPos, customerHeading);
    }

    public void CalculatePositions(Vector3 playerPos)
    {
        // calculating the PICKUP indexes

        if (!pickUpCalculated)
        {
            
            for (int i = 0; i < calculationsPerFrame; i++)
            {
                var currentLocation = LocationPositions[calculationsProgress];
                var currentPosition = new Vector3(currentLocation.X, currentLocation.Y, currentLocation.Z);
                var distance = World.CalculateTravelDistance(playerPos, currentPosition) / 1609.34f;
                
                if (distance < closestPickUpDistance)
                {
                    closestPickUpDistance = distance;
                    closestPickUpIdx = calculationsProgress;
                }

                if (distance >= minDistanceForPickup && distance <= maxDistanceForPickup)
                {
                    possiblePickupIndex.Add(calculationsProgress);
                }

                calculationsProgress++;

                if (calculationsProgress == LocationPositions.Count) { break; }
                if (i == calculationsPerFrame) { break; }
            }
        }

        // calculating PICKUP finished

        if (calculationsProgress == LocationPositions.Count && !pickUpCalculated) 
        { 
            pickUpCalculated = true; 
            calculationsProgress = 0;
        }

        // taking the PICKUP index

        if (pickUpCalculated && !pickUpReady)
        {
            
            if (possiblePickupIndex.Count == 0)
            {
                pickupIdx = closestPickUpIdx;
            }
            else
            {
                var randomPickUpIndex = random.Next(0, possiblePickupIndex.Count);

                pickupIdx = possiblePickupIndex[randomPickUpIndex];

                
            }

            pickUpPosition = new(LocationPositions[pickupIdx].X, LocationPositions[pickupIdx].Y, LocationPositions[pickupIdx].Z);

            pickUpReady = true;
        }
        
        // calculating the DROPOFF indexes

        if (!dropOffCalculated && pickUpReady)
        {
            for (int i = 0; i < calculationsPerFrame; i++)
            {
                var currentLocation = MarkerPositions[calculationsProgress];
                var currentPosition = new Vector3(currentLocation.X, currentLocation.Y, currentLocation.Z);
                var distance = World.CalculateTravelDistance(pickUpPosition, currentPosition) / 1609.34f;

                if (distance >= minDistanceForDropOff && calculationsProgress != pickupIdx)
                {
                    possibleDropOffIndex.Add(calculationsProgress);
                }

                calculationsProgress++;

                if (calculationsProgress == MarkerPositions.Count) { break; }
                if (i == calculationsPerFrame) { break; }
            }
        }

        // calculating DROPOFF finished

        if (calculationsProgress == MarkerPositions.Count && !dropOffCalculated)
        {
            dropOffCalculated = true;
            calculationsProgress = 0;
        }

        // taking DROPOFF index

        if (dropOffCalculated && !dropOffReady)
        {
            if (possibleDropOffIndex.Count == 0)
            {
                dropOffIdx = random.Next(0, MarkerPositions.Count);
            }
            else
            {
                var randomDropOffIndex = random.Next(0, possibleDropOffIndex.Count);

                dropOffIdx = possibleDropOffIndex[randomDropOffIndex];
            }

            while (pickupIdx == dropOffIdx)
            {
                dropOffIdx = random.Next(0, MarkerPositions.Count);
            }

            dropOffReady = true;
        }

        // calculations are ready - pass to the assign positions

        if (pickUpReady && dropOffReady)
        {
            calculationsReady = true;

            if (pickupIdx < 820) { pickUpLocation = "city";  }
            else { pickUpLocation = "countryside";  }

            if (dropOffIdx < 820) { dropOffLocation = "city"; }
            else { dropOffLocation = "countryside";  }
        }
        
    }

    public void ResetCalculationVariables()
    {
        calculationsReady = false;

        pickUpCalculated = false;
        pickUpReady = false;

        dropOffCalculated = false;
        dropOffReady = false;

        possiblePickupIndex.Clear();
        possibleDropOffIndex.Clear();

        closestPickUpDistance = 10000000f;
}

    public void TestDistances()
    {
        using StreamWriter writer = new StreamWriter("scripts/ulog.txt");

        float maxValue = 0;
        float maxGetDistanceValue = 0;

        for (int i = 372; i < LocationPositions.Count; i++)
        {
            var currentLocation = LocationPositions[i];
            var pickupPos = new Vector3(currentLocation.X, currentLocation.Y, currentLocation.Z);

            for (int j = 372; j < MarkerPositions.Count; j++)
            {
                var currentMarker = MarkerPositions[j];
                var dropOffPos = new Vector3(currentMarker.X, currentMarker.Y, currentMarker.Z);

                var distance = World.CalculateTravelDistance(pickupPos, dropOffPos) / 1609.34f;
                var distance2 = World.GetDistance(pickupPos, dropOffPos) / 1609.34f;

                if (distance > 10f)
                {
                    writer.WriteLine($"Pickup [{i}] - Drop Off [{j}]\nTravel Distance [{distance}] miles --- GetDistance [{distance2}] miles\n");
                }

                float result = distance - distance2;
                

                if (result > maxValue) { maxValue = result; }
                if (distance2 > maxGetDistanceValue) {  maxGetDistanceValue = distance2; }
                


                //if (distance - distance2 >= 1)
                //{
                //    writer.WriteLine($"Difference between Travel and Get Distance - {distance - distance2} miles diff");
                //}
            }
        }

        writer.WriteLine($"Max Difference from Travel and Get Distance - {maxValue}");
        writer.WriteLine($"Max Get Distance Value - {maxGetDistanceValue}");
    }
}
