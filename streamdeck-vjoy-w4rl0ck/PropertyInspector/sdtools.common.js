// ****************************************************************
// * EasyPI v1.4
// * Author: BarRaider
// *
// * JS library to simplify the communication between the
// * Stream Deck's Property Inspector and the plugin.
// *
// * Project page: https://github.com/BarRaider/streamdeck-easypi
// * Support: http://discord.barraider.com
// *
// * Initially forked from Elgato's common.js file
// ****************************************************************

var websocket = null,
  uuid = null,
  registerEventName = null,
  actionInfo = {},
  inInfo = {},
  runningApps = [],
  isQT = navigator.appVersion.includes("QtWebEngine");

function showElementsByClassName(className) {
  var elements = document.getElementsByClassName(className);
  for (var i = 0; i < elements.length; i++) {
    elements[i].style.display = "block";
  }
}

function requestGlobalSettings() {
  if (websocket && websocket.readyState === 1) {
    console.log("requestGlobalSettings");
    websocket.send(
      JSON.stringify({
        event: "getGlobalSettings",
        context: uuid,
      }),
    );
  } else {
    console.error(
      "requestGlobalSettings wrong websocket state",
      websocket.readyState,
    );
  }
}

function setGlobalSettings(payload) {
  if (websocket && websocket.readyState === 1) {
    console.log("setGlobalSettings", payload);
    websocket.send(
      JSON.stringify({
        event: "setGlobalSettings",
        context: uuid,
        payload,
      }),
    );
  } else {
    console.error(
      "setGlobalSettings wrong websocket state",
      websocket.readyState,
    );
  }
}

function connectElgatoStreamDeckSocket(
  inPort,
  inUUID,
  inRegisterEvent,
  inInfo,
  inActionInfo,
) {
  uuid = inUUID;
  registerEventName = inRegisterEvent;
  console.log("connectElgatoStreamDeckSocket", inUUID, inActionInfo);
  actionInfo = JSON.parse(inActionInfo); // cache the info
  inInfo = JSON.parse(inInfo);
  websocket = new WebSocket("ws://127.0.0.1:" + inPort);

  addDynamicStyles(inInfo.colors);

  websocket.onopen = websocketOnOpen;
  websocket.onmessage = websocketOnMessage;

  // Allow others to get notified that the websocket is created
  document.dispatchEvent(new Event("websocketCreate"));

  loadConfiguration(actionInfo.payload.settings);
  initPropertyInspector();

  if (actionInfo?.payload?.controller === "Keypad") {
    showElementsByClassName("keypadOnly");
  } else if (actionInfo?.payload?.controller === "Encoder") {
    showElementsByClassName("encoderOnly");
  }
}

function websocketOnOpen() {
  var json = {
    event: registerEventName,
    uuid: uuid,
  };
  websocket.send(JSON.stringify(json));

  // Notify the plugin that we are connected
  sendValueToPlugin("propertyInspectorConnected", "property_inspector");
}

function websocketOnMessage(evt) {
  // Received message from Stream Deck
  var jsonObj = JSON.parse(evt.data);

  if (jsonObj.event === "didReceiveSettings") {
    var payload = jsonObj.payload;
    console.log("didReceiveSettings");
    loadConfiguration(payload.settings);
  }
}

function loadConfiguration(payload) {
  // console.log('loadConfiguration', payload);
  for (const element of document.querySelectorAll("[data-setting]")) {
    switch (element.nodeName) {
      case "INPUT":
      case "SELECT":
        switch (element.type) {
          case "text":
          case "select-one":
            element.value =
              payload[element.dataset.setting] ?? element.dataset.default ?? "";
            delete payload[element.dataset.setting];
            break;
          case "checkbox":
            element.checked =
              !!payload[element.dataset.setting] ??
              !!element.dataset.default ??
              false;
            delete payload[element.dataset.setting];
            break;
        }
        break;
    }
  }

  for (var key in payload) {
    try {
      var elem = document.getElementById(key);
      if (elem.classList.contains("sdCheckbox")) {
        // Checkbox
        elem.checked = payload[key];
      } else if (elem.classList.contains("sdFile")) {
        // File
        var elemFile = document.getElementById(elem.id + "Filename");
        elemFile.innerText = payload[key];
        if (!elemFile.innerText) {
          elemFile.innerText = "No file...";
        }
      } else if (elem.classList.contains("sdList")) {
        // Dynamic dropdown
        var textProperty = elem.getAttribute("sdListTextProperty");
        var valueProperty = elem.getAttribute("sdListValueProperty");
        var valueField = elem.getAttribute("sdValueField");

        var items = payload[key];
        elem.options.length = 0;

        for (var idx = 0; idx < items.length; idx++) {
          var opt = document.createElement("option");
          opt.value = items[idx][valueProperty];
          opt.text = items[idx][textProperty];
          elem.appendChild(opt);
        }
        elem.value = payload[valueField];
      } else if (elem.classList.contains("sdHTML")) {
        // HTML element
        elem.innerHTML = payload[key];
      } else {
        // Normal value
        elem.value = payload[key];
      }
      // console.log("Load", key, payload[key]);
    } catch (err) {
      console.warn("loadConfiguration failed for key", key, err);
    }
  }
  document.dispatchEvent(new Event("configurationLoaded"));
}

function originalSetSettings() {
  const payload = {};
  const elements = document.getElementsByClassName("sdProperty");

  Array.prototype.forEach.call(elements, function (elem) {
    var key = elem.id;
    if (elem.classList.contains("sdCheckbox")) {
      // Checkbox
      payload[key] = elem.checked;
    } else if (elem.classList.contains("sdFile")) {
      // File
      var elemFile = document.getElementById(elem.id + "Filename");
      payload[key] = elem.value;
      if (!elem.value) {
        // Fetch innerText if file is empty (happens when we lose and regain focus to this key)
        payload[key] = elemFile.innerText;
      } else {
        // Set value on initial file selection
        elemFile.innerText = elem.value;
      }
    } else if (elem.classList.contains("sdList")) {
      // Dynamic dropdown
      const valueField = elem.getAttribute("sdValueField");
      payload[valueField] = elem.value;
    } else if (elem.classList.contains("sdHTML")) {
      // HTML element
      const valueField = elem.getAttribute("sdValueField");
      payload[valueField] = elem.innerHTML;
    } else {
      // Normal value
      payload[key] = elem.value;
    }
    // console.log("Save: " + key + "<=" + payload[key]);
  });

  for (const element of document.querySelectorAll("[data-setting]")) {
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
        }
        break;
    }
  }

  console.log("originalSetSettings", payload);
  setSettingsToPlugin(payload);
}

const debounce = (mainFunction, delay) => {
  // Declare a variable called 'timer' to store the timer ID
  let timer;

  // Return an anonymous function that takes in any number of arguments
  return function (...args) {
    // Clear the previous timer to prevent the execution of 'mainFunction'
    clearTimeout(timer);

    // Set a new timer that will execute 'mainFunction' after the specified delay
    timer = setTimeout(() => {
      mainFunction(...args);
    }, delay);
  };
};
const setSettings = debounce(originalSetSettings, 250);

function setSettingsToPlugin(payload) {
  if (websocket && websocket.readyState === 1) {
    const json = {
      event: "setSettings",
      context: uuid,
      payload: payload,
    };
    websocket.send(JSON.stringify(json));

    console.log("setSettingsToPlugin", payload);

    document.dispatchEvent(new Event("settingsUpdated"));
  }
}

// Sends an entire payload to the sendToPlugin method
function sendPayloadToPlugin(payload) {
  if (websocket && websocket.readyState === 1) {
    const json = {
      action: actionInfo["action"],
      event: "sendToPlugin",
      context: uuid,
      payload: payload,
    };
    console.log("sendPayloadToPlugin", payload);
    websocket.send(JSON.stringify(json));
  }
}

// Sends one value to the sendToPlugin method
function sendValueToPlugin(value, param) {
  if (websocket && websocket.readyState === 1) {
    const json = {
      action: actionInfo["action"],
      event: "sendToPlugin",
      context: uuid,
      payload: {
        [param]: value,
      },
    };
    console.log("sendValueToPlugin", value, param);
    websocket.send(JSON.stringify(json));
  }
}

function openWebsite() {
  if (websocket && websocket.readyState === 1) {
    const json = {
      event: "openUrl",
      payload: {
        url: "https://BarRaider.com",
      },
    };
    websocket.send(JSON.stringify(json));
  }
}

if (!isQT) {
  document.addEventListener("DOMContentLoaded", function () {
    initPropertyInspector();
  });
}

window.addEventListener("beforeunload", function (e) {
  e.preventDefault();

  // Notify the plugin we are about to leave
  sendValueToPlugin("propertyInspectorWillDisappear", "property_inspector");

  // Don't set a returnValue to the event, otherwise Chromium with throw an error.
});

function prepareDOMElements(baseElement) {
  baseElement = baseElement || document;

  /**
   * You could add a 'label' to a textares, e.g. to show the number of charactes already typed
   * or contained in the textarea. This helper updates this label for you.
   */
  baseElement.querySelectorAll("textarea").forEach((e) => {
    const maxl = e.getAttribute("maxlength");
    e.targets = baseElement.querySelectorAll(`[for='${e.id}']`);
    if (e.targets.length) {
      let fn = () => {
        for (let x of e.targets) {
          x.textContent = maxl
            ? `${e.value.length}/${maxl}`
            : `${e.value.length}`;
        }
      };
      fn();
      e.onkeyup = fn;
    }
  });
}

function initPropertyInspector() {
  // Place to add functions
  prepareDOMElements(document);
}

function addDynamicStyles(clrs) {
  const node =
    document.getElementById("#sdpi-dynamic-styles") ||
    document.createElement("style");
  if (!clrs.mouseDownColor)
    clrs.mouseDownColor = fadeColor(clrs.highlightColor, -100);
  const clr = clrs.highlightColor.slice(0, 7);
  const clr1 = fadeColor(clr, 100);
  const clr2 = fadeColor(clr, 60);
  const metersActiveColor = fadeColor(clr, -60);

  console.log("addDynamicStyles", clrs);

  node.setAttribute("id", "sdpi-dynamic-styles");
  node.innerHTML = `

    input[type="radio"]:checked + label span,
    input[type="checkbox"]:checked + label span {
        background-color: ${clrs.highlightColor};
    }

    input[type="radio"]:active:checked + label span,
    input[type="radio"]:active + label span,
    input[type="checkbox"]:active:checked + label span,
    input[type="checkbox"]:active + label span {
      background-color: ${clrs.mouseDownColor};
    }

    input[type="radio"]:active + label span,
    input[type="checkbox"]:active + label span {
      background-color: ${clrs.buttonPressedBorderColor};
    }

    td.selected,
    td.selected:hover,
    li.selected:hover,
    li.selected {
      color: white;
      background-color: ${clrs.highlightColor};
    }

    .sdpi-file-label > label:active,
    .sdpi-file-label.file:active,
    label.sdpi-file-label:active,
    label.sdpi-file-info:active,
    input[type="file"]::-webkit-file-upload-button:active,
    button:active {
      background-color: ${clrs.buttonPressedBackgroundColor};
      color: ${clrs.buttonPressedTextColor};
      border-color: ${clrs.buttonPressedBorderColor};
    }

    ::-webkit-progress-value,
    meter::-webkit-meter-optimum-value {
        background: linear-gradient(${clr2}, ${clr1} 20%, ${clr} 45%, ${clr} 55%, ${clr2})
    }

    ::-webkit-progress-value:active,
    meter::-webkit-meter-optimum-value:active {
        background: linear-gradient(${clr}, ${clr2} 20%, ${metersActiveColor} 45%, ${metersActiveColor} 55%, ${clr})
    }
    `;
  document.body.appendChild(node);
}

/** UTILITIES */

/*
    Quick utility to lighten or darken a color (doesn't take color-drifting, etc. into account)
    Usage:
    fadeColor('#061261', 100); // will lighten the color
    fadeColor('#200867'), -100); // will darken the color
*/
function fadeColor(col, amt) {
  const min = Math.min,
    max = Math.max;
  const num = parseInt(col.replace(/#/g, ""), 16);
  const r = min(255, max((num >> 16) + amt, 0));
  const g = min(255, max((num & 0x0000ff) + amt, 0));
  const b = min(255, max(((num >> 8) & 0x00ff) + amt, 0));
  return "#" + (g | (b << 8) | (r << 16)).toString(16).padStart(6, 0);
}
