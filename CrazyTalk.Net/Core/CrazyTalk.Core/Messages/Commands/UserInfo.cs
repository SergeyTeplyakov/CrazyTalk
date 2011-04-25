using System.Diagnostics.Contracts;

namespace CrazyTalk.Core.Messages.Commands
{
    /// <summary>
    /// User information
    /// </summary>
    public class UserInfo
    {
        public UserInfo(string userName)
        {
            Contract.Requires(userName != null);

            Name = userName;
        }
        public string Name { get; set; }
    }
}