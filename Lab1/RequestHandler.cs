using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Projekat1
{

    internal static class RequestHandler
    {
        private static readonly HttpClient client = new HttpClient();
        private static string token;
        private static string user;

        private static readonly Queue<HttpListenerContext> requestQueue = new Queue<HttpListenerContext>();
        private static readonly object lockQueue = new object();

        private static readonly Cache cache = new Cache();
        private static readonly object lockCache = new object();

        public static void LoadToken()
        {
            IConfigurationRoot secrets = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();


            if (secrets["github_token"] == null)
                throw new Exception("You need to set github token");

            if (secrets["user-agent"] == null)
                throw new Exception("You need to set user-agent");

            token = secrets["github_token"];
            user = secrets["user-agent"];
        }
        public static void SetMaxConcurrentCount(int count)
        {
            if (ThreadPool.SetMaxThreads(count, count) == false)
                throw new Exception("Couldn't set max threads");
        }
        public static void Enqueue(HttpListenerContext context)
        {
            lock (lockQueue)
            {
                requestQueue.Enqueue(context);
            }

            ThreadPool.QueueUserWorkItem(handleRequest);
        }

        private static void handleRequest(object? state)
        {
            HttpListenerContext context;
            lock (lockQueue)
            {
                context = requestQueue.Dequeue();
            }

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (!CheckRequest(request, response))
                return;

            string repo = request.QueryString["repo"];
            JObject data = cache.get(repo);
            if (data != null)
            {
                //Cache hit
                Console.WriteLine("Cache Hit on " + repo);
                Respond(response, HttpStatusCode.OK, data.ToString());
                return;
            }

            //Cache miss
            lock (lockCache)
            {
                // Check again, maybe it was updated
                data = cache.get(repo);
                if (data != null)
                {
                    Console.WriteLine("Cache hit on " + repo + " prevented stampede");
                    Respond(response, HttpStatusCode.OK, data.ToString());
                    return;
                }
                Console.WriteLine("Cache miss on " + repo);
                try
                {
                    JArray gitResponse = GithubRequest(repo);
                    int totalCommits = 0;
                    data = new JObject();
                    foreach (var commiter in gitResponse)
                    {
                        var author = (string)commiter["author"]["login"];
                        var commits = (int)commiter["total"];
                        totalCommits += commits;

                        data.Add(author, commits);
                    }
                    data.Add("total", totalCommits);

                    cache.set(repo, data);
                    Console.WriteLine("Updating cache " + repo);
                }
                catch (HttpRequestException e)
                {
                    if (e.StatusCode == HttpStatusCode.NotFound)
                    {
                        Respond(response, HttpStatusCode.NotFound, "Repo doesn't exist");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Respond(response, HttpStatusCode.InternalServerError);
                    return;
                }

            }

            Respond(response, HttpStatusCode.OK, data.ToString());
            return;
        }

        private static JArray GithubRequest(string repo)
        {
            string url = "https://api.github.com/repos/" + repo + "/stats/contributors";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("User-Agent", user);
            request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");

            using var response = client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            string content = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(content);
            if (content.Equals("{}"))
                return new JArray();

            JArray json = JArray.Parse(content);
            return json;
        }
        private static bool CheckRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!request.HttpMethod.Equals("GET"))
            {
                Respond(response, HttpStatusCode.BadRequest, "Invalid request");
                return false;
            }

            if (request.Url != null && !request.Url.AbsolutePath.Equals("/"))
            {
                Respond(response, HttpStatusCode.NotFound);
                return false;
            }

            string? repo = request.QueryString["repo"];
            if (request.QueryString.Count != 1 ||
                request.QueryString.GetKey(0) != "repo" ||
                string.IsNullOrWhiteSpace(repo))
            {
                Respond(response, HttpStatusCode.BadRequest, "Invalid arguments");
                return false;
            }

            if (!Regex.IsMatch(repo, ".+/.+"))
            {
                Respond(response, HttpStatusCode.BadRequest, "Repo should be in format owner/repo");
                return false;
            }

            return true;
        }

        private static void Respond(HttpListenerResponse response, HttpStatusCode statusCode, string? msg = null)
        {
            response.StatusCode = (int)statusCode;
            Stream output = response.OutputStream;

            if (!string.IsNullOrWhiteSpace(msg))
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(msg);
                response.ContentLength64 = buffer.Length;
                output.Write(buffer, 0, buffer.Length);
            }
            output.Close();
        }
    }
}
