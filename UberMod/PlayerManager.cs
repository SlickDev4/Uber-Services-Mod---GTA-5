using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;

public class PlayerManager
{
    private MenuManager _menuManager;
    private Ped _player;

    private Ped? previousPlayerPed;

    public PlayerManager(MenuManager menuManager)
	{
        _menuManager = menuManager;
        previousPlayerPed = Game.Player.Character;
    }

    private Ped GetPlayer()
    {
        return Game.Player.Character;
    }

    public bool PlayerHasChanged()
    {
        Ped currentPlayerPed = Game.Player.Character;

        if (currentPlayerPed != previousPlayerPed)
        {
            previousPlayerPed = currentPlayerPed;
            return true;
        }

        return false;
    }

	public string getPlayerStatus()
	{
        if (_menuManager.GetStatus() == "Available")
        {
            return "available";
        }

        return "unavailable";
	}

    public Vector3 getPlayerPosition()
    {
        return GetPlayer().Position;
    }

    public Vehicle GetPlayerVehicle()
    {
        return GetPlayer().CurrentVehicle;
    }

    public void IncreaseMoney(int amount)
    {
        Game.Player.Money += amount;
    }

    public void DisableVehicleControls()
    {
        Game.DisableControlThisFrame(GTA.Control.VehicleAccelerate);
        Game.DisableControlThisFrame(GTA.Control.VehicleBrake);
        Game.DisableControlThisFrame(GTA.Control.VehicleExit);
    }

    public void EnableVehicleControls()
    {
        Game.EnableControlThisFrame(GTA.Control.VehicleAccelerate);
        Game.EnableControlThisFrame(GTA.Control.VehicleBrake);
        Game.EnableControlThisFrame(GTA.Control.VehicleExit);
    }
}
