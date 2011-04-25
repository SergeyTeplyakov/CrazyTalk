using System.Diagnostics.Contracts;

namespace CrazyTalk.Core.Messages.Commands
{
    public class LoginCommand : Command
    {
        public LoginCommand(UserInfo userInfo)
            : base(CommandType.Login)
        {
            Contract.Requires(userInfo != null);

            UserInfo = userInfo;
        }

        public UserInfo UserInfo { get; private set; }

        public override string ToString()
        {
            return string.Format("LoginCommand: User name = {0}", UserInfo.Name);
        }
    }
}