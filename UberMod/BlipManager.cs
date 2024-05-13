using GTA;
using GTA.Math;

public class BlipManager
{
    public BlipManager()
	{
        // This class is managing the blips on the map
	}

    public Blip CreateBlip(Vector3 position, BlipSprite sprite, BlipColor color, string name, bool showRoute)
    {
        // This method is used to create blips whenever needed
        // - customer blip when you haven't picked them up yet
        // - car blip when the customer is in the car and you are not
        // - destination blip when you are in the car with the customer

        Blip blip = World.CreateBlip(position);
        blip.Sprite = sprite;
        blip.Color = color;
        blip.Name = name;
        blip.ShowRoute = showRoute;

        return blip;
    }

    public void UpdateBlip(Blip blip)
    {
        // This method is updating the blip route on the map since there is an issue where the route is disappearing sometimes

        blip.ShowRoute = false;
        blip.ShowRoute = true;
    }

    public void DeleteBlip(ref Blip? blip)
    {
        // This method is deleting the blips when needed since they can stack up if not deleted

        blip?.Delete();
        blip = null;
    }
}
