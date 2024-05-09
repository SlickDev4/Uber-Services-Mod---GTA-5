using System;
using GTA;
using GTA.UI;

public class Notifications
{
    private static readonly Random random = new Random();

    public Notifications()
	{

	}

	public void SubtitleNotification(string text, int time, bool notificationNeeded)
	{
		if (notificationNeeded)
		{
            GTA.UI.Screen.ShowSubtitle(text, time);
        }
	}

	public void SideNotification(string text)
	{
        GTA.UI.Notification.Show(text);
    }

	public void CrashNotifications(string crashSeverity)
	{
        int randomIndex = random.Next(0, smallCrashes.Count);

        if (crashSeverity == "small") { SubtitleNotification(smallCrashes[randomIndex], 3000, true); } 
        else if (crashSeverity == "medium") { SubtitleNotification(mediumCrashes[randomIndex], 3000, true); }
        else if (crashSeverity == "big") { SubtitleNotification(bigCrashes[randomIndex], 3000, true); }
    }

    private static List<string> smallCrashes { get; } = new List<string>
    {
        "That was a close one!",
        "Your car got scratched! Not cool.",
        "Seriously? Another fender bender!",
        "Great, just what I needed.",
        "Come on, watch where you're going!",
        "That's gonna leave a mark.",
    };

    private static List<string> mediumCrashes { get; } = new List<string>
    {
        "Damn it, your bumper's wrecked!",
        "Seriously? That's gonna cost you.",
        "Oh great, now I have to deal with this.",
        "Now you need a new paint job!",
        "Thanks a lot for bumping my head.",
        "This day just keeps getting better.",
    };

    private static List<string> bigCrashes { get; } = new List<string>
    {
        "Are you kidding me?! Your car's totaled!",
        "I can't believe this! What a disaster.",
        "That's it, I'm taking the bus from now on.",
        "Well, there goes your insurance premium.",
        "Unbelievable! This is gonna take forever to fix.",
        "Could this day get any worse?",
    };
}
