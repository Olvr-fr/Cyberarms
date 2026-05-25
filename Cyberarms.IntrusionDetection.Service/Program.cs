using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Cyberarms.IntrusionDetection {
    static class Program {
        private const string CrashLog = @"C:\ProgramData\Cyberarms\crash.log";

        static void Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            System.Windows.Forms.Application.ThreadException += Application_ThreadException;

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
			{
				new Service()
			};
            try {
                ServiceBase.Run(ServicesToRun);
            } catch (Exception ex) {
                WriteCrashLog("ServiceBase.Run", ex);
                try { System.Diagnostics.EventLog.WriteEntry("Cyberarms Intrusion Detection Service", ex.Message); } catch { }
            }
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
            Exception ex = e.ExceptionObject as Exception;
            WriteCrashLog("AppDomain.UnhandledException", ex);
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e) {
            WriteCrashLog("Application.ThreadException", e.Exception);
            try { System.Diagnostics.EventLog.WriteEntry("Cyberarms Intrusion Detection Service Base", e.Exception.Message, System.Diagnostics.EventLogEntryType.Error); } catch { }
        }

        internal static void WriteCrashLog(string source, Exception ex) {
            try {
                string dir = Path.GetDirectoryName(CrashLog);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string entry = String.Format(
                    "[{0}] SOURCE: {1}\r\nMESSAGE: {2}\r\nSTACK TRACE:\r\n{3}\r\n{4}\r\n",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    source,
                    ex?.Message ?? "(null exception)",
                    ex?.StackTrace ?? "(no stack trace)",
                    new string('-', 80));
                File.AppendAllText(CrashLog, entry);
            } catch { }
        }
    }
}
