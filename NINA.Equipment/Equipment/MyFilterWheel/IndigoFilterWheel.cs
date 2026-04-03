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
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;
using NINA.INDI;
using NINA.INDI.Interfaces;
using NINA.Indigo.Devices;
using NINA.Profile.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFilterWheel {

    internal class IndigoFilterWheel : IndigoDevice<IINDIFilterWheel>, IFilterWheel, IDisposable {

        public IndigoFilterWheel(INDIDeviceInfo info, IProfileService profileService) : base(info) {
            this.profileService = profileService;
        }

        public int[] FocusOffsets => GetProperty(nameof(IINDIFilterWheel.FocusOffsets), new int[] { });
        public string[] Names => GetProperty(nameof(IINDIFilterWheel.Names), new string[] { });

        public short Position {
            get => (short)(device?.Position ?? -1);
            set {
                if (device != null && ShouldBeConnected) {
                    device.Position = (int)value;
                    WaitForReadyState();
                    InvalidatePropertyCache();
                }
            }
        }

        private Task<bool> WaitForReadyState(CancellationToken ct = default) {
            do {
                CoreUtil.Wait(TimeSpan.FromMilliseconds(100), ct);
            } while (device.IsMoving);

            return Task.FromResult(true);
        }

        private IProfileService profileService;

        public AsyncObservableCollection<FilterInfo> Filters => profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;

        protected override string ConnectionLostMessage => Loc.Instance["LblFilterwheelConnectionLost"];

        protected override Task PreConnect() {
            if (profileService != null) {
                var settings = profileService.ActiveProfile.FilterWheelSettings;
                var instance = GetInstance();
                instance.ConfigureConnectionProperties(
                    settings.IndiConnectionMode,
                    settings.IndiAutoSearch,
                    settings.IndiAddress,
                    settings.IndiPort,
                    settings.IndiBaudRate
                );
            }
            return Task.CompletedTask;
        }

        protected override Task PostConnect() {
            var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;

            var duplicates = filtersList.GroupBy(x => x.Position).Where(x => x.Count() > 1).ToList();
            foreach (var group in duplicates) {
                foreach (var filterToRemove in group) {
                    Logger.Warning($"Duplicate filter position defined in filter list. Removing: {filterToRemove.Name}");
                    filtersList.Remove(filterToRemove);
                }
            }

            if (filtersList.Count > 0) {
                var existingPositions = filtersList.Select(x => (int)x.Position).ToList();
                var missingPositions = Enumerable.Range(0, existingPositions.Max()).Except(existingPositions);
                foreach (var position in missingPositions) {
                    if (device.Names.Length > position) {
                        var offset = device.FocusOffsets.Length > position ? device.FocusOffsets[position] : 0;
                        var filterToAdd = new FilterInfo(device.Names[position], offset, (short)position);
                        Logger.Warning($"Missing filter position. Importing filter: {filterToAdd.Name}");
                        filtersList.Insert(position, filterToAdd);
                    }
                }
            }

            int profileFilters = filtersList.Count;
            var deviceFilters = device.Names.Length;

            if (profileFilters < deviceFilters) {
                for (int i = profileFilters; i < deviceFilters; i++) {
                    var offset = device.FocusOffsets.Length > i ? device.FocusOffsets[i] : 0;
                    var filter = new FilterInfo(device.Names[i], offset, (short)i);
                    Logger.Info($"Importing INDIGO filter: {filter.Name}");
                    filtersList.Add(filter);
                }
            } else if (profileFilters > deviceFilters) {
                for (int i = profileFilters - 1; i >= deviceFilters; i--) {
                    var filterToRemove = filtersList[i];
                    Logger.Warning($"Too many filters defined. Removing: {filterToRemove.Name}");
                    filtersList.Remove(filterToRemove);
                }
            }
            profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters = filtersList;
            return Task.CompletedTask;
        }

        protected override IINDIFilterWheel GetInstance() {
            return device ??= new NINA.Indigo.Devices.IndigoFilterWheel(_device);
        }
    }
}
