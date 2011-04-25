namespace CrazyTalk.Core.Messages.Commands
{
    /// <summary>
    /// Command types enumeration
    /// </summary>
    public enum CommandType
    {
        Login,
        UserState,

        Ack,

        TextMessage,
    }

    /// <summary>
    /// Base class for all commands that we could send in Messages
    /// </summary>
    public abstract class Command
    {
        protected Command(CommandType commandType)
        {
            CommandType = commandType;
        }

        public CommandType CommandType { get; private set; }
    }
}