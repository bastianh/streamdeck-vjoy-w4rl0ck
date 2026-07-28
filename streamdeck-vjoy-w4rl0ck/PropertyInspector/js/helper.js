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
