using System.Diagnostics.Contracts;

namespace CrazyTalk.Core.Messages.Commands
{
    public class UserStateCommand : Command
    {
        public UserStateCommand(UserInfo userInfo, string userState)
            : base(CommandType.UserState)
        {
            Contract.Requires(userInfo != null);
            Contract.Requires(userState != null);

            UserInfo = userInfo;
            UserState = userState;
        }

        public UserInfo UserInfo { get; private set; }

        public string UserState { get; private set; }

        public override string ToString()
        {
            return string.Format("UserStateCommand: User name = {0}, state = {1}", UserInfo.Name, UserState);
        }
    }
}