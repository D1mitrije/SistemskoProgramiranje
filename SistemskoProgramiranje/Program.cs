using System.Net;

namespace SistemskoProgramiranje
{
    internal class Program
    {
        private const string SERVER_URL = "http://localhost:5050/";
        private const int WORKER_COUNT = 4;
        private const int CACHE_EXPIRATION_SECONDS = 60;

        static void Main(string[] args)
        {
            string projectFolder = Directory.GetCurrentDirectory();

            string rootFolder = Path.Combine(projectFolder, "Root");
            string logsFolder = Path.Combine(projectFolder, "Logs");

            Directory.CreateDirectory(rootFolder);
            Directory.CreateDirectory(logsFolder);

            SafeLogger logger =
                new SafeLogger(Path.Combine(logsFolder, "server.log"));

            RequestQueue requestQueue =
                new RequestQueue(logger);

            ExpiringCache cache =
                new ExpiringCache(
                    TimeSpan.FromSeconds(CACHE_EXPIRATION_SECONDS),
                    logger
                );

            WordCounterService wordCounterService =
                new WordCounterService(
                    rootFolder,
                    cache,
                    logger
                );

            WorkerPool workerPool =
                new WorkerPool(
                    WORKER_COUNT,
                    requestQueue,
                    wordCounterService,
                    logger
                );

            workerPool.Start();

            HttpListener listener = new HttpListener();

            listener.Prefixes.Add(SERVER_URL);

            listener.Start();


            Console.WriteLine("Brojanje slova");
            Console.WriteLine($"Server URL: {SERVER_URL}");
            Console.WriteLine($"Root folder: {rootFolder}");
            Console.WriteLine($"Broj worker niti: {WORKER_COUNT}");
            Console.WriteLine($"Vreme trajanja kesa: {CACHE_EXPIRATION_SECONDS} sekundi");
            Console.WriteLine("Primer poziva:");
            Console.WriteLine("http://localhost:5050/fajl.txt");
          

            logger.Log("Server je pokrenut.");

            while (true)
            {
                try
                {
                    HttpListenerContext context =
                        listener.GetContext();

                    logger.Log($"Primljen zahtev: {context.Request.RawUrl}");

                    requestQueue.Enqueue(context);
                }
                catch (Exception ex)
                {
                    logger.Log($"Greska: {ex.Message}");
                }
            }
        }
    }
}