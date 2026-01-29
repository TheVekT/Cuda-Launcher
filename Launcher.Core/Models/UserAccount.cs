using System.Text.Json.Serialization;

namespace Launcher.Core.Models
{
    public class UserAccount
    {
        public string Username { get; set; }
        public string UUID { get; set; }
        public string AccessToken { get; set; }
        public bool IsOffline { get; set; }
        
        public bool IsSelected { get; set; }
        public string AccountTypeString => IsOffline ? "Offline" : "Microsoft";
        
        public UserAccount() 
        { 
        }
        public UserAccount(string name, string uuid, string token, bool isOffline)
        {
            Username = name;
            UUID = uuid;
            AccessToken = token;
            IsOffline = isOffline;
        }
    }
}