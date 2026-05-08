using System.Net;

namespace SistemskoProgramiranje
{
    public class RequestQueue
    {
        private readonly Queue<HttpListenerContext> queue =
            new Queue<HttpListenerContext>();

        private readonly object queueLock = new object();

        private readonly SafeLogger logger;

        public RequestQueue(SafeLogger logger)
        {
            this.logger = logger;
        }

        public void Enqueue(HttpListenerContext context)
        {
            lock (queueLock)
            {
                queue.Enqueue(context);

                logger.Log($"Zahtev dodat u red. Trenutno: {queue.Count}");

                Monitor.Pulse(queueLock);
            }
        }

        public HttpListenerContext Dequeue()
        {
            lock (queueLock)
            {
                while (queue.Count == 0)
                {
                    Monitor.Wait(queueLock);
                }

                HttpListenerContext context =
                    queue.Dequeue();

                logger.Log($"Zahtev uzet iz reda. Preostalo: {queue.Count}");

                return context;
            }
        }
    }
}