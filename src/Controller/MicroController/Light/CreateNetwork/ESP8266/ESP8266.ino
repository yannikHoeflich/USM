#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <FastLED.h>
#include <NTPClient.h>

// time
#define TIME_SERVER "2.europe.pool.ntp.org"
#define TIME_OFFSET 3600
#define TIME_UPDATE_FREQUENCY 3600000

// leds
#define LED_COUNT 37
#define LED_PIN 4

double DimmingFactor = 0.5;
const int ledCount = LED_COUNT;
CRGB Leds[LED_COUNT];

// wifi
#define SSID "Led Ring"
#define PSK  "123456789"

// USM
#define DEVICE_TYPE 0
#define SPECTRUM 2

#define USM_VERSION 1
#define SCAN_INT 0x6DDFBB37
#define VARIABLE_BUFFER_SIZE 100
#define ARGUMENT_BUFFER_SIZE 10
#define BYTE_CODE_BUFFER_SIZE 10000
#define ARRAY_BUFFER_SIZE 1000

byte ByteCodeBuffer[BYTE_CODE_BUFFER_SIZE];
bool CancelByteCode = false;
bool ByteCodeRunning = false;
int Id;

int ArrayPos = 0;
short ArrayBuffer[ARRAY_BUFFER_SIZE];
long LastFrame = 0;
int delayCounter;

// UDP
#define PORT 4210

WiFiUDP UdpClient;
byte IncomingPacket[BYTE_CODE_BUFFER_SIZE + 100];
byte ResponsePacket[32];
int PacketSize = 0;
int ResponsePos = 0;

void setup() {
  FastLED.addLeds<WS2812B, LED_PIN, GRB>(Leds, LED_COUNT);
  if(!WiFi.softAP(SSID, PSK)){
    Serial.println("error creating SoftAp");
    }
  UdpClient.begin(PORT);

  char mac[17];
  WiFi.macAddress().toCharArray(mac, 18);
  byte macBytes[6];
  ParseBytes(mac, ':', macBytes, 6, 16);
  Id = macBytes[0] + (macBytes[1] << 8) + (macBytes[2] << 16) + (macBytes[3] << 24);
  for (int i = 0; i < LED_COUNT; i++) {
    Leds[i] = CRGB(0, 0, 0);
  }
  FastLED.show();
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
  //Serial.println(WiFi.status());
  if (PacketSize) {
    ProcessPackage();
  }
  if (!PacketSize) {
    CheckForPackage();
  }
}

void CheckForPackage() {
  PacketSize = UdpClient.parsePacket();
  if (PacketSize) {
    UdpClient.read(IncomingPacket, sizeof(IncomingPacket));
  }
}

void ProcessPackage() {
  PacketSize = 0;
  ResponsePos = 0;
  WriteResponse(0, 1);
  WriteResponse(IncomingPacket[0], 1);
  switch (IncomingPacket[0]) {
    case 0: {
        break;
      }
    case 1: {
        WriteResponse(SCAN_INT, 4);
        break;
      }
    case 2: {
        WriteResponse(USM_VERSION, 4);
        WriteResponse(Id, 4);
        WriteResponse(DEVICE_TYPE, 1);
        break;
      }
    case 3: {
        WriteResponse(LED_COUNT, 4);
        WriteResponse(SPECTRUM, 1);
        break;
      }
    case 16: {
        int byteCodeSize = (IncomingPacket[1]) + (IncomingPacket[2] << 8) + (IncomingPacket[3] << 16) + (IncomingPacket[4] << 24);
        for (int i = 0; i < byteCodeSize; i++) {
          ByteCodeBuffer[i] = IncomingPacket[i + 5];
        }
        for (int i = 0; i < 10; i++) {
          ByteCodeBuffer[i + byteCodeSize] = 0;
        }
        Run();
        break;
      }
    case 17: {
        byte rawFactor = IncomingPacket[1];
        DimmingFactor = rawFactor * 1.0 / 255;
        if (!ByteCodeRunning) {
          Run();
        }
        break;
      }
  }
  if (ResponsePos > 0) {
    UdpClient.beginPacket(UdpClient.remoteIP(), UdpClient.remotePort());
    UdpClient.write(ResponsePacket, ResponsePos);
    UdpClient.endPacket();
  }
}

bool NeedByteCodeCancelation() {
  CancelByteCode = PacketSize > 0 && IncomingPacket[0] == 16;
  return CancelByteCode;
}

void WriteResponse(long value, int bytes) {
  for (int i = 0; i < bytes; i++) {
    ResponsePacket[ResponsePos] = value >> (8 * i);
    ResponsePos++;
  }
}

long GetTime() {
  return millis();
}

short GetMinutesOfDay() {
  return GetTime() % (24 * 60 * 60);
}

void WaitFrame(short waitTime) {
  long currentTime = millis();
  long delayTime = waitTime - (currentTime - LastFrame);
  if (delayTime > 0)
    delay(delayTime);
  LastFrame = millis();
}

void Run() {
  short variables[VARIABLE_BUFFER_SIZE];
  variables[0] = 0;
  variables[1] = LED_COUNT;
  short locals[ARGUMENT_BUFFER_SIZE];
  ArrayPos = 0;
  ByteCodeRunning = true;
  Run(0, variables, locals);
  ByteCodeRunning = false;
  CancelByteCode = false;
}

short Run(short pos, short variables[VARIABLE_BUFFER_SIZE], short locals[ARGUMENT_BUFFER_SIZE]) {
  if (pos < 0) {
    switch (pos) {
      case -1: {
          Leds[locals[0]] = CRGB((byte)(locals[1] * DimmingFactor), (byte)(locals[2] * DimmingFactor), (byte)(locals[3] * DimmingFactor));
          break;
        }
      case -2: {
          FastLED.show();
          break;
        }
      case -3: {
          short rnd =  RandomShort();
          return rnd;
          break;
        }
      case -4: {
          short arrayPointer = ArrayPos;
          ArrayPos += locals[0];
          return arrayPointer;
          break;
        }
      case -5: {
          short arrayPointer = locals[0];
          short index = locals[1];
          ArrayBuffer[arrayPointer + index] = locals[2];
          break;
        }
      case -6: {
          short arrayPointer = locals[0];
          short index = locals[1];
          return ArrayBuffer[arrayPointer + index];
          break;
        }
      case -7: {
          WaitFrame(locals[0]);
          break;
        }
      case -8: {
          delay(locals[0]);
          break;
        }
      case -9: {
          return GetMinutesOfDay();
          break;
        }
    }

    return 0;
  }

  while (pos < BYTE_CODE_BUFFER_SIZE && !CancelByteCode) {
    delayCounter++;
    if (delayCounter > 10000) {
      delayCounter = 0;
      CheckForPackage();
      if (!NeedByteCodeCancelation()) {
        if (PacketSize) {
          ProcessPackage();
        }
      }
    }

    //Serial.print(pos);
    //Serial.print(" = ");
    switch (ByteCodeBuffer[pos++]) {
      case 0:
        //Serial.println("func");
        return Calculate(pos, variables, locals);
        break;
      case 1:
        //Serial.println("set var");
        SetVariable(pos, variables, locals);
        break;
      case 2:
        //Serial.println("jump");
        Jump(pos, variables, locals);
        break;
      case 3:
        //Serial.println("run function (get value)");
        GetValue(pos, variables, locals);
        break;
      case 4:
        //Serial.println("set local");
        SetLocal(pos, variables, locals);
        break;
    }
  }
  /*if (pos >= BYTE_CODE_BUFFER_SIZE) {
    Serial.print("out of range because ");
    Serial.print(pos);
    Serial.print(" >= ");
    Serial.println(BYTE_CODE_BUFFER_SIZE);
  } else if (CancelByteCode) {
    Serial.println("cancelled because of new request");
  } else {
    Serial.println("I don't know what the fuck is gioing on here");
  }*/
  return 0;
}

byte GetByte(short& pos) {
  return ByteCodeBuffer[pos++];
}

short GetShort(short& pos) {
  return ByteCodeBuffer[pos++] + (ByteCodeBuffer[pos++] << 8);
}

void SetLocal(short& pos, short variables[VARIABLE_BUFFER_SIZE], short locals[ARGUMENT_BUFFER_SIZE]) {
  short index = GetShort(pos);
  locals[index] = Calculate(pos, variables, locals);
}

void Jump(short& pos, short variables[VARIABLE_BUFFER_SIZE], short locals[ARGUMENT_BUFFER_SIZE]) {
  short variableIndex = GetShort(pos);
  short newPos = GetShort(pos);
  if (variableIndex == -1 || variables[variableIndex] == 0) {
    //Serial.print("jmp ");
    //Serial.print(pos);
    //Serial.print(" => ");
    //Serial.println(newPos);
    pos = newPos;
  }
}

void SetVariable(short& pos, short variables[VARIABLE_BUFFER_SIZE], short locals[ARGUMENT_BUFFER_SIZE]) {
  short index = GetShort(pos);
  variables[index] = Calculate(pos, variables, locals);
}

short Calculate(short& pos, short variables[VARIABLE_BUFFER_SIZE], short locals[ARGUMENT_BUFFER_SIZE]) {
  byte calculationCount = GetByte(pos);
  byte calculationType;
  short localValue;
  short value = 0;
  for (int i = 0; i < calculationCount; i++) {
    calculationType = GetByte(pos);
    localValue = GetValue(pos, variables, locals);
    switch (calculationType) {
      case 0:
        value += localValue;
        break;
      case 1:
        value -= localValue;
        break;
      case 2:
        value *= localValue;
        break;
      case 3:
        value /= localValue;
        break;
      case 4:
        value %= localValue;
        break;
      case 5:
        value &= localValue;
        break;
      case 6:
        value |= localValue;
        break;
      case 7:
        value ^= localValue;
        break;
      case 8:
        value = value == localValue ? 1 : 0;
        break;
      case 9:
        value = value != localValue ? 1 : 0;
        break;
      case 10:
        value = value < localValue ? 1 : 0;
        break;
      case 11:
        value = value > localValue ? 1 : 0;
        break;
    }
  }
  return value;
}

short GetValue(short& pos, short variables[VARIABLE_BUFFER_SIZE], short locals[ARGUMENT_BUFFER_SIZE]) {
  short value = 0;
  switch (GetByte(pos)) {
    case 0: {
        value = GetShort(pos);
        break;
      }
    case 1: {
        short variableIndex = GetShort(pos);
        value = variables[variableIndex];
        break;
      }
    case 2: {
        short funcPointer = GetShort(pos);
        short parameterAmount = GetShort(pos);
        short arguments[ARGUMENT_BUFFER_SIZE];
        for (int i = 0; i < parameterAmount && i < ARGUMENT_BUFFER_SIZE; i++) {
          arguments[i] = GetValue(pos, variables, locals);
        }
        value = Run(funcPointer, variables, arguments);
        break;
      }
    case 3: {
        short localIndex = GetShort(pos);
        value = locals[localIndex];
        break;
      }
  }
  return value;
}


static unsigned int c = 123456789, y = 987654321, z = 43219876;
unsigned int RandomInt() {
  unsigned int x = (unsigned int)GetTime();
  unsigned long long t;
  x = 314527869 * x + 1234567;
  y ^= y << 5;
  y ^= y >> 7;
  y ^= y << 22;
  t = 4294584393ULL * z + c;
  c = t >> 32;
  z = t;
  return x + y + z;
}

short RandomShort() {
  return (short)RandomInt();
}
