using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HttpServer
{
    public class WebServerAsync
    {
        private static ManualResetEvent allDone = new ManualResetEvent(false);// Thread signal
        private static string root = null;

        // TCP Listener
        public static void StartListening(int port, string root, int maxPendingConns)
        {
            WebServerAsync.root = root;

            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(maxPendingConns);

                Console.WriteLine("Server is up and ready! localhost:" + port);

                while(true)
                {
                    // Non-signaled state
                    allDone.Reset();

                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    // Wait for connection
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        private static void AcceptCallback(IAsyncResult asyncResult)
        {
            // Signal main thread to proceed
            allDone.Set();

            Socket listener = (Socket)asyncResult.AsyncState;
            Socket handler = listener.EndAccept(asyncResult);

            StateObject state = new StateObject();
            state.clientSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        private static void ReadCallback(IAsyncResult asyncResult)
        {
            StateObject state = (StateObject)asyncResult.AsyncState;
            Socket handler = state.clientSocket;

            int bytesRead = handler.EndReceive(asyncResult);

            if (bytesRead > 0)
            {
                string httpHeaderReceived = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

                var httpFileRequest = httpHeaderReceived.Split(new[] {'\r', '\n'}).FirstOrDefault();
                var fileNameStartIndex = httpFileRequest.IndexOf("GET /") + "GET /".Length;
                var fileNameEndIndex = httpFileRequest.IndexOf("HTTP/") - 1;// Minus space
                var fileName = httpFileRequest.Substring(fileNameStartIndex, fileNameEndIndex - fileNameStartIndex);

                Console.WriteLine(httpFileRequest);
                Console.WriteLine("File requested: " + fileName);

                HttpRespond(handler, fileName);
            }
        }
        
        private static void HttpRespond(Socket handler, string fileName)
        {
            // Check if requested file exists
            if(File.Exists(root + @"\" + fileName))
            {
                // Header attribute
                string contentType = string.Empty;

                switch (Path.GetExtension(fileName))
                {  
                    case ".pdf": contentType = "application/pdf"; break;
                    case ".exe": contentType = "application/octet-stream"; break;
                    case ".zip": contentType = "application/zip"; break;
                    case ".doc": contentType = "application/msword"; break;
                    case ".xls": contentType = "application/vnd.ms-excel"; break;
                    case ".ppt": contentType = "application/vnd.ms-powerpoint"; break;
                    case ".gif": contentType = "image/gif"; break;
                    case ".png": contentType = "image/png"; break;
                    case ".jpeg":
                    case ".jpg": contentType = "image/jpg"; break;
                    case ".html": contentType = "text/html"; break;
                    
                    default: contentType = "application/force-download"; break;
                }

                byte[] bodyToSend = File.ReadAllBytes(root + @"\" + fileName);
                byte[] headerToSend = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\n" +
                                                              "Content-Type: " + contentType + "\n" +
                                                              "Content-Length: " + bodyToSend.Count() + "\n\n");

                byte[] contentToSend = new byte[bodyToSend.Length + headerToSend.Length];
                headerToSend.CopyTo(contentToSend, 0);
                bodyToSend.CopyTo(contentToSend, headerToSend.Length);

                Send(handler, contentToSend);
            }
            else if(Directory.Exists(root + @"\" + fileName))// Block existing dir requests
            {
                byte[] header = Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\n");
                Send(handler, header);
            }
            else// File requested does not exist
            {
                byte[] header = Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\n");
                Send(handler, header);
            }
        }
        
        private static void Send(Socket handler, byte[] byteData)
        {
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult asyncResult)
        {
            try
            { 
                Socket handler = (Socket)asyncResult.AsyncState;
 
                int bytesSent = handler.EndSend(asyncResult);
                Console.WriteLine("Sent " + bytesSent + " bytes to client.");

                handler.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
