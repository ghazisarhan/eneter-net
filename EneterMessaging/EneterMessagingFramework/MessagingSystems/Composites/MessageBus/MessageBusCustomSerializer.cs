﻿

using System;
using System.IO;
using System.Text;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    internal class MessageBusCustomSerializer : ISerializer
    {
        public object Serialize<T>(T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                if (typeof(T) != typeof(MessageBusMessage))
                {
                    throw new InvalidOperationException("Only " + typeof(MessageBusMessage).Name + " can be serialized.");
                }

                object aTemp = dataToSerialize;
                MessageBusMessage aMessage = (MessageBusMessage)aTemp;

                using (MemoryStream aStream = new MemoryStream())
                {
                    BinaryWriter aWriter = new BinaryWriter(aStream);

                    // Write messagebus request.
                    byte aRequestType = (byte)aMessage.Request;
                    aWriter.Write((byte)aRequestType);

                    // Write Id.
                    myEncoderDecoder.WritePlainString(aWriter, aMessage.Id, Encoding.UTF8, myIsLittleEndian);

                    // Write message data.
                    if (aMessage.Request == EMessageBusRequest.SendRequestMessage ||
                        aMessage.Request == EMessageBusRequest.SendResponseMessage)
                    {
                        if (aMessage.MessageData == null)
                        {
                            throw new InvalidOperationException("Message data is null.");
                        }

                        myEncoderDecoder.Write(aWriter, aMessage.MessageData, myIsLittleEndian);
                    }

                    return aStream.ToArray();
                }
            }
        }

        public T Deserialize<T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                if (serializedData is byte[] == false)
                {
                    throw new ArgumentException("Input parameter 'serializedData' is not byte[].");
                }

                if (typeof(T) != typeof(MessageBusMessage))
                {
                    throw new InvalidOperationException("Data can be deserialized only into" + typeof(MessageBusMessage).Name);
                }

                MessageBusMessage aResult;

                byte[] aData = (byte[])serializedData;

                using (MemoryStream aStream = new MemoryStream(aData))
                {
                    BinaryReader aReader = new BinaryReader(aStream);

                    // Read message bus request.
                    int aRequest = aReader.ReadByte();
                    EMessageBusRequest aMessageBusRequest = (EMessageBusRequest)aRequest;

                    // Read Id
                    string anId = myEncoderDecoder.ReadPlainString(aReader, Encoding.UTF8, myIsLittleEndian);

                    // Read message data.
                    object aMessageData = null;
                    if (aMessageBusRequest == EMessageBusRequest.SendRequestMessage ||
                        aMessageBusRequest == EMessageBusRequest.SendResponseMessage)
                    {
                        aMessageData = myEncoderDecoder.Read(aReader, myIsLittleEndian);
                    }


                    aResult = new MessageBusMessage(aMessageBusRequest, anId, aMessageData);
                    return (T)(object)aResult;
                }
            }
        }


        private bool myIsLittleEndian = true;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();
    }
}
