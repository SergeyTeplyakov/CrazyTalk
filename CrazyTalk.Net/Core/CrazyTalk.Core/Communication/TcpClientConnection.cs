//-----------------------------------------------------------------------------------------------//
// Wrapper around System.Sockets.TcpClient
//
// Author:    Sergey Teplyakov
// Date:      April 24, 2011
//-----------------------------------------------------------------------------------------------//


using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using CrazyTalk.Core.Messages;

namespace CrazyTalk.Core.Communication
{    

    /// <summary>
    /// Wrapper around tcp client connection
    /// </summary>
    public sealed class TcpClientConnection : IDisposable
    {
        //---------------------------------------------------------------------------------------//
        // EventArgs
        //---------------------------------------------------------------------------------------//

        /// <summary>
        /// Event that occurs when client disconnects from remote server
        /// </summary>
        public class SocketDisconnectedEventArgs : EventArgs
        {
            public SocketDisconnectedEventArgs(Exception error)
            {
                Error = error;
            }

            // Error is optional
            public Exception Error { get; private set; }
        }

        /// <summary>
        /// EventArgs that holds data from remote host
        /// </summary>
        public class DataReceivedEventArgs : EventArgs
        {
            public DataReceivedEventArgs(string data)
            {
                Contract.Requires(data != null);
                Data = data;
            }
            public string Data { get; private set; }
        }

        //---------------------------------------------------------------------------------------//
        // Private Fields
        //---------------------------------------------------------------------------------------//

        private readonly TcpClient tcpClient;
        private readonly NetworkStream stream;
        private readonly byte[] buffer;

        //---------------------------------------------------------------------------------------//
        // Construction, Destruction
        //---------------------------------------------------------------------------------------//

        /// <summary>
        /// Connects to remote server
        /// </summary>
        public TcpClientConnection(IPEndPoint endPoint)
        {
            Contract.Requires(endPoint != null);
            
            buffer = new byte[10 * 1024];
            tcpClient = new TcpClient();
            tcpClient.Connect(endPoint);
            stream = tcpClient.GetStream();
            stream.BeginRead(buffer, 0, buffer.Length, EndRead, null);
        }

        public void Dispose()
        {
            try
            {
                tcpClient.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("TcpClientConnection: disposing failed! {0}", ex);
            }
        }


        //---------------------------------------------------------------------------------------//
        // Public interface
        //---------------------------------------------------------------------------------------//
        public void SendString(string data)
        {
            Contract.Requires(!string.IsNullOrEmpty(data));

            Console.WriteLine("Sending message: {0}", data);

            //string text = messageConverter.ConvertToXml(message).ToString();
            //Console.WriteLine("Text: {0}", text);
            var output = ASCIIEncoding.UTF8.GetBytes(data);
            if (output.Length != 0)
            {
                Console.WriteLine("Sending data into output stream. Size: {0}", output.Length);
                stream.Write(output, 0, output.Length);
                stream.Flush();
            }
        }

        //---------------------------------------------------------------------------------------//
        // Events
        //---------------------------------------------------------------------------------------//
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<SocketDisconnectedEventArgs> SocketDisconnected;

        private void OnDataReceived(string data)
        {
            Contract.Requires(!string.IsNullOrEmpty(data));

            var handler = DataReceived;
            if (handler != null)
                handler(this, new DataReceivedEventArgs(data));
        }

        private void OnSocketDisconnected(Exception error = null)
        {
            var handler = SocketDisconnected;
            if (handler != null)
                handler(this, new SocketDisconnectedEventArgs(error));
        }
        
        //---------------------------------------------------------------------------------------//
        // Private Functions
        //---------------------------------------------------------------------------------------//
        private void EndRead(IAsyncResult result)
        {
            try
            {

                if (result == null)
                    return;
                // One chunk of data could contains more than 1 message and 1 message
                // could be splitted into several chunks of data
                // Now we're ignoring this!
                var count = stream.EndRead(result);
                var data = ASCIIEncoding.UTF8.GetString(buffer, 0, count);
                OnDataReceived(data);
            }
            catch (SocketException e)
            {
                Console.WriteLine("TcpClientConnection: EndRead failes! {0}", e);
                CheckSocketConnection(e);
            }
            catch (Exception ex)
            {
                // If socket is closed we could get IOException with SocketExcpetion as
                // inner exception
                Console.WriteLine("TcpClientConnection: EndRead failes! {0}", ex);
                CheckSocketConnection(ex);
            }
            finally
            {
                TryBeginRead();
            }
        }

        private void TryBeginRead()
        {
            try
            {
                if (tcpClient.Connected)
                    stream.BeginRead(buffer, 0, buffer.Length, EndRead, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("TcpClientConnection: error begin reading! {0}", ex);
                CheckSocketConnection(ex);
            }
            
        }

        private void CheckSocketConnection(Exception e)
        {
            if (!tcpClient.Connected)
            {
                OnSocketDisconnected(e);
            }
        }

        //private void OnMessageReceived(Message message)
        //{
        //    var handler = MessageReceived;
        //    if (handler != null)
        //        handler(this, new DataReceivedEventArgs(message));
        //}

        //private void OnInvalidStateMessageReceived(string textMessage, IEnumerable<string> errors)
        //{
        //    var handler = InvalidStateMessageReceived;
        //    if (handler != null)
        //        handler(this, new InvalidStateMessageReceivedEventArgs(ServerInfo, textMessage, errors));
        //}
        //private void OnSocketDisconnected(Exception error)
        //{
        //    var handler = SocketDisconnected;
        //    if (handler != null)
        //        handler(this, new SocketDisconnectedEventArgs(error));
        //}

    }

}