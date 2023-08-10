#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <FastLED.h>
#include <NTPClient.h>
#include <AnimatedGIF.h>

#define DEBUG false

// time
#define TIME_SERVER "2.europe.pool.ntp.org"
#define TIME_OFFSET 3600
#define TIME_UPDATE_FREQUENCY 3600000

WiFiUDP NtpUDP;
NTPClient TimeClient(NtpUDP, TIME_SERVER);
long StartTime = 0;
long StartMillis = 0;

// leds
#define HEIGHT 16
#define WIDTH 16
#define LED_COUNT HEIGHT * WIDTH

#define LED_PIN 4

const int ledCount = LED_COUNT;
CRGB Leds[LED_COUNT];

// wifi
#define SSID "FRITZ!Box 7560 XE"
#define PSK  "TbT%b7Lmw29.hm,)<PP9b2jj3V-Ex~24KW<E&Y:m=gdwX<$tqJCkj,QVzedv}5Z"

//#define SSID "led_steuerungs_antrieb"
//#define PSK  "64A73V646EG66QC9"

// USM
#define DEVICE_TYPE 2
#define SPECTRUM 2

#define USM_VERSION 3
#define SCAN_INT 0x6DDFBB37
#define VARIABLE_BUFFER_SIZE 100
#define ARGUMENT_BUFFER_SIZE 10
#define BYTE_CODE_BUFFER_SIZE 5000
#define ARRAY_BUFFER_SIZE 1000

// gif 
AnimatedGIF gif;

// animation
byte ByteCodeBuffer[BYTE_CODE_BUFFER_SIZE];
bool CancelCurrentAnimation = false;
bool AnimationRunning = false;
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
int requestIndex = 0;

void setup() {
  if(DEBUG){
    Serial.begin(115200);
  }
  FastLED.addLeds<WS2812B, LED_PIN, GRB>(Leds, LED_COUNT).setCorrection( TypicalLEDStrip );
  WiFi.begin(SSID, PSK);
  WaitUntilWifiConnected();

  
  if(DEBUG)
    Serial.println("connected to WiFi");
  UdpClient.begin(PORT);
  TimeClient.begin();
  TimeClient.setTimeOffset(TIME_OFFSET);

  char mac[17];
  WiFi.macAddress().toCharArray(mac, 18);
  byte macBytes[6];
  ParseBytes(mac, ':', macBytes, 6, 16);
  Id = macBytes[0] + (macBytes[1] << 8) + (macBytes[2] << 16) + (macBytes[3] << 24);

  
  if(DEBUG){
    Serial.print("Id: ");
    Serial.println(Id);
  }
  
  for (int i = 0; i < LED_COUNT; i++) {
    Leds[i] = CRGB(0, 0, 0);
  }
  FastLED.show();
  
  gif.begin(GIF_PALETTE_RGB888);
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
  PacketSize = 0;
  ResponsePos = 0;
  requestIndex = 0;
  
  int packetType = readValue(1);
  int currentId = readValue(4);

  // Serial.print("id: ");
  // Serial.println(currentId);
  
  // Serial.print("packetType: ");
  // Serial.println(packetType);

    if(DEBUG){
      Serial.print("new request: ");
    }
  if(currentId != 0 && currentId != Id){
    if(DEBUG){
      Serial.println("- wrong id");
    }
    return;
  }

  
  WriteResponse(0, 1);
  WriteResponse(currentId, 4);
  WriteResponse(packetType, 1);
  
  switch (packetType) {
    case 0: {
        break;
      }
    case 1: {
        if(DEBUG)
          Serial.println("received scan");
        WriteResponse(SCAN_INT, 4);
        WriteResponse(Id, 4);
        break;
      }
    case 2: {
        if(DEBUG)
          Serial.println("received init");
        WriteResponse(USM_VERSION, 4);
        WriteResponse(Id, 4);
        WriteResponse(DEVICE_TYPE, 1);
        break;
      }
    case 3: {
        if(DEBUG)
          Serial.println("received specific init");
        WriteResponse(WIDTH, 4);
        WriteResponse(HEIGHT, 4);
        WriteResponse(SPECTRUM, 1);
        break;
      }
    case 19: {
        if(DEBUG)
          Serial.println("new gif");
      int gifSize = readValue(4);
        for (int i = 0; i < gifSize; i++) {
          ByteCodeBuffer[i] = IncomingPacket[i + 9];
        }

        startPlayGif(gifSize);
        break;
      }
    case 20: {
        if(DEBUG)
          Serial.println("new animation");
        int byteCodeSize = readValue(4);
        for (int i = 0; i < byteCodeSize; i++) {
          ByteCodeBuffer[i] = readValue(1);
        }
        for (int i = 0; i < 10; i++) {
          ByteCodeBuffer[i + byteCodeSize] = 0;
        }
        if(DEBUG)
          Serial.println("starting");
        Run();
        if(DEBUG)
          Serial.println("finished");
        break;
      }
    case 21: {
        byte dimmingFactor = readValue(1);
        FastLED.setBrightness(dimmingFactor);
        if (!AnimationRunning) {
          Run();
        }
        break;
      }
    default:{
        if(DEBUG){
          Serial.print("unknown package type ");
          Serial.println(packetType);
        }
        break;
    }
  }
  if (ResponsePos > 6) {
    // Serial.println("send response");
    // Serial.print("to: ");
    // Serial.println(UdpClient.remoteIP());
    // Serial.print("length: ");
    // Serial.println(ResponsePos);
    UdpClient.beginPacket(UdpClient.remoteIP(), UdpClient.remotePort());
    UdpClient.write(ResponsePacket, ResponsePos);
    UdpClient.endPacket();
    // Serial.println("send complete");
  }
}

void startPlayGif(int gifSize){
  int rc;
  rc = gif.open((uint8_t *)ByteCodeBuffer, gifSize, drawGif);
  if(!rc){
    return;
  }

  int frameDelay = 0;
  AnimationRunning = true;
  long lastFrame = 0;
  //while(gif.playFrame(true, NULL)){}

  while (!CancelCurrentAnimation) {
    CheckForPackage();
    if (!NeedByteCodeCancelation()) {
      if (PacketSize) {
        ProcessPackage();
      }
    }
    
    if(millis() - lastFrame >= frameDelay){
      lastFrame = millis();
      rc = gif.playFrame(false, &frameDelay);
    }
    delay(1);
  }
  CancelCurrentAnimation = false;
  AnimationRunning = false;
  gif.close();
}

void drawGif(GIFDRAW *pDraw){
  Leds[0] = CRGB(0, 0, 0);
  uint8_t r, g, b, *s, *p, *pPal = (uint8_t *)pDraw->pPalette;
  int x, y = pDraw->iY + pDraw->y;
    s = pDraw->pPixels;
    if (pDraw->ucDisposalMethod == 2){
      p = &pPal[pDraw->ucBackground * 3];
      r = p[0];
      g = p[1];
      b = p[2];

      for (x=0; x<pDraw->iWidth; x++) {
        if (s[x] == pDraw->ucTransparent) {
            Leds[getIndex(x, y)] = CRGB(r, g, b);
        }
      }
      pDraw->ucHasTransparency = 0;
    }
    // Apply the new pixels to the main image
    if (pDraw->ucHasTransparency) // if transparency used
    {
      const uint8_t ucTransparent = pDraw->ucTransparent;
      for (x=0; x<pDraw->iWidth; x++)
      {
        if (s[x] != ucTransparent) {
           p = &pPal[s[x] * 3];
            Leds[getIndex(x, y)] = CRGB(p[0], p[1], p[2]);
        }
      }
    }
    else // no transparency, just copy them all
    {
      for (x=0; x<pDraw->iWidth; x++)
      {
           p = &pPal[s[x] * 3];
            Leds[getIndex(x, y)] = CRGB(p[0], p[1], p[2]);
      }
    }
    if (pDraw->y == pDraw->iHeight-1) // last line has been decoded, display the image
          FastLED.show();
}

int getIndex(int x, int y){
  if(y % 2 == 0){
    return y*WIDTH + (WIDTH - x - 1) + 1; 
  } else{
    return y*WIDTH+x + 1;
  }
}

bool NeedByteCodeCancelation() {
  CancelCurrentAnimation = PacketSize > 0 && (IncomingPacket[0] == 19 || IncomingPacket[0] == 20);
  return CancelCurrentAnimation;
}

void WriteResponse(long value, int bytes) {
  for (int i = 0; i < bytes; i++) {
    ResponsePacket[ResponsePos] = value >> (8 * i);
    ResponsePos++;
  }
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


long GetTime() {
  if (StartTime == 0 || millis() - StartMillis > TIME_UPDATE_FREQUENCY) {
    TimeClient.update();
    StartTime = TimeClient.getEpochTime();
    StartMillis = millis();
  }
  return StartTime + (millis() - StartMillis);
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
  variables[1] = WIDTH;
  variables[1] = HEIGHT;
  short locals[ARGUMENT_BUFFER_SIZE];
  ArrayPos = 0;
  AnimationRunning = true;
  Run(0, variables, locals);
  AnimationRunning = false;
  CancelCurrentAnimation = false;
}

short Run(short pos, short variables[VARIABLE_BUFFER_SIZE], short locals[ARGUMENT_BUFFER_SIZE]) {
  if (pos < 0) {
    switch (pos) {
      case -1: {
          int x = locals[0];
          int y = locals[1];
          int index = getIndex(x, y);
          Leds[index] = CRGB(locals[2], locals[3], locals[4]);
          break;
        }
      case -2: {
          FastLED.show();
          break;
        }
      case -3: {
          short rnd =  RandomShort();
          if(DEBUG)
            Serial.println(rnd);
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

  while (pos < BYTE_CODE_BUFFER_SIZE && !CancelCurrentAnimation) {
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
  } else if (CancelCurrentAnimation) {
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
