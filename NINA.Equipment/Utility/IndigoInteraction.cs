#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Indigo;
using NINA.INDI.Enums;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Equipment.Utility {

    /// <summary>
    /// Provides discovery of devices available through the INDIGO protocol
    /// (https://github.com/indigo-astronomy/indigo).
    /// Works alongside <see cref="INDIInteraction"/> so that both INDI and INDIGO
    /// devices are presented to the user at the same time.
    /// </summary>
    public class IndigoInteraction(IProfileService profileService) {
        private readonly IProfileService profileService = profileService;

        public List<ICamera> GetCameras(IExposureDataFactory exposureDataFactory) {
            return new List<ICamera>();
        }

        public async Task<List<IFocuser>> GetFocusers() {
            var l = new List<IFocuser>();
            if (!await IndigoClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDIGO server not ready - skipping INDIGO focuser enumeration");
                return l;
            }

            string driver = profileService.ActiveProfile.FocuserSettings.IndigoDriver;

            foreach (var device in await IndigoClient.Instance.GetDevices(DeviceInterface.FOCUSER_INTERFACE, driver)) {
                l.Add(new IndigoFocuser(device, profileService));
            }
            return l;
        }

        public async Task<List<ITelescope>> GetTelescopes() {
            var l = new List<ITelescope>();
            if (!await IndigoClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDIGO server not ready - skipping INDIGO telescope enumeration");
                return l;
            }

            string driver = profileService.ActiveProfile.TelescopeSettings.IndigoDriver;

            foreach (var device in await IndigoClient.Instance.GetDevices(DeviceInterface.TELESCOPE_INTERFACE, driver)) {
                l.Add(new IndigoTelescope(device, profileService));
            }
            return l;
        }

        public async Task<List<IRotator>> GetRotators() {
            var l = new List<IRotator>();
            if (!await IndigoClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDIGO server not ready - skipping INDIGO rotator enumeration");
                return l;
            }

            string driver = profileService.ActiveProfile.RotatorSettings.IndigoDriver;

            foreach (var device in await IndigoClient.Instance.GetDevices(DeviceInterface.ROTATOR_INTERFACE, driver)) {
                l.Add(new IndigoRotator(device, profileService));
            }
            return l;
        }

        public async Task<List<IFilterWheel>> GetFilterWheels() {
            var l = new List<IFilterWheel>();
            if (!await IndigoClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDIGO server not ready - skipping INDIGO filterwheel enumeration");
                return l;
            }

            string driver = profileService.ActiveProfile.FilterWheelSettings.IndigoDriver;

            foreach (var device in await IndigoClient.Instance.GetDevices(DeviceInterface.FILTER_INTERFACE, driver)) {
                l.Add(new IndigoFilterWheel(device, profileService));
            }
            return l;
        }

        public async Task<List<IFlatDevice>> GetFlatDevices() {
            var l = new List<IFlatDevice>();
            if (!await IndigoClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDIGO server not ready - skipping INDIGO flat device enumeration");
                return l;
            }

            string driver = profileService.ActiveProfile.FlatDeviceSettings.IndigoDriver;

            foreach (var device in await IndigoClient.Instance.GetDevices(DeviceInterface.LIGHTBOX_INTERFACE, driver)) {
                l.Add(new IndigoFlatDevice(device, profileService));
            }
            return l;
        }

        public async Task<List<IWeatherData>> GetWeatherData() {
            var l = new List<IWeatherData>();
            if (!await IndigoClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDIGO server not ready - skipping INDIGO weather data enumeration");
                return l;
            }

            string driver = profileService.ActiveProfile.WeatherDataSettings.IndigoDriver;

            foreach (var device in await IndigoClient.Instance.GetDevices(DeviceInterface.WEATHER_INTERFACE, driver)) {
                l.Add(new IndigoWeatherData(device, profileService));
            }
            return l;
        }

        public async Task<List<ISwitchHub>> GetSwitches() {
            var l = new List<ISwitchHub>();
            if (!await IndigoClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDIGO server not ready - skipping INDIGO switch hub enumeration");
                return l;
            }

            string driver = profileService.ActiveProfile.SwitchSettings.IndigoDriver;

            foreach (var device in await IndigoClient.Instance.GetDevices(DeviceInterface.AUX_INTERFACE, driver)) {
                l.Add(new IndigoSwitchHub(device, profileService));
            }
            return l;
        }

        public static string GetVersion() {
            return IndigoClient.Instance.GetServerVersionString();
        }

        public static Version GetPlatformVersion() {
            return IndigoClient.Instance.GetServerPlatformVersion();
        }
    }
}
