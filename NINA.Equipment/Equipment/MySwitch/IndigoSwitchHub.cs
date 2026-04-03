#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;
using NINA.INDI;
using NINA.INDI.Interfaces;
using NINA.Indigo.Devices;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MySwitch {

    /// <summary>
    /// Equipment-layer wrapper for an INDIGO AUX device that exposes power ports,
    /// USB ports, dew-heater duty cycles, and other writable channels as a standard
    /// NINA <see cref="ISwitchHub"/>.
    /// </summary>
    public class IndigoSwitchHub : IndigoDevice<IINDISwitchHub>, ISwitchHub, IDisposable {

        public IndigoSwitchHub(INDIDeviceInfo info, IProfileService profileService = null) : base(info) {
            this.profileService = profileService;
            switches = new AsyncObservableCollection<ISwitch>();
        }

        private readonly IProfileService profileService;

        private ICollection<ISwitch> switches;
        public ICollection<ISwitch> Switches {
            get => switches;
            private set {
                switches = value;
                RaisePropertyChanged();
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblSwitchConnectionLost"];

        protected override IINDISwitchHub GetInstance() {
            return device ??= new NINA.Indigo.Devices.IndigoSwitchHub(_device);
        }

        protected override Task PreConnect() {
            if (profileService != null) {
                var s = profileService.ActiveProfile.SwitchSettings;
                GetInstance().ConfigureConnectionProperties(
                    s.IndiConnectionMode,
                    s.IndiAutoSearch,
                    s.IndiAddress,
                    s.IndiPort,
                    s.IndiBaudRate
                );
            }
            return Task.CompletedTask;
        }

        protected override async Task PostConnect() {
            await Task.Delay(TimeSpan.FromSeconds(1));
            BuildSwitchCollection();
            GetInstance().ValuesUpdated += OnIndigoValuesUpdated;
        }

        protected override void PostDisconnect() {
            GetInstance().ValuesUpdated -= OnIndigoValuesUpdated;
            Switches = new AsyncObservableCollection<ISwitch>();
        }

        private void OnIndigoValuesUpdated(string propertyName) {
            foreach (var sw in switches) {
                if (sw is IndiReadSwitchItem r && r.PropertyName == propertyName) {
                    r.Poll();
                } else if (sw is IndiWritableSwitchItem w && w.PropertyName == propertyName) {
                    w.SyncFromDevice();
                }
            }
        }

        private void BuildSwitchCollection() {
            var list = new AsyncObservableCollection<ISwitch>();
            short id = 0;
            foreach (var desc in device.GetDescriptors()) {
                list.Add(desc.IsWritable
                    ? (ISwitch)new IndiWritableSwitchItem(id++, desc, device)
                    : new IndiReadSwitchItem(id++, desc, device));
            }
            Switches = list;
            Logger.Info($"IndigoSwitchHub '{Name}': discovered {list.Count} switch channel(s)");
        }

        #region Unsupported

        public IList<string> SupportedActions { get; } = new List<string>();
        public string Action(string actionName, string actionParameters) => throw new NotImplementedException();
        public void CommandBlind(string command, bool raw = false) => throw new NotImplementedException();
        public bool CommandBool(string command, bool raw = false) => throw new NotImplementedException();
        public string CommandString(string command, bool raw = false) => throw new NotImplementedException();

        #endregion
    }
}
