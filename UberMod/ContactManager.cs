using iFruitAddon2;
using NativeUI;

public class ContactManager
{
    private CustomiFruit _iFruit;
    private MenuManager _menuManager;

    public ContactManager(MenuManager menuManager)
    {
        _iFruit = new CustomiFruit();
        _menuManager = menuManager;
    }

    public void AddContact()
    {
        iFruitContact contactA = new("Uber Menu");
        contactA.Answered += ContactAnswered;
        contactA.DialTimeout = 2000;
        contactA.Active = true;
        contactA.Icon = ContactIcon.Taxi;
        _iFruit.Contacts.Add(contactA);
    }

    public void UpdateIFruit()
    {
        _iFruit.Update();
    }

    private void ContactAnswered(iFruitContact contact)
    {
        _iFruit.Close(1000);
        _menuManager.ShowMainMenu();
    }
}
