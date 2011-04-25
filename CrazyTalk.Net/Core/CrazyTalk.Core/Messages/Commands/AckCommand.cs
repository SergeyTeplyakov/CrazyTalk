namespace CrazyTalk.Core.Messages.Commands
{
    public class AckCommand : Command
    {
        public AckCommand(long messageId)
            : base(CommandType.Ack)
        {
            MessageId = messageId;
        }

        public long MessageId { get; private set; }
        
        public override string ToString()
        {
            return string.Format("AckCommand: Ack message Id = {0}", MessageId);
        }
    }

}