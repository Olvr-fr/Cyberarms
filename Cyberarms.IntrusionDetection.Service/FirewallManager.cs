using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetFwTypeLib;


namespace Cyberarms.IntrusionDetection {
    internal class FirewallManager {
        private static FirewallManager _instance;
        private dynamic firewallManager;
        internal static FirewallManager Instance {
            get {
                if (_instance == null) {
                    _instance = new FirewallManager();
                }
                return _instance;

            }
        }

        private FirewallManager() {
            Type fwType = Type.GetTypeFromProgID("HNetCfg.FwMgr");
            if (fwType != null)
                firewallManager = Activator.CreateInstance(fwType);
        }

        internal void AddPort(string strName,
                                   int Port,
                                   NetFwTypeLib.NET_FW_SCOPE_ Scope,
                                   NetFwTypeLib.NET_FW_IP_PROTOCOL_ Protocol, 
                                   string remoteAddresses) {
            dynamic fireWallPort =
                          Activator.CreateInstance(
                               Type.GetTypeFromProgID("HNetCfg.FWOpenPort"));
            fireWallPort.RemoteAddresses = remoteAddresses;
            fireWallPort.Enabled = true;
            fireWallPort.Name = strName;
            fireWallPort.Port = Port;
            fireWallPort.Protocol = Protocol;

            firewallManager.LocalPolicy.CurrentProfile
                                       .GloballyOpenPorts.Add(fireWallPort);
        }

        

        internal void RemovePort(int Port,
                                      NetFwTypeLib.NET_FW_IP_PROTOCOL_ Protocol) {
            firewallManager.LocalPolicy.CurrentProfile
               .GloballyOpenPorts.Remove(Port, Protocol);
        }

        internal void AddAuthorizedApplication(string strName,
                                                string processImageFileName,
                                                NetFwTypeLib.NET_FW_SCOPE_ Scope) {
            dynamic authorizedApplication
                  = Activator.CreateInstance(Type.GetTypeFromProgID(
                                    "HNetCfg.FwAuthorizedApplication"));
            authorizedApplication.Name = strName;
            authorizedApplication.Scope = Scope;
            authorizedApplication.Enabled = true;
            authorizedApplication.ProcessImageFileName = processImageFileName;
            firewallManager.LocalPolicy.CurrentProfile
                           .AuthorizedApplications.Add(authorizedApplication);
        }

        internal void RemoveAuthorizedApplication(string processFileName) {
            firewallManager.LocalPolicy.CurrentProfile
                           .AuthorizedApplications.Remove(processFileName);
        }

        internal dynamic ReadPort(string name) {
            dynamic ports = firewallManager.LocalPolicy.CurrentProfile.GloballyOpenPorts;
            foreach (dynamic port in ports) {
                System.Diagnostics.Debug.Print(port.Name);
                if (port.Name == name) return port;
            }
            return null;

        }

    }
}
