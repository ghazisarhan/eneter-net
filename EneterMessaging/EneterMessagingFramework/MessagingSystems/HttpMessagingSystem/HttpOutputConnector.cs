﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.IO;
using System.Net;
using System.Threading;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal class HttpOutputConnector : IOutputConnector
    {
        public HttpOutputConnector(string httpServiceConnectorAddress, string responseReceiverId, int pollingFrequencyMiliseconds, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // just check if the channel id is a valid Uri
                    myUri = new Uri(httpServiceConnectorAddress);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(httpServiceConnectorAddress + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                myResponseReceiverId = responseReceiverId;
                myPollingFrequencyMiliseconds = pollingFrequencyMiliseconds;
                myProtocolFormatter = protocolFormatter;
            }
        }

        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myStopReceivingRequestedFlag = false;

                    myResponseMessageHandler = responseMessageHandler;

                    myStopPollingWaitingEvent.Reset();
                    myResponseReceiverThread = new Thread(DoPolling);
                    myResponseReceiverThread.Start();

                    // Wait until thread listening to response messages is running.
                    if (!myListeningToResponsesStartedEvent.WaitOne(1000))
                    {
                        EneterTrace.Warning(TracedObject + "failed to start the thread listening to response messages within 1 second.");
                    }

                    // Send open connection message.
                    object anEncodedMessage = myProtocolFormatter.EncodeOpenConnectionMessage(myResponseReceiverId);
                    SendMessage(anEncodedMessage);
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    // Send close connection message.
                    try
                    {
                        object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(myResponseReceiverId);
                        SendMessage(anEncodedMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to send close connection message.", err);
                    }

                    myStopReceivingRequestedFlag = true;
                    myStopPollingWaitingEvent.Set();

                    if (myResponseReceiverThread != null && Thread.CurrentThread.ManagedThreadId != myResponseReceiverThread.ManagedThreadId)
                    {
#if COMPACT_FRAMEWORK
                    //if (myResponseReceiverThread != null)
#else
                        if (myResponseReceiverThread.ThreadState != ThreadState.Unstarted)
#endif
                        {
                            if (!myResponseReceiverThread.Join(3000))
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myResponseReceiverThread.ManagedThreadId);

                                try
                                {
                                    myResponseReceiverThread.Abort();
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                                }
                            }
                        }
                    }
                    myResponseReceiverThread = null;
                    myResponseMessageHandler = null;
                }
            }
        }

        public bool IsConnected { get { return myIsListeningToResponses; } }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeMessage(myResponseReceiverId, message);
                    SendMessage(anEncodedMessage);
                }
            }
        }

        private void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                byte[] aMessage = (byte[])message;
#if !SILVERLIGHT
                HttpRequestInvoker.InvokePostRequest(myUri, aMessage);
#else
                // In case of Silverlight the request must be sent outside the main Silverlight thread.
                HttpRequestInvoker.InvokeNotInSilverlightThread(() => HttpRequestInvoker.InvokePostRequest(myUri, aMessage));
#endif
            }
        }


        private void DoPolling()
        {
            using (EneterTrace.Entering())
            {
                myIsListeningToResponses = true;
                myListeningToResponsesStartedEvent.Set();

                try
                {
                    // Create URI for polling.
                    string aParameters = "?id=" + myResponseReceiverId;
                    Uri aPollingUri = new Uri(myUri, aParameters);

                    while (!myStopReceivingRequestedFlag)
                    {
                        myStopPollingWaitingEvent.WaitOne(myPollingFrequencyMiliseconds);

                        if (!myStopReceivingRequestedFlag)
                        {
                            // Send poll request to get messages from the service.
                            WebResponse anHttpResponse = HttpRequestInvoker.InvokeGetRequest(aPollingUri);

                            // Convert the response to the fast memory stream
                            using (MemoryStream aBufferedResponse = new MemoryStream())
                            {
                                Stream aResponseStream = anHttpResponse.GetResponseStream();

                                // Write the incoming response to the fast memory stream.
                                StreamUtil.ReadToEnd(aResponseStream, aBufferedResponse);
                                aBufferedResponse.Position = 0;

                                // The response can contain more messages.
                                while (!myStopReceivingRequestedFlag && aBufferedResponse.Position < aBufferedResponse.Length)
                                {
                                    ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)aBufferedResponse);
                                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, myUri.Host, null);

                                    if (!myResponseMessageHandler(aMessageContext))
                                    {
                                        EneterTrace.Warning(TracedObject + "failed to process all response messages.");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (WebException err)
                {
                    string anErrorStatus = err.Status.ToString();
                    EneterTrace.Error(TracedObject + "detected an error during listening to response messages. Error status: '" + anErrorStatus + "'. ", err);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.DoListeningFailure, err);
                }

                myIsListeningToResponses = false;
                myListeningToResponsesStartedEvent.Reset();
            }
        }


        private Uri myUri;
        private string myResponseReceiverId;
        private IProtocolFormatter myProtocolFormatter;
        private Thread myResponseReceiverThread;
        private volatile bool myIsListeningToResponses;
        private volatile bool myStopReceivingRequestedFlag;
        private ManualResetEvent myListeningToResponsesStartedEvent = new ManualResetEvent(false);
        private Func<MessageContext, bool> myResponseMessageHandler;

        private int myPollingFrequencyMiliseconds;
        private ManualResetEvent myStopPollingWaitingEvent = new ManualResetEvent(false);
        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
