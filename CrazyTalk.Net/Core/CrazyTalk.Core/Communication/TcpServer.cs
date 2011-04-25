//-----------------------------------------------------------------------------------------------//
// Wrapper around TcpListener that manages connections and data receiving processes
//
// Author:    Sergey Teplyakov
// Date:      April 24, 2011
//-----------------------------------------------------------------------------------------------//


using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Threading;
using CrazyTalk.Core.Utils;
using System.Runtime.InteropServices;


namespace CrazyTalk.Core.Communication
{
    /// <summary>
    /// Wrapper around TcpListener that manages connections and data receiving processes
    /// </summary>
    public sealed class TcpServer : IDisposable
    {
        //---------------------------------------------------------------------------------------//
        // EventArgs classes
        //---------------------------------------------------------------------------------------//

        /// <summary>
        /// Simple EnentArgs descendant with additional IPEndPoint property
        /// </summary>
        public class SocketEndPointEventArgs : EventArgs
        {
            public SocketEndPointEventArgs(IPEndPoint ipEndPoint)
            {
                Contract.Requires(ipEndPoint != null);

                IPEndPoint = ipEndPoint;
            }

            public IPEndPoint IPEndPoint { get; private set; }
        }

        /// <summary>
        /// In some cases we can't handle exceptions locally.
        /// For example, we can't handle exceptions in dipse method, we should swallow
        /// that exception and not throw  from Dispose, but we should notify our caller.
        /// </summary>
        public class TcpServerErrorEventArgs : EventArgs
        {
            public TcpServerErrorEventArgs(Exception error)
            {
                Contract.Requires(error != null);

                Error = error;
            }

            public Exception Error { get; private set; }
        }

        /// <summary>
        /// EventArgs descendant that holds data received from appropriate EndPoint
        /// </summary>
        public class DataReceivedEventArgs : EventArgs
        {
            public DataReceivedEventArgs(IPEndPoint ipEndPoint, byte[] data)
            {
                Contract.Requires(ipEndPoint != null);
                Contract.Requires(data != null);

                IPEndPoint = ipEndPoint;
                Data = data;
            }

            public IPEndPoint IPEndPoint { get; private set; }
            public byte[] Data { get; private set; }
        }

        /// <summary>
        /// This exception throws when we can't find appropriate socket by IPEndPoint
        /// </summary>
        public class SocketNotFoundException : Exception
        {
            public SocketNotFoundException(IPEndPoint ipEndPoint)
                : base(string.Format("Socket not found. IPEndPoint: {0}", ipEndPoint))
            {
                Contract.Requires(ipEndPoint != null);

                IPEndPoint = ipEndPoint;
            }

            public IPEndPoint IPEndPoint { get; private set; }
        }

        //---------------------------------------------------------------------------------------//
        // Private fields
        //---------------------------------------------------------------------------------------//
        #region Private fields
        private const int SocketBufferSize = 10 * 1024;
        private readonly TcpListener tcpServer;
        private bool disposed;
        private readonly Dictionary<IPEndPoint, Socket> socketsDictionary;
        private readonly ReaderWriterLockSlim socketsDictionaryLock = new ReaderWriterLockSlim();
        //private readonly object socketsDictionaryLock = new object();
        #endregion Private fields

        //---------------------------------------------------------------------------------------//
        // Construction, Destruction
        //---------------------------------------------------------------------------------------//
        #region Construction, Destruction

        public TcpServer(int port)
        {
            Contract.Requires(0 <= port && port <= ushort.MaxValue);

            socketsDictionary = new Dictionary<IPEndPoint, Socket>();
            tcpServer = new TcpListener(IPAddress.Any, port);
            tcpServer.Start();
            tcpServer.BeginAcceptSocket(EndAcceptSocket, tcpServer);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            try
            {
                tcpServer.Stop();
                using (socketsDictionaryLock.UseWriteLock())
                {
                    foreach (Socket sock in socketsDictionary.Values)
                    {
                        sock.Close();
                    }
                    socketsDictionary.Clear();
                }
                
            }
            catch (SocketException ex)
            {
                //TODO: add calling to event!
                Console.WriteLine("tcpServer.Stop failed: " + ex);
            }
        }
        #endregion Construction, Destruction

        //---------------------------------------------------------------------------------------//
        //Public Methods
        //---------------------------------------------------------------------------------------//
        #region Public Methods

        public void SendString(IPEndPoint ipEndPoint, string str)
        {
            Contract.Requires(ipEndPoint != null);
            Contract.Requires(str != null);

            var output = ASCIIEncoding.UTF8.GetBytes(str);
            SendData(ipEndPoint, output);
        }

        public void SendData(IPEndPoint ipEndPoint, byte[] data)
        {
            Contract.Requires(ipEndPoint != null);
            Contract.Requires(data != null);

            // Unfortunately we can't use precondition like: socketsDictionary.ContainsKey(ipEndPoint)
            // because potentially we could face concurrency issues. It's possible that this precondition
            // will not violated before call, but will fails during the call

            Socket sock;
            using(socketsDictionaryLock.UseReadLock())
            {
                if (!socketsDictionary.ContainsKey(ipEndPoint))
                    throw new SocketNotFoundException(ipEndPoint);
                
                sock = socketsDictionary[ipEndPoint];
            }
            sock.Send(data);
        }
        #endregion Public Methods

        //---------------------------------------------------------------------------------------//
        //Events
        //---------------------------------------------------------------------------------------//
        #region Events
        public event EventHandler<SocketEndPointEventArgs> SocketConnected;
        public event EventHandler<SocketEndPointEventArgs> SocketDisconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        private void OnSocketConnected(IPEndPoint ipEndPoint)
        {
            Contract.Assert(ipEndPoint != null);

            var handler = SocketConnected;
            if (handler != null)
                handler(this, new SocketEndPointEventArgs(ipEndPoint));
        }

        private void OnSocketDisconnected(IPEndPoint ipEndPoint)
        {
            Contract.Assert(ipEndPoint != null);

            var handler = SocketDisconnected;
            if (handler != null)
                handler(this, new SocketEndPointEventArgs(ipEndPoint));
        }

        private void OnDataReceived(IPEndPoint ipEndPoint, byte[] data)
        {
            Contract.Assert(ipEndPoint != null);
            Contract.Assert(data != null);

            var handler = DataReceived;
            if (handler != null)
                handler(this, new DataReceivedEventArgs(ipEndPoint, data));
        }

        #endregion Events

        //---------------------------------------------------------------------------------------//
        //Private Functions
        //---------------------------------------------------------------------------------------//
        #region Private Functions

        // Handling new connection
        private void Connect(Socket socket)
        {
            Contract.Requires(socket != null);

            IPEndPoint endPoint = (IPEndPoint)socket.RemoteEndPoint;

            Console.WriteLine("Accepting connection from: {0}", endPoint);

            using(socketsDictionaryLock.UseWriteLock())
            {
                //если уже есть соединение от того же узла, текущее соединение закрываю
                if (socketsDictionary.ContainsKey(endPoint))
                {
                    //theLog.Log.DebugFormat("TcpServer.Connected: Socket already connected! Removing from local storage! EndPoint: {0}", endPoint);
                    socketsDictionary[endPoint].Close();
                }

                SetDesiredKeepAlive(socket);
                socketsDictionary[endPoint] = socket;
            }

            OnSocketConnected(endPoint);
        }

        // Helper method that sets desired KeepAlive interval
        private static void SetDesiredKeepAlive(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            const uint time = 10000;
            const uint interval = 20000;
            SetKeepAlive(socket, true, time, interval);
        }

        private static void SetKeepAlive(Socket s, bool on, uint time, uint interval)
        {
            /* the native structure
            struct tcp_keepalive {
            ULONG onoff;
            ULONG keepalivetime;
            ULONG keepaliveinterval;
            };
            */

            // marshal the equivalent of the native structure into a byte array
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)time).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
            // of course there are other ways to marshal up this byte array, this is just one way

            // call WSAIoctl via IOControl
            int ignore = s.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);

        }

        // Handing socket disconnection
        private void Disconnect(Socket socket)
        {
            Contract.Requires(socket != null);

            IPEndPoint endPoint = (IPEndPoint)socket.RemoteEndPoint;
            socket.Close();
            using(socketsDictionaryLock.UseWriteLock())
            {
                socketsDictionary.Remove(endPoint);
            }
            OnSocketDisconnected(endPoint);
        }

        // Handing incoming data
        private void ReceiveData(byte[] data, IPEndPoint endPoint)
        {
            Contract.Requires(data != null);
            Contract.Requires(endPoint != null);

            OnDataReceived(endPoint, data);
        }
        
        // Callback method that called when AcceptSocket occurs
        private void EndAcceptSocket(IAsyncResult asyncResult)
        {
            TcpListener listener = (TcpListener)asyncResult.AsyncState;
            try
            {
                if (disposed)
                {
                    Console.WriteLine("TcpServer.EndAcceptSocket: tcp server already disposed!");
                    return;
                }

                // Retrieving new socket
                Socket sock = listener.EndAcceptSocket(asyncResult);
                
                // Handling new connection
                Connect(sock);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.Completed += ReceiveCompleted;
                e.SetBuffer(new byte[SocketBufferSize], 0, SocketBufferSize);
                BeginReceiveAsync(sock, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error accepting socket! {0}", ex);
            }
            finally
            {
                TryBeginAcceptSocket(listener);
            }
        }

        private void TryBeginAcceptSocket(TcpListener listener)
        {
            try
            {
                listener.BeginAcceptSocket(EndAcceptSocket, listener);
            }
            catch (SocketException e)
            {
                Console.WriteLine("BeginAcceptSocket failed with SocketException! {0}", e);
                // TODO: call error event
            }
        }

        // Start wating data for specified socket
        private void BeginReceiveAsync(Socket sock, SocketAsyncEventArgs e)
        {
            if (!sock.ReceiveAsync(e))
            {
                // IO operation finished synchronously
                // Lets call data handler manually
                ReceiveCompleted(sock, e);
            }
        }

        // Data received event handler
        void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            Socket sock = (Socket)sender;
            
            if (!sock.Connected)
            {
                Disconnect(sock);
                return;
            }

            if (e.SocketError != SocketError.Success)
            {
                //TODO: ????
                //theLog.Log.Info("ReceiveComplete: ошибка приема данных: " + e.SocketError);
                return;
            }

            try
            {

                int size = e.BytesTransferred;
                if (size == 0)
                {
                    // This means that socket disconnected
                    Disconnect(sock);
                }
                else
                {
                    byte[] buf = new byte[size];
                    Array.Copy(e.Buffer, buf, size);
                    ReceiveData(buf, (IPEndPoint)sock.RemoteEndPoint);

                    BeginReceiveAsync(sock, e);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("ReceiveCompleted socket error: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveCompleted error: " + ex.Message);
            }
        }
        #endregion Private Functions

    }

}
