using System.Diagnostics;
using System.Net;
using System.Text;

namespace SistemskoProgramiranje
{
    public class WorkerPool
    {
        private readonly int workerCount;

        private readonly RequestQueue requestQueue;

        private readonly WordCounterService wordCounterService;

        private readonly SafeLogger logger;

        public WorkerPool(
            int workerCount,
            RequestQueue requestQueue,
            WordCounterService wordCounterService,
            SafeLogger logger)
        {
            this.workerCount = workerCount;
            this.requestQueue = requestQueue;
            this.wordCounterService = wordCounterService;
            this.logger = logger;
        }

        public void Start()
        {
            for (int i = 0; i < workerCount; i++)
            {
                int workerId = i + 1;

                ThreadPool.QueueUserWorkItem(
                    _ => WorkerLoop(workerId)
                );
            }

            logger.Log($"Pokrenut WorkerPool sa {workerCount} niti.");
        }

        private void WorkerLoop(int workerId)
        {
            logger.Log($"Worker {workerId} pokrenut.");

            while (true)
            {
                HttpListenerContext context =
                    requestQueue.Dequeue();

                Stopwatch stopwatch =
                    Stopwatch.StartNew();

                try
                {
                    logger.Log($"Worker {workerId} obradjuje zahtev.");

                    string response =
                        wordCounterService.ProcessRequest(
                            context.Request.RawUrl ?? ""
                        );

                    stopwatch.Stop();

                    response +=
                        $"\nVreme obrade: {stopwatch.ElapsedMilliseconds} ms";

                    SendResponse(context, response, 200);
                }
                catch (FileNotFoundException ex)
                {
                    SendResponse(context, ex.Message, 404);
                }
                catch (ArgumentException ex)
                {
                    SendResponse(context, ex.Message, 400);
                }
                catch (Exception)
                {
                    SendResponse(
                        context,
                        "Doslo je do greske na serveru.",
                        500
                    );
                }
            }
        }

        private void SendResponse(
            HttpListenerContext context,
            string text,
            int statusCode)
        {
            byte[] buffer =
                Encoding.UTF8.GetBytes(text);

            context.Response.StatusCode =
                statusCode;

            context.Response.ContentType =
                "text/plain; charset=utf-8";

            context.Response.ContentLength64 =
                buffer.Length;

            using Stream output =
                context.Response.OutputStream;

            output.Write(buffer, 0, buffer.Length);
        }
    }
}