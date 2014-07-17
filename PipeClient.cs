using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Xml.Serialization;
//using System.Runtime.Serialization;
//using System.Runtime.Serialization.Formatters.Binary;

namespace QRC.ICS.Service
{
    public class PipeClient
    {
        /// <summary>
        /// Sends the specified command.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd">The command.</param>
        /// <param name="PipeName">Name of the pipe.</param>
        /// <param name="TimeOut">The time out.</param>
        /// <exception cref="System.TimeoutException">Could not connect to the server within the specified timeout period.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">timeout is less than 0 and not set to System.Threading.Timeout.Infinite.</exception>
        /// <exception cref="System.InvalidOperationException">The client is already connected.</exception>
        /// <exception cref="System.IO.IOException">The server is connected to another client and the time-out period has expired.</exception>
        /// <exception cref="System.InvalidOperationException">An error occurred during serialization. The original exception is available using the System.Exception.InnerException property.</exception>
        public void Send<T>(T cmd, string PipeName, int TimeOut = 1000)
        {
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous))
            {
                pipeStream.Connect(TimeOut);
                new XmlSerializer(typeof(T)).Serialize(pipeStream, cmd);
            }
        }

        /// <summary>
        /// Receive the specified command.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd">The command.</param>
        /// <param name="PipeName">Name of the pipe.</param>
        /// <param name="TimeOut">The time out.</param>
        /// <exception cref="System.TimeoutException">Could not connect to the server within the specified timeout period.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">timeout is less than 0 and not set to System.Threading.Timeout.Infinite.</exception>
        /// <exception cref="System.InvalidOperationException">The client is already connected.</exception>
        /// <exception cref="System.IO.IOException">The server is connected to another client and the time-out period has expired.</exception>
        /// <exception cref="System.InvalidOperationException">An error occurred during serialization. The original exception is available using the System.Exception.InnerException property.</exception>
        public T Receive<T>(string PipeName, int TimeOut = 1000)
        {
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName, PipeDirection.In, PipeOptions.Asynchronous))
            {
                pipeStream.Connect(TimeOut);
                T cmd = default(T);
                using (StreamReader reader = new StreamReader(pipeStream))
                {
                    // Wait for object to be placed on stream by client then read it   
                    string msg = reader.ReadToEnd();
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
    }
}
