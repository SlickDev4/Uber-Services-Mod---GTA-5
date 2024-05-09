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

public class BlipManager
{
    public BlipManager()
	{
	}

    public Blip CreateBlip(Vector3 position, BlipSprite sprite, BlipColor color, string name, bool showRoute)
    {
        Blip blip = World.CreateBlip(position);
        blip.Sprite = sprite;
        blip.Color = color;
        blip.Name = name;
        blip.ShowRoute = showRoute;

        return blip;
    }

    public void UpdateBlip(Blip blip)
    {
        blip.ShowRoute = false;
        blip.ShowRoute = true;
    }

    public void DeleteBlip(ref Blip? blip)
    {
        blip?.Delete();
        blip = null;
    }
}
