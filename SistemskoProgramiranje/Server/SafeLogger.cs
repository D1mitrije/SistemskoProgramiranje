namespace SistemskoProgramiranje
{
    public class SafeLogger
    {
        private readonly string filePath;

        private readonly object logLock =
            new object();

        public SafeLogger(string filePath)
        {
            this.filePath = filePath;
        }

        public void Log(string message)
        {
            string line =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] " +
                $"[Thread {Thread.CurrentThread.ManagedThreadId}] " +
                message;

            lock (logLock)
            {
                Console.WriteLine(line);

                File.AppendAllText(
                    filePath,
                    line + Environment.NewLine
                );
            }
        }
    }
}