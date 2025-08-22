namespace PIWebAPIApp.Utilities
{
    public static class Logger
    {
        private static readonly object _lock = new object();

        public static void Info(string message)
        {
            Log("INFO", message);
        }

        public static void Error(string message)
        {
            Log("ERROR", message);
        }

        public static void Debug(string message)
        {
#if DEBUG
            Log("DEBUG", message);
#endif
        }

        public static void Warn(string message)
        {
            Log("WARN", message);
        }

        private static void Log(string level, string message)
        {
            lock (_lock)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] [{level}] {message}";
                
                Console.WriteLine(logMessage);
                
                // Опционально: запись в файл
                // File.AppendAllText("app.log", logMessage + Environment.NewLine);
            }
        }
    }
}