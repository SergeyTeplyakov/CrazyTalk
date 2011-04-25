using System.Diagnostics.Contracts;

namespace CrazyTalk.Core.Messages.Commands
{
    public class TextMessageCommand : Command
    {

        public TextMessageCommand(UserInfo from, UserInfo to, string message)
            : base(CommandType.TextMessage)
        {
            Contract.Requires(from != null);
            Contract.Requires(to != null);
            Contract.Requires(message != null);

            From = from;
            To = to;
            Message = message;
        }

        public UserInfo From { get; private set; }
        public UserInfo To { get; private set; }
        public string Message { get; private set; }

        public override string ToString()
        {
            return string.Format("TextMessageCommand: From = {0}, To = {1}, Message = {2}",
                                 From.Name, To.Name, Message);
        }
    }
}