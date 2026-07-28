/**
 * A function that implements debounce functionality.
 * @param {Function} mainFunction - The function to be executed after the debounce period.
 * @param {number} delay - The time in milliseconds to wait before executing the mainFunction.
 * @returns {Function} - The debounced function.
 */
const debounce = (mainFunction, delay) => {
  let timer;
  return function (...args) {
    clearTimeout(timer);
    timer = setTimeout(() => {
      mainFunction(...args);
    }, delay);
  };
};

/**
 * Parses the setting values of the given parent element and returns them as a payload object.
 *
 * @param {HTMLElement} parent_element The parent element containing setting elements.
 * @return {Object} The payload object containing the parsed setting values.
 */
function parse_setting_values(parent_element) {
  payload = {};
  for (const element of parent_element.querySelectorAll("[data-setting]")) {
    switch (element.nodeName) {
      case "INPUT":
      case "SELECT":
        switch (element.type) {
          case "text":
          case "select-one":
            payload[element.dataset.setting] = element.value;
            break;
          case "checkbox":
            payload[element.dataset.setting] = element.checked;
            break;
          case "radio":
            if (element.checked)
              payload[element.dataset.setting] = element.value;
            break;
          default:
            console.log("SAVE UNKNOWN", element);
        }
        break;
    }
  }
  return payload;
}

function update_setting_fields(parent_element, payload) {
  for (const element of parent_element.querySelectorAll("[data-setting]")) {
    switch (element.nodeName) {
      case "INPUT":
      case "SELECT":
        switch (element.type) {
          case "text":
          case "select-one":
            element.value =
              payload[element.dataset.setting] ?? element.dataset.default ?? "";
            break;
          case "checkbox":
            element.checked =
              !!payload[element.dataset.setting] ??
              !!element.dataset.default ??
              false;
            break;
          case "radio":
            element.checked =
              (payload[element.dataset.setting] ??
                element.dataset.default ??
                "") == element.value;
            break;
          default:
            console.warn("Unknown element.type", element.type, element);
        }
        break;
      default:
        console.warn("Unknown element.nodeName", element.nodeName, element);
    }
  }
}

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
 */
function update_device_options(select, devices, fallback) {
  const selected = select.value || (fallback > 0 ? String(fallback) : "");
  const options = devices.map(
    (device) => new Option(device_option_label(device), device.vJoyIndex),
  );
  if (
    selected !== "" &&
    !devices.some((device) => String(device.vJoyIndex) === selected)
  ) {
    options.unshift(
      new Option(`${selected} (not configured in vJoy)`, selected),
    );
  }
  select.replaceChildren(...options);
  select.value = selected;
}

function register_settings_handler(parent_element, callback) {
  for (const element of parent_element.querySelectorAll("[data-setting]")) {
    switch (element.nodeName) {
      case "INPUT":
      case "SELECT":
        switch (element.type) {
          case "text":
          case "select-one":
          case "checkbox":
          case "radio":
            element.addEventListener("input", callback);
            break;
          default:
            console.warn("Unknown element.type", element.type, element);
        }
        break;
      default:
        console.warn("Unknown element.nodeName", element.nodeName, element);
    }
  }
}
