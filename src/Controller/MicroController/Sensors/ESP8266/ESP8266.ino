#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <Adafruit_AHTX0.h>

//Sensor
Adafruit_AHTX0 aht;


WiFiUDP NtpUDP;

// wifi
#define SSID "FRITZ!Box 7560 XE"
#define PSK  "TbT%b7Lmw29.hm,)<PP9b2jj3V-Ex~24KW<E&Y:m=gdwX<$tqJCkj,QVzedv}5Z"

// USM
#define DEVICE_TYPE 1
#define RECOMMENDED_UPDATE_WAY 0
#define UPDATE_FREQUENCY 1000
#define DECIMALS 1

#define USM_VERSION 2
#define SCAN_INT 0x6DDFBB37

#define ID_COUNT 2

// UDP
#define PORT 4210

#define ID_COUNT 2

String SUFFIX_MOISTURE = "%";
String SUFFIX_TEMP = "°C";

int Ids[ID_COUNT];

int CurrentId;

WiFiUDP UdpClient;
byte IncomingPacket[100];
byte ResponsePacket[32];
int PacketSize = 0;
int ResponsePos = 0;
int requestIndex = 0;

long long suffixs[ID_COUNT];

double Values[ID_COUNT];

void setup() {
  Serial.begin(9600);

  delay(500);

  if (!aht.begin()) {
    Serial.println("Could not find AHT? Check wiring");
    while (1) delay(10);
  }
  Serial.println("AHT10 or AHT20 found");
  
  WiFi.begin(SSID, PSK);
  WaitUntilWifiConnected();
  UdpClient.begin(PORT);

  char mac[17];
  WiFi.macAddress().toCharArray(mac, 18);
  byte macBytes[6];
  ParseBytes(mac, ':', macBytes, 6, 16);
  Ids[0] = macBytes[0] + (macBytes[1] << 8) + (macBytes[2] << 16) + (macBytes[3] << 24);
  Ids[0] ^= macBytes[2] + (macBytes[3] << 8) + (macBytes[4] << 16) + (macBytes[5] << 24);

  for(int i = 1; i < ID_COUNT; i++){
    Ids[i] = Ids[i-1]+7;  
  }
  
  suffixs[0] = stringToLongBits(SUFFIX_MOISTURE, SUFFIX_MOISTURE.length());
  suffixs[1] = stringToLongBits(SUFFIX_TEMP, SUFFIX_TEMP.length());
  Serial.println("--suffixs--");
  Serial.println((int)suffixs[0]);
  Serial.println((int)suffixs[1]);
  Serial.println("--end--");
  
}

void ParseBytes(const char* str, char sep, byte* bytes, int maxBytes, int base) {
  for (int i = 0; i < maxBytes; i++) {
    bytes[i] = strtoul(str, NULL, base);
    str = strchr(str, sep);
    if (str == NULL || *str == '\0') {
      break;
    }
    str++;
  }
}

void loop() {
  updateValues();
  if (PacketSize) {
    ProcessPackage();
  }
  if (!PacketSize) {
    CheckForPackage();
  }
}

void CheckForPackage() {
  WaitUntilWifiConnected();

  PacketSize = UdpClient.parsePacket();
  if (PacketSize) {
    UdpClient.read(IncomingPacket, sizeof(IncomingPacket));
  }
}

void WaitUntilWifiConnected() {
  while (WiFi.status() != WL_CONNECTED) {
    delay(100);
  }
}

void ProcessPackage() {
  requestIndex = 0;
  PacketSize = 0;
  ResponsePos = 0;
  
  int packetType = readValue(1);
  CurrentId = readValue(4);

  int currentIdIndex;

  for(currentIdIndex = 0; currentIdIndex < ID_COUNT; currentIdIndex++){
    if(CurrentId == Ids[currentIdIndex]){
        break;
    }
  }
  
  WriteResponse(0, 1);
  WriteResponse(CurrentId, 4);
  WriteResponse(packetType, 1);

  switch (packetType) {
    case 0: {
        break;
      }
    case 1: {
        int requestScanInt = readValue(4);
        if(requestScanInt != 551531122)
          break;
        WriteResponse(SCAN_INT, 4);
        for(int i = 0; i < ID_COUNT; i++){
          WriteResponse(Ids[i], 4);  
        }
        break;
      }
    case 2: {
        WriteResponse(USM_VERSION, 4);
        WriteResponse(CurrentId, 4);
        WriteResponse(DEVICE_TYPE, 1);
        break;
      }
    case 3: {
        WriteResponse(RECOMMENDED_UPDATE_WAY, 1);
        WriteResponse(UPDATE_FREQUENCY, 4);
        WriteResponse(DECIMALS, 1);

        WriteResponse(suffixs[currentIdIndex], 8);
        break;
      }
    case 16: {
          WriteResponse(DoubleToLongBits(Values[currentIdIndex]), 8);
        break;
      }
  }
  if (ResponsePos > 0) {
    UdpClient.beginPacket(UdpClient.remoteIP(), UdpClient.remotePort());
    UdpClient.write(ResponsePacket, ResponsePos);
    UdpClient.endPacket();
  }
}
void WriteResponse(long long value, int bytes) {
  for (int i = 0; i < bytes; i++) {
    ResponsePacket[ResponsePos] = value >> (8 * i);
    ResponsePos++;
  }
}

long long DoubleToLongBits(double value){
  return *((long long*)(&value));
}

long long stringToLongBits(String str, int len){
  byte buf[8];
  for(int i = 0; i < 8; i++){
    buf[i] = 0;  
  }
  str.getBytes(buf, len + 1);
  
  
  return (*((long long*)(&buf)));
}


double updateValues(){
  sensors_event_t humidity, temp;
  aht.getEvent(&humidity, &temp);// populate temp and humidity objects with fresh data
  Values[0] = humidity.relative_humidity;
  Values[1] = temp.temperature;
}

long readValue(int bytes){
  long value = 0;
  for(int i = 0; i < bytes; i++){
    int idx = i + requestIndex;
    value += IncomingPacket[idx] << (i*8);
  }
  requestIndex += bytes;
  return value;
}
