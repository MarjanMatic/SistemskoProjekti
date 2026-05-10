using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Projekat1
{
    internal class Server
    {
        private readonly HttpListener listener;
        public Server(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
                throw new PlatformNotSupportedException("Http listener is not supported on this system");

            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            listener = new HttpListener();
            foreach(string prefix in prefixes)
                listener.Prefixes.Add(prefix);
        }

        public void Start() {
            if (listener.IsListening)
                throw new InvalidOperationException("Server has already been started");

            listener.Start();
            Console.WriteLine("Listening");

            while (listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    RequestHandler.Enqueue(context);
                }
                catch (Exception ex)
                {
                    if (!listener.IsListening)
                        break;

                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public void Stop()
        {
            listener.Stop();
        }
    }
}
