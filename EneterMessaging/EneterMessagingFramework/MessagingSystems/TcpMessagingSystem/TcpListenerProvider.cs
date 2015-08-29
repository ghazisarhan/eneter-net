﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpListenerProvider
    {
        public TcpListenerProvider(IPAddress ipAddress, int port, bool reuseAddressFlag)
            : this(new IPEndPoint(ipAddress, port), reuseAddressFlag)
        {
        }

        public TcpListenerProvider(IPEndPoint address, bool reuseAddressFlag)
        {
            using (EneterTrace.Entering())
            {
                myAddress = address;
                myReuseAddressFlag = reuseAddressFlag;
            }
        }

        public void StartListening(Action<TcpClient> connectionHandler)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    if (connectionHandler == null)
                    {
                        throw new ArgumentNullException("The input parameter connectionHandler is null.");
                    }

                    try
                    {
                        myStopListeningRequested = false;

                        myConnectionHandler = connectionHandler;

                        myListener = new TcpListener(myAddress);
                        myListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

#if !COMPACT_FRAMEWORK && !WINDOWS_PHONE80 && !WINDOWS_PHONE81
                        myListener.Server.LingerState = new LingerOption(true, 0);
#endif
                        myListener.Start();

                        // Listen in another thread
                        myListeningThread = new Thread(DoTcpListening);
                        myListeningThread.Start();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);

                        try
                        {
                            // Clear after failed start
                            StopListening();
                        }
                        catch
                        {
                            // We tried to clean after failure. The exception can be ignored.
                        }

                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    myStopListeningRequested = true;

                    if (myListener != null)
                    {
                        try
                        {
                            myListener.Stop();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.IncorrectlyStoppedListening, err);
                        }
                        myListener = null;
                    }

#if COMPACT_FRAMEWORK
                    if (myListeningThread != null)
#else
                    if (myListeningThread != null && myListeningThread.ThreadState != ThreadState.Unstarted)
#endif
                    {
                        if (!myListeningThread.Join(1000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToStopThreadId + myListeningThread.ManagedThreadId.ToString());

                            try
                            {
                                myListeningThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.FailedToAbortThread, err);
                            }
                        }
                    }
                    myListeningThread = null;

                    myConnectionHandler = null;
                }
            }
        }

        public bool IsListening
        { 
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myListeningManipulatorLock))
                    {
                        return myListener != null;
                    }
                }
            }
        }

        // Implementation of the listening loop for the Compact Framework platform.
        // Note: Socket in Compact Framework does not have BeginAcceptTcpClient() method.
        // Note: BeginAcceptTcpClient() hangs in stress tests. Using QueueUserWorkItem has the same performance and is stable.
        private void DoTcpListening()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Listening loop.
                    while (!myStopListeningRequested)
                    {
                        // Get the connected client.
                        try
                        {
                            TcpClient aTcpClient = myListener.AcceptTcpClient();

                            // Execute handler in another thread.
                            WaitCallback aHandler = x =>
                            {
                                try
                                {
                                    myConnectionHandler(aTcpClient);
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Error(TracedObject + ErrorHandler.ProcessingTcpConnectionFailure, err);
                                }

                                if (aTcpClient != null)
                                {
                                    aTcpClient.Close();
                                }
                            };
                            ThreadPool.QueueUserWorkItem(aHandler);
                        }
                        catch (Exception err)
                        {
                            // If the exception is not caused by closing the socket.
                            if (!myStopListeningRequested)
                            {
                                EneterTrace.Error(TracedObject + ErrorHandler.ProcessingTcpConnectionFailure, err);
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedInListeningLoop, err);
                }
            }
        }


        private IPEndPoint myAddress;
        private Action<TcpClient> myConnectionHandler;

        private bool myReuseAddressFlag;

        private TcpListener myListener;
        private Thread myListeningThread;
        private volatile bool myStopListeningRequested;

        private object myListeningManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}


#endif