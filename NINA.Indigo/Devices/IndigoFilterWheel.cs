#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI;
using NINA.INDI.Devices;

namespace NINA.Indigo.Devices {

    /// <summary>
    /// INDIGO filter wheel device. Uses IndigoClient for protocol communication.
    /// </summary>
    public class IndigoFilterWheel : INDIFilterWheel {
        public IndigoFilterWheel(INDIDeviceInfo info) : base(info) { }
        protected override INDIClient Client => IndigoClient.Instance;
    }
}
