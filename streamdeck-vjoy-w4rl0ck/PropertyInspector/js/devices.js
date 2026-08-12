/**
 * Builds the label of a device option out of a device entry sent by the plugin.
 *
 * @param {Object} device An entry of the plugin's device list.
 * @return {string} The text shown for that device.
 */
function device_option_label(device) {
  switch (device.vJoyStatus) {
    case "VJD_STAT_OWN":
      return `${device.vJoyIndex} (in use by this plugin)`;
    case "VJD_STAT_BUSY":
      return `${device.vJoyIndex} (in use by another application)`;
    default:
      return String(device.vJoyIndex);
  }
}

/**
 * Rebuilds a vJoy device selector from the device list sent by the plugin,
 * keeping the current selection. A selected device vJoy no longer reports is
 * kept as an option of its own, so a saved setting never silently disappears.
 *
 * @param {HTMLSelectElement} select The device selector to rebuild.
 * @param {Array} devices The plugin's device list: {vJoyIndex, vJoyStatus}.
 * @param {number} [fallback] Selection to apply while the selector is empty.
 * @param {Array} [leading] Options to keep in front of the devices:
 *   {label, value}.
 */
function update_device_options(select, devices, fallback, leading = []) {
  const selected = select.value || (fallback > 0 ? String(fallback) : "");
  const options = leading
    .map((option) => new Option(option.label, option.value))
    .concat(
      devices.map(
        (device) => new Option(device_option_label(device), device.vJoyIndex),
      ),
    );
  if (selected !== "" && !options.some((option) => option.value === selected)) {
    options.splice(
      leading.length,
      0,
      new Option(`${selected} (not configured in vJoy)`, selected),
    );
  }
  select.replaceChildren(...options);
  select.value = selected;
}
