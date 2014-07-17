using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Xml.Serialization;

namespace QRC.ICS.Service
{
    public class PipeServer
    {
        public async Task<T> Listen<T>(string PipeName)
        {
            // Create NamedPipeServerStream
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                // Wait for a Connection from a client
                await Task.Factory.FromAsync(pipeServer.BeginWaitForConnection, pipeServer.EndWaitForConnection, null);

                T cmd = default(T);
                using (StreamReader reader = new StreamReader(pipeServer))
                {
                    // Wait for object to be placed on stream by client then read it   
                    string msg = await reader.ReadToEndAsync();
                    using (TextReader objReader = new StringReader(msg))
                    {
                        // Deserialize object from stream
                        var xs = new XmlSerializer(typeof(T));
                        cmd = (T)xs.Deserialize(objReader);
                    }
                }

                return cmd;
            }
        }

        //public async Task Send<T>(T cmd, string PipeName, int TimeOut = 1000)
        public void Send<T>(T cmd, string PipeName, int TimeOut = 1000)
        {
            // Create NamedPipeServerStream
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                // Wait for a Connection from a client
                var asyncResult = pipeServer.BeginWaitForConnection(null, null);

                if (asyncResult.AsyncWaitHandle.WaitOne(TimeOut))
                {
                    pipeServer.EndWaitForConnection(asyncResult);
                }
                else
                    throw new TimeoutException();

                new XmlSerializer(typeof(T)).Serialize(pipeServer, cmd);
            }
        }
    }
}

