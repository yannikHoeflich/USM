# USM Protocol
**VERSION NAME** `1.0.1`
**VERSION ID** `2`

## Version Changes

### Breaking Changes
#### General
- update to support multiple Devices on one endpoint, with adding the id in every request
#### Scanning
- gets a unique ID for the request too
- returns device ids at the scan response

## Overview
The USM Protocol is the protocol that is used by all USM Devices. It is designed to always have a controller and clients. It supports multiple controllers actually is designed to have and don't need to have a single controller like other smart home services. It runs over the local network, mainly WiFi but you can also use a LAN connection.

## General Information
NAME|VALUE
----|-----
PORT|4210
BASE PROTOCOL|UDP

**General Request**
NAME|SIZE|DESCRIPTION
----|----|-----------
DEVICE_ID|32 Bit|The Id of the device that the request is targeting **(In scan request is it 0)**
REQUEST_PACKAGE_ID|8 Bit|The ID of the request

## Package types
The Package Types is defined in every package by the first byte.
Some package need a response but this is always defined in the package type.

### 0 - Response
If a package begins with a 0 it is a response to a request the receiver did.

**Response**
NAME|SIZE|DESCRIPTION
----|----|-----------
DEVICE_ID|32 Bit|The Id of the device that is sending the response
REQUEST_PACKAGE_ID|8 Bit|The ID of the request that triggered this response

### 1 - Scan
This request has normally just that one byte and is a broadcast for the whole network.
The answer is a 32Bit Integer. 
By default that response Integer is `1843378999` or `6DDFBB37` in hex.

**Request**
NAME|SIZE|DESCRIPTION
----|----|-----------
SCAN_INT|32 Bit|`551531122` or `0x20DFB272`

**RESPONSE**
NAME|SIZE|DESCRIPTION
----|----|-----------
SCAN_INT|32 Bit|`1843378999` or `0x6DDFBB37`
DEVICE_IDS|32 Bit ARRAY|An array with all "devices" that are controlled over that endpoint

### 2 - general initialization
This request the data of that device.

**Request**
NAME|SIZE|DESCRIPTION
----|----|-----------
DEVICE_ID|32 Bit|The Id of the device that the request is targeting

**RESPONSE**
NAME|SIZE|DESCRIPTION
----|----|-----------
PROTOCOL_VERSION|32 Bit|the version id of the current protocol version. You can see the current version at the top of this document.
ID|32 Bit|A random Integer that needs to be the same every time, also after restart. The MAC address is a good way to implement this .
DEVICE_TYPE|8 Bit| This is the id of the device type. You can read more about device types later.


### 3 - Device specific initialization
**Request**
NAME|SIZE|DESCRIPTION
----|----|-----------
-|-|-

The response of this is different of every device type, so you can read how this is build for every device type in the section of that. It is listed here because every device type needs to have this.

### 4 - Get data from sensor
When the device of a host is registered in a sensor it send back the value of the data when it changed

NAME|SIZE|DESCRIPTION
----|----|-----------
VALUE|16 Bit| The value is from 0 to 32768

## Device types

### 0 - Light
This is used if you have lights, no matter if a lightbulb, LED-strips or even addressable lights light addressable LED-strips or computer fans. It is just named Lights, feel free to connect other things to it like motors.

#### Package Types
##### 3 - Light initialization
**Request**
NAME|SIZE|DESCRIPTION
----|----|-----------
-|-|-

**RESPONSE**
NAME|SIZE|DESCRIPTION
----|----|-----------
LIGHT_AMOUNT|32 Bit|The amount of individual lights. A addressable LED-strip can have multiple for example. If the lights are not individually addressable this should be 1.
COLOR_SPECTRUM|8 Bit|This represents the color spectrum that the light can show, the color spectrums are listed under this table on their own.

**SPECTRUMS**
CODE|SPECTRUM
----|--------
0|One color, just On/Off
1|One color, dimmable
2|RGB
-| If you have other spectrums like RGBW you have to use RGB and then convert that to the spectrum that you need on the device.

##### 16 - Run animation
If this package is received, all other bytes are a compiled LAL script.

**Request**
NAME|SIZE|DESCRIPTION
----|----|-----------
SCRIPT|depends on script|A Compiled LAL script

##### 17 - Dim
This is only needed in dimmable devices (COLOR_SPECTRUM: 1 and 2)
NAME|SIZE|DESCRIPTION
----|----|-----------
DIMMING_VALUE|8 Bit|the color brightness from 0 to 255 (255 is brightest and 0 is off)

### 1 - Sensors
Sensors can be anything, the measure something as a number and can send them to one or multiple hosts. This is also used by switches.

#### Package Types
##### 3 - Switch initialization
This initialization doesn't have any usage.
**Request**
NAME|SIZE|DESCRIPTION
----|----|-----------
-|-|-

**RESPONSE**
NAME|SIZE|DESCRIPTION
----|----|-----------
RECOMMENDED_UPDATE_WAY|8 Bit| Update way id, all ways are listed under this table.
UPDATE_FREQUENCY|32 Bit| The frequency what the host is recommended to request an update, only if RECOMMENDED_UPDATE_WAY=0 otherwise just 0.
DECIMALS|8 Bit| The value gets multiplied with this, to match the value that the user should see
SUFFIX|64 Bit| A string encoded with UTF-8 that is appeded to the shown value ('%', '°C', etc)

**update ways**
ID|DESCRIPTION
----|----
0|Host have to request
1|register ip in this device with later defined way. Then the sensor send the value every time it updates.

##### 16 - Get data
Gets the current sensor data
**Request**
NAME|SIZE|DESCRIPTION
----|----|-----------
-|-|-

**RESPONSE**
NAME|SIZE|DESCRIPTION
----|----|-----------
VALUE|64 Bit| The Value represented in a double

##### 17 - Add ip
Adds the current IP in a list of devices where the data is send when it changes.

NAME|SIZE|DESCRIPTION
----|----|-----------
-|-|-


##### 18 - Remove ip
Removes the current IP in a list of devices where the data is send when it changes.

NAME|SIZE|DESCRIPTION
----|----|-----------
-|-|-