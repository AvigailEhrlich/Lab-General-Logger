using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace InstrumentC_Processor
{
    internal class Logger
    {
        #region AVIGAIL 01/08/24

        private static ThreadLocal<string> logPath = new ThreadLocal<string>(() => string.Empty);
        private static ThreadLocal<bool> isLoggerDefined = new ThreadLocal<bool>(() => false);
        private static ThreadLocal<bool> isInfoLoggingEnabled = new ThreadLocal<bool>(() => false);
        private static ThreadLocal<string> fullLogPath = new ThreadLocal<string>(() => string.Empty);

        internal static void DefineLogger()
        {
            if (isLoggerDefined.Value) return;

            try
            {
                // Try to load configuration from the main application's config file
                Configuration cfg = null;
                string configFilePath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

                try
                {
                    ExeConfigurationFileMap map = new ExeConfigurationFileMap
                    {
                        ExeConfigFilename = configFilePath
                    };
                    cfg = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                }
                catch
                {
                    // Handle the case where the main application's config file cannot be loaded
                }

                // If main application's config file was not loaded, try loading the config file for the current application (patholab_common config)
                if (cfg == null || cfg.AppSettings.Settings.Count == 0)
                {
                    string assemblyPath = Assembly.GetExecutingAssembly().Location;
                    try
                    {
                        ExeConfigurationFileMap map = new ExeConfigurationFileMap
                        {
                            ExeConfigFilename = assemblyPath + ".config"
                        };
                        cfg = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                    }
                    catch
                    {
                        // Handle the case where the current assembly's config file cannot be loaded
                    }
                }

                // If still no configuration, use default settings
                var appSettings = cfg?.AppSettings ?? new AppSettingsSection();
                string logPathFromConfig = appSettings.Settings["LogPath"]?.Value ?? string.Empty;
                isInfoLoggingEnabled.Value = appSettings.Settings["EnableInfoLogFlag"]?.Value != "F";

                // Define default log path
                string defaultLogFolder = @"C:\temp\";
                string safeUserName = Environment.UserName.MakeSafeFilename('_');
                string logFolderPath = string.IsNullOrEmpty(logPathFromConfig) ? Path.Combine(defaultLogFolder, safeUserName) : Path.Combine(logPathFromConfig, safeUserName);

                // Define log file name with current date
                string logFileName = $"Log-{DateTime.Now:dd-MM-yyyy}.txt";

                // Set the full log path
                fullLogPath.Value = Path.Combine(logFolderPath, logFileName);
                logPath.Value = fullLogPath.Value; // Initialize logPath for use in WriteToLog

                // If logging is enabled, ensure the directory exists
                if (!Directory.Exists(logFolderPath))
                {
                    Directory.CreateDirectory(logFolderPath);
                }

                isLoggerDefined.Value = true;
            }
            catch (Exception ex)
            {
                // Handle the exception, such as logging it
                Console.WriteLine($"Error in DefineLogger method: {ex.Message}");
            }
        }

        internal static string CallingMethodDetails()
        {
            string methodName = "UnknownMethod";
            string className = "UnknownClass";
            string namespaceName = "UnknownNamespace";

            try
            {
                var stackTrace = new StackTrace();
                var frame = stackTrace.GetFrame(3);
                var callingMethod = frame?.GetMethod();
                methodName = callingMethod?.Name ?? "UnknownMethod";
                className = callingMethod?.DeclaringType?.Name ?? "UnknownClass";
                namespaceName = callingMethod?.DeclaringType?.Namespace ?? "UnknownNamespace";
            }
            catch (Exception)
            {
                // Ignore exceptions from stack trace retrieval
            }
            return $"{DateTime.Now} [{namespaceName}.{className}.{methodName}]";
        }

        public static void WriteInfoToLog(string strLog)
        {
            DefineLogger();
            if (!isInfoLoggingEnabled.Value) return;

            WriteLog(strLog);
        }

        public static void WriteExceptionToLog(string strLog)
        {
            DefineLogger();
            WriteLog(strLog);
        }

        private static void WriteLog(string strLog)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(logPath.Value, true))
                {
                    sw.WriteLine(CallingMethodDetails()); // Log method details
                    sw.WriteLine($"{strLog}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                // Consider logging this exception to a file or other medium
                Console.WriteLine($"Error writing to log: {ex.Message}");
            }
        }

        #endregion
    }

    public static class StringExtensionMethods
    {
        public static XmlReader ToXmlReader(this string value)
        {
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                IgnoreWhitespace = true,
                IgnoreComments = true
            };
            var xmlReader = XmlReader.Create(new StringReader(value), settings);
            xmlReader.Read();
            return xmlReader;
        }

        public static string MakeSafeFilename(this string filename, char replaceChar)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, replaceChar);
            }
            return filename;
        }

        public static string Compress(this string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            MemoryStream ms = new MemoryStream();
            using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }

            ms.Position = 0;
            MemoryStream outStream = new MemoryStream();

            byte[] compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return Convert.ToBase64String(gzBuffer);
        }

        public static string Decompress(this string compressedText)
        {
            byte[] gzBuffer = Convert.FromBase64String(compressedText);
            using (MemoryStream ms = new MemoryStream())
            {
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 4, (gzBuffer.Length - 4));

                byte[] buffer = new byte[msgLength];

                ms.Position = 0;
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    zip.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }

        public static byte[] ToBiteArray(this string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(this byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}
