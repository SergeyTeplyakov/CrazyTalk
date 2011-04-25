using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CrazyTalk.Core.Messages
{
    /// <summary>
    /// Abstract interface for converting message from xml data.
    /// </summary>
    [ContractClass(typeof(MessageConverterContracts))]
    public interface IMessageConverter
    {
        Message ConvertFromXml(XElement element);
        XElement ConvertToXml(Message message);
    }

    [ContractClassFor(typeof(IMessageConverter))]
    internal class MessageConverterContracts : IMessageConverter
    {
        public Message ConvertFromXml(XElement element)
        {
            Contract.Requires(element != null);
            Contract.Ensures(Contract.Result<Message>() != null);

            return default(Message);
        }

        public XElement ConvertToXml(Message message)
        {
            Contract.Requires(message != null);
            Contract.Ensures(Contract.Result<XElement>() != null);

            return default(XElement);
        }
    }
}
