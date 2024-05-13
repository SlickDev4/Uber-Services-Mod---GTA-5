using GTA;

public class PlayerManager
{
    private Ped? previousPlayerPed;

    public PlayerManager()
	{
        // This class is handling the Player

        previousPlayerPed = Game.Player.Character;
    }

    public bool PlayerHasChanged()
    {
        // This method is checking if the player has changed between Franklin/Michael/Trevor

        Ped currentPlayerPed = Game.Player.Character;

        // Basically we are checking if the current player is the same as the player that the game loaded with
        // If it is not the same, we assign the current player, we use this so we can abort the mission if the player changes
        // during a ride
        if (currentPlayerPed != previousPlayerPed)
        {
            previousPlayerPed = currentPlayerPed;
            return true;
        }

        return false;
    }

    public void IncreaseMoney(int amount)
    {
        // This method is simply increasing the player's money by the given amount

        Game.Player.Money += amount;
    }

    public void DisableVehicleControls()
    {
        // This method is disabling 3 controls
        // - Player can't accelerate with the vehicle
        // - Player can't go backwards/brake with the vehicle
        // - Player can't exit the vehicle
        // This is used while the customer is entering or leaving the vehicle on the pickup/drop off points

        Game.DisableControlThisFrame(GTA.Control.VehicleAccelerate);
        Game.DisableControlThisFrame(GTA.Control.VehicleBrake);
        Game.DisableControlThisFrame(GTA.Control.VehicleExit);
    }
}
