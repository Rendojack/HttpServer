using System.Net.Sockets;

namespace HttpServer
{
    // For async client data reading
    public class StateObject
    { 
        public Socket clientSocket = null; 
        public const int bufferSize = 1024;
        public byte[] buffer = new byte[bufferSize];// Chunk received
    }
}
