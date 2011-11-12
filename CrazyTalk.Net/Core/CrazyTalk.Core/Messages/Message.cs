using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using CrazyTalk.Core.Messages.Commands;

namespace CrazyTalk.Core.Messages
{
    /// <summary>
    /// Abstraction around communication message
    /// </summary>
    public class Message
    {
        public Message(long version, long id, Command command)
        {
            Contract.Requires(command != null);
            
            Version = version;
            Id = id;
            Command = command;
        }

        public long Version { get; private set; }
        public long Id { get; private set; }
        public Command Command { get; private set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Version: {0}", Version).AppendLine()
              .AppendFormat("Message Id: {0}", Id).AppendLine()
              .AppendFormat("{0}", Command);
            return sb.ToString();
        }
    }
}
