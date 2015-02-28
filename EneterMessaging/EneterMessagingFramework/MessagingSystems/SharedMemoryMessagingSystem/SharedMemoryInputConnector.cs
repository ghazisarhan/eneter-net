﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if NET4 || NET45

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    internal class SharedMemoryInputConnector : IInputConnector
    {
        public SharedMemoryInputConnector(string inputConnectorAddress, IProtocolFormatter protocolFormatter, TimeSpan sendResponseTimeout, int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
                myMaxMessageSize = maxMessageSize;
                mySendResponseTimeout = sendResponseTimeout;
                mySecurity = memoryMappedFileSecurity;

                // Note: openTimeout is not used if SharedMemoryReceiver creates memory mapped file.
                TimeSpan aDummyOpenTimout = TimeSpan.Zero;
                myReceiver = new SharedMemoryReceiver(inputConnectorAddress, false, aDummyOpenTimout, myMaxMessageSize, mySecurity);
            }
        }

        public void StartListening(Action<MessageContext> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                if (messageHandler == null)
                {
                    throw new ArgumentNullException("messageHandler is null.");
                }

                lock (myListenerManipulatorLock)
                {
                    try
                    {
                        myRequestMessageHandler = messageHandler;
                        myReceiver.StartListening(HandleRequestMessage);
                    }
                    catch
                    {
                        StopListening();
                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListenerManipulatorLock)
                {
                    myReceiver.StopListening();
                    myRequestMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListenerManipulatorLock)
                {
                    return myReceiver.IsListening;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                SharedMemorySender aClientSender;
                lock (myConnectedClients)
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientSender);
                }

                if (aClientSender == null)
                {
                    throw new InvalidOperationException("The connection with client '" + outputConnectorAddress + "' is not open.");
                }

                aClientSender.SendMessage(x => myProtocolFormatter.EncodeMessage(outputConnectorAddress, message, x));
            }
        }

        // When service disconnects a client.
        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                SharedMemorySender aClientSender;
                lock (myConnectedClients)
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientSender);
                    if (aClientSender != null)
                    {
                        myConnectedClients.Remove(outputConnectorAddress);
                    }
                }

                if (aClientSender != null)
                {
                    try
                    {
                        aClientSender.SendMessage(x => myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress, x));
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning("failed to send the close message.", err);
                    }

                    aClientSender.Dispose();
                }
            }
        }

        private void HandleRequestMessage(Stream message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(message);

                if (aProtocolMessage != null)
                {
                    // If it is open connection then add the new connected client.
                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            lock (myConnectedClients)
                            {
                                if (!myConnectedClients.ContainsKey(aProtocolMessage.ResponseReceiverId))
                                {
                                    TimeSpan aDummyOpenTimeout = TimeSpan.Zero;
                                    SharedMemorySender aClientSender = new SharedMemorySender(aProtocolMessage.ResponseReceiverId, false, aDummyOpenTimeout, mySendResponseTimeout, myMaxMessageSize, mySecurity);

                                    myConnectedClients[aProtocolMessage.ResponseReceiverId] = aClientSender;
                                }
                                else
                                {
                                    EneterTrace.Warning(TracedObject + "could not open connection for client '" + aProtocolMessage.ResponseReceiverId + "' because the client with same id is already connected.");
                                }
                            }
                        }
                        else
                        {
                            EneterTrace.Warning(TracedObject + "could not connect a client because response recevier id was not available in open connection message.");
                        }
                    }
                    // When a client closes connection with the service.
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            lock (myConnectedClients)
                            {
                                SharedMemorySender aClientSender;
                                myConnectedClients.TryGetValue(aProtocolMessage.ResponseReceiverId, out aClientSender);

                                if (aClientSender != null)
                                {
                                    myConnectedClients.Remove(aProtocolMessage.ResponseReceiverId);

                                    aClientSender.Dispose();
                                }
                            }
                        }
                    }

                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                    try
                    {
                        myRequestMessageHandler(aMessageContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private IProtocolFormatter myProtocolFormatter;
        private int myMaxMessageSize;
        private MemoryMappedFileSecurity mySecurity;

        private TimeSpan mySendResponseTimeout;

        private object myListenerManipulatorLock = new object();
        private SharedMemoryReceiver myReceiver;

        private Action<MessageContext> myRequestMessageHandler;

        private Dictionary<string, SharedMemorySender> myConnectedClients = new Dictionary<string, SharedMemorySender>();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif