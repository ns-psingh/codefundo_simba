"use strict";

// Use the Azure IoT device SDK for devices that connect to Azure IoT Central.
var clientFromConnectionString = require('azure-iot-device-mqtt').clientFromConnectionString;
var Message = require('azure-iot-device').Message;
var ConnectionString = require('azure-iot-device').ConnectionString;

var connectionString = 'HostName=saas-iothub-37185713-c25c-4400-b838-70716fe80ee9.azure-devices.net;DeviceId=442ee0c3-70d6-4e17-b253-d23dae2195d5;SharedAccessKey=0gtqZktOacmPmlvB6AY6xMUgMddjF9aX7ev6WHUYazY=';
var client = clientFromConnectionString(connectionString);
var targetTemperature=0;
var count=0;
// Send device measurements.
function sendTelemetry() {
if(count==1)
process.exit(0);
  //var latitude = 0 + (Math.random() * 15);
  //var longitude = 70 + (Math.random() * 10);
var latitude = process.argv[2];
var longitude=process.argv[3];  
var fanmode = 0;
  var data = JSON.stringify({ 
    latitude: latitude, 
    longitude: longitude,
    fanmode: "1",
    overheat: "ER 123"});
  var message = new Message(data);
  client.sendEvent(message, (err, res) => console.log(`Sent message: ${message.getData()}` +
    (err ? `; error: ${err.toString()}` : '') +
    (res ? `; status: ${res.constructor.name}` : '')));
	count=count+1;
console.log(count);
}

// Send device properties.
function sendDeviceProperties(twin) {
var properties = {
 serialNumber: '123-ABC',
 manufacturer: 'Contoso'
};
twin.properties.reported.update(properties, (err) => console.log(`Sent device properties; ` +
 (err ? `error: ${err.toString()}` : `status: success`)));
}

// Add any settings your device supports,
// mapped to a function that is called when the setting is changed.

// Handle settings changes that come from Azure IoT Central via the device twin.

// Handle device connection to Azure IoT Central.
var connectCallback = (err) => {
  if (err) {
    console.log(`Device could not connect to Azure IoT Central: ${err.toString()}`);
  } else {
    console.log('Device successfully connected to Azure IoT Central');

    // Send telemetry measurements to Azure IoT Central every 1 second.
    setInterval(sendTelemetry, 1000);

    // Get device twin from Azure IoT Central.
    client.getTwin((err, twin) => {
      if (err) {
        console.log(`Error getting device twin: ${err.toString()}`);
      } else {
        // Send device properties once on device start up.
        sendDeviceProperties(twin);
        // Apply device settings and handle changes to device settings.
        
      }
    });
  }
};

// Start the device (connect it to Azure IoT Central).
client.open(connectCallback);
