using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrazyTalk.Core.Messages;

namespace CrazyTalk.Core
{
    /// <summary>
    /// Interface for CrazyTalks server.
    /// Because we're expecting many different implementations it's ideal for dependency
    /// design principle.
    /// </summary>
    public abstract class CrazyServerBase
    {
        protected abstract void MessageReceived(Message message);
    }
}
