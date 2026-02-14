namespace Launcher.UI.WPF.Helpers
{
    public enum AccountType
    {
        Microsoft,
        Offline
    }
    
    [Flags]
    public enum ConfirmButtons
    {
        None = 0,
        Confirm = 1,       
        Delete = 2,       
        Cancel = 4, 
        
        DeleteAndCancel = Delete | Cancel,
        ConfirmAndCancel = Confirm | Cancel
    }
}
