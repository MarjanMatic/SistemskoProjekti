using Projekat1;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Configuration;

RequestHandler.LoadToken();
RequestHandler.SetMaxConcurrentCount(20);
var server = new Server(["http://localhost:8080/"]);

new Thread(stopOnKeypress).Start();
server.Start();

void stopOnKeypress()
{
    Console.ReadKey(intercept: true);
    server.Stop();
}