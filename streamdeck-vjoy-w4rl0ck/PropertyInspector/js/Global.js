function receivedGlobalSettings(settings) {
  console.log("LOADING", settings);
  payload = { ...settings };
  for (let i = 0; i < 9; i++) {
    payload["axis" + i] = settings.axis[i];
  }
  update_setting_fields(document, payload);
}

function saveGlobalSettings() {
  const values = parse_setting_values(document);
  payload = { vjoy: parseInt(values.vjoy), axis: [] };
  for (let i = 0; i < 8; i++) {
    payload.axis.push(parseInt(values["axis" + i]));
  }
  console.log("SAVING", payload);
  window.opener.setGlobalSettings(payload);
}

window.onload = () => {
  //document.querySelector("#send").addEventListener("click", () => {
  //  window.opener.sendToInspector("Message from external window.");
  //});

  window.addEventListener("message", function (event) {
    if (event.data.event === "didReceiveGlobalSettings") {
      receivedGlobalSettings(event.data.payload.settings);
    }
  });
  const saveSettings = debounce(saveGlobalSettings, 250);

  register_settings_handler(document, saveSettings);

  window.opener.requestGlobalSettings();
};
