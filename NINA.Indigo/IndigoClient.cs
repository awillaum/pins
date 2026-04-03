#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.INDI;
using NINA.INDI.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Indigo {

    /// <summary>
    /// Client for the INDIGO astronomy protocol (https://github.com/indigo-astronomy/indigo).
    /// INDIGO is fully compatible with the INDI XML-over-TCP protocol, so this client reuses
    /// the INDI protocol parsing layer from <see cref="INDIClient"/> and only overrides the
    /// server management to start <c>indigo_server</c> instead of <c>indiserver</c>.
    /// </summary>
    public class IndigoClient : INDIClient {

        // Default INDIGO port. Use 7625 to allow INDI (7624) and INDIGO to run simultaneously.
        public const int DefaultPort = 7625;

        private static IndigoClient _instance;
        private static readonly object _instanceLock = new();

        /// <summary>
        /// Thread-safe singleton. Uses <see cref="DefaultPort"/> by default.
        /// </summary>
        public new static IndigoClient Instance {
            get {
                if (_instance == null) {
                    lock (_instanceLock) {
                        if (_instance == null) {
                            _instance = new IndigoClient(DefaultPort);
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Creates an IndigoClient on the specified port.
        /// Server startup is handled in the background after construction.
        /// </summary>
        public IndigoClient(int port) : base(port, false) {
            Task.Run(async () => await StartIndigoServerAsync());
        }

        protected override async Task StartServerInFifoMode() {
            // Delegate to the indigo_server startup (called via base if needed)
            await StartIndigoServerAsync();
        }

        private async Task StartIndigoServerAsync() {
            try {
                KillExistingIndigoServer();

                var startInfo = new ProcessStartInfo {
                    FileName = "indigo_server",
                    Arguments = $"-p {_port}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process process = null;
                try {
                    process = Process.Start(startInfo);
                } catch (Exception ex) {
                    Logger.Warning($"Could not start indigo_server: {ex.Message}. Trying to connect to an already-running server.");
                }

                if (process != null) {
                    Logger.Info($"INDIGO server started with PID {process.Id}, port: {_port}");
                } else {
                    Logger.Info($"Attempting to connect to an existing INDIGO server on port {_port}");
                }

                // Attempt to connect with retries
                bool connected = false;
                for (int attempt = 1; attempt <= 10; attempt++) {
                    await Task.Delay(150 * attempt);

                    if (await Connect()) {
                        connected = true;
                        Logger.Info($"Connected to INDIGO server on attempt {attempt}");
                        break;
                    }
                }

                if (!connected) {
                    Logger.Error("Failed to connect to INDIGO server after multiple attempts");
                    _serverReadyTcs.TrySetResult(false);
                    return;
                }

                _serverReadyTcs.TrySetResult(true);
            } catch (Exception ex) {
                Logger.Error($"Error starting INDIGO server: {ex.Message}");
                _serverReadyTcs.TrySetResult(false);
            }
        }

        private static void KillExistingIndigoServer() {
            try {
                var startInfo = new ProcessStartInfo {
                    FileName = "pkill",
                    Arguments = "-9 indigo_server",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var p = Process.Start(startInfo);
                p?.WaitForExit(2000);
                Logger.Debug("Killed any existing indigo_server processes");
            } catch (Exception ex) {
                Logger.Debug($"pkill indigo_server: {ex.Message}");
            }
        }

        /// <summary>
        /// For INDIGO, drivers are pre-loaded in the server configuration.
        /// This override enumerates all devices already announced by the running
        /// indigo_server and optionally filters by driver executable name.
        /// No FIFO-based driver loading is performed.
        /// </summary>
        public override async Task<IReadOnlyList<INDIDeviceInfo>> GetDevices(DeviceInterface deviceInterface, string driver, CancellationToken ct = default) {
            await _serverReadyTcs.Task;

            // Refresh device discovery
            GetProperties(string.Empty);
            await Task.Delay(TimeSpan.FromMilliseconds(700), ct);

            var devices = new Dictionary<string, INDIDeviceInfo>();
            foreach (var dev in _discoveredDevices) {
                if ((dev.Value.Interface & deviceInterface) == 0) continue;

                // If a specific driver is requested (not "None"/empty) filter by driver executable
                if (!string.IsNullOrEmpty(driver) && driver != "None") {
                    if (!string.Equals(dev.Value.Driver, driver, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!devices.ContainsKey(dev.Key)) {
                    Logger.Info($"INDIGO found device {dev.Key}");
                    devices.Add(dev.Key, dev.Value);
                }
            }

            return devices.Values.ToList();
        }

        public override string GetServerVersionString() {
            return IsConnected ? "indigo_server (connected)" : "indigo_server (unknown)";
        }

        public override Version GetServerPlatformVersion() {
            return new Version(0, 0, 0, 0);
        }
    }
}
