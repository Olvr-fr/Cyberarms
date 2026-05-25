using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics;
using Cyberarms.IntrusionDetection.Api.Plugin;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection {
    internal class WindowsLogManager {
        private DateTime lastSearchDate;
        
        // public override event AttackDetectedHandler AttackDetected;

        private EventLog eventLogCyberarms = null;


        private static WindowsLogManager _instance;
        internal static WindowsLogManager Instance {
            get {
                if (_instance == null) {
                    _instance = new WindowsLogManager();
                    _instance.lastSearchDate = DateTime.Now;
                }
                return _instance;
            }
        }

        
        internal void WriteEntry(string text, EventLogEntryType type, int eventId, short category) {
            try {
                if (eventLogCyberarms == null)
                    eventLogCyberarms = new EventLog(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME, ".", Globals.CYBERARMS_WINDOWS_EVENT_SOURCE);
                eventLogCyberarms.WriteEntry(text, type, eventId, category);
            } catch { }
        }

        internal void WriteEntry(string text) {
            WriteEntry(text, EventLogEntryType.Information, 0, 0);
        }

        
        
        /// <summary>
        /// Keep it private to avoid multiple instances
        /// </summary>
        private WindowsLogManager() {
            
        }



    }
}
