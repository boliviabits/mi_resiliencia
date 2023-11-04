namespace MiResiliencia.Helpers
{
    public class ThreadSafeFileWriter
    {
        public string ReadFile(string filePathAndName)
        {
            // This block will be protected area
            using (var mutex = new Mutex(false, filePathAndName.Replace("\\", "").Replace("/", "")))
            {
                var hasHandle = false;
                try
                {
                    // Wait for the muted to be available
                    hasHandle = mutex.WaitOne(Timeout.Infinite, false);
                    // Do the file read
                    if (!File.Exists(filePathAndName))
                        return string.Empty;
                    return File.ReadAllText(filePathAndName);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    // Very important! Release the mutex
                    // Or the code will be locked forever
                    if (hasHandle)
                        mutex.ReleaseMutex();
                }
            }
        }

        public void WriteFile(string filePathAndName, string fileContents)
        {
            using (var mutex = new Mutex(false, filePathAndName.Replace("\\", "").Replace("/","")))
            {
                var hasHandle = false;
                try
                {
                    hasHandle = mutex.WaitOne(Timeout.Infinite, false);
                    File.AppendAllText(filePathAndName, fileContents);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (hasHandle)
                        mutex.ReleaseMutex();
                }
            }
        }
    }
}
