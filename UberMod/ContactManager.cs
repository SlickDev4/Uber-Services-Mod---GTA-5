using iFruitAddon2;

public class ContactManager
{
    private CustomiFruit _iFruit;
    private MenuManager _menuManager;

    public ContactManager(MenuManager menuManager)
    {
        // This class is creating the Uber Menu in the iFruit phone of the player

        _iFruit = new CustomiFruit();
        _menuManager = menuManager;
    }

    public void AddContact()
    {
        // This is the method creating the menu

        iFruitContact contactA = new("Uber Menu");
        contactA.Answered += ContactAnswered;
        contactA.DialTimeout = 2000;
        contactA.Active = true;
        contactA.Icon = ContactIcon.Taxi;
        _iFruit.Contacts.Add(contactA);
    }

    public void UpdateIFruit()
    {
        // This method is updating the iFruit phone

        _iFruit.Update();
    }

    private void ContactAnswered(iFruitContact contact)
    {
        // This method is called when the contact is called and answered

        _iFruit.Close(1000);
        _menuManager.ShowMainMenu();
    }
}
