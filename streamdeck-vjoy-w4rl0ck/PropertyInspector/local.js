let childWindows = [];
let lastPluginPayload = null;
let selectedDevice = null;

function sendFromPlugin(payload) {
  console.log("sendFromPlugin payload", payload);
  lastPluginPayload = payload;
  document.getElementById("status").innerHTML =
    `Device #${payload.device}: (${payload.status})`;
  update_device_selector();
}

// loadConfiguration cannot fill the device selector: its options are only known
// once the plugin has sent its device list, which happens later. The saved value
// is remembered here and reapplied whenever the list arrives.
function update_device_selector() {
  const select = document.querySelector('[data-setting="device"]');
  if (select === null || lastPluginPayload === null) return;
  const defaultDevice = lastPluginPayload.global?.vjoy;
  update_device_options(
    select,
    lastPluginPayload.devices ?? [],
    selectedDevice,
    [
      {
        label: defaultDevice > 0 ? `Default (device ${defaultDevice})` : "Default",
        value: "0",
      },
    ],
  );
}

// A child window opens long after the plugin pushed its data, so it asks for a
// replay once its own message handler is in place.
function requestPluginData() {
  if (lastPluginPayload === null) return;
  const message = {
    event: "sendToPropertyInspector",
    payload: lastPluginPayload,
  };
  childWindows.forEach((child) => child.postMessage(message, "*"));
}

function setup_elements() {
  for (const element of document.querySelectorAll("[data-setting]")) {
    console.log("E", element, element.nodeName, element.type);
    switch (element.nodeName) {
      case "INPUT":
      case "SELECT":
        switch (element.type) {
          case "text":
          case "select-one":
          case "checkbox":
            element.addEventListener("input", setSettings);
            break;
          default:
            console.warn("Unknown element.type", element.type, element);
        }
        break;
      default:
        console.warn("Unknown element.nodeName", element.nodeName, element);
    }
  }

  for (const controlElement of document.querySelectorAll("[data-open]")) {
    controlElement.addEventListener("click", function () {
      const target = controlElement.getAttribute("data-open");
      childWindows.push(window.open(target));
    });
  }

  for (const controlElement of document.querySelectorAll("[data-visible]")) {
    const hideElement = document.getElementById(
      controlElement.getAttribute("data-visible"),
    );
    hideElement.style.display = controlElement.checked ? "block" : "none";
    document.addEventListener("configurationLoaded", function () {
      hideElement.style.display = controlElement.checked ? "block" : "none";
    });
    controlElement.addEventListener("change", function () {
      hideElement.style.display = controlElement.checked ? "block" : "none";
    });
  }
}

document.addEventListener("websocketCreate", function () {
  selectedDevice = actionInfo?.payload?.settings?.device ?? null;
  websocket.addEventListener("message", function (evt) {
    var jsonObj = JSON.parse(evt.data);
    if (jsonObj.event === "sendToPropertyInspector") {
      var payload = jsonObj.payload;
      // console.log("PAYLOAD", payload);
      sendFromPlugin(payload);
    } else if (jsonObj.event === "didReceiveSettings") {
      // Runs after EasyPI has already cleared the selection it could not apply.
      selectedDevice = jsonObj.payload?.settings?.device ?? selectedDevice;
      update_device_selector();
    }
    childWindows.forEach((child) => {
      let pm = child.postMessage(jsonObj, "*");
      console.log("pm", pm);
    });
  });
  setup_elements();
});
