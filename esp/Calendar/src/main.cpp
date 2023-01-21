#include <Arduino.h>

// core libraries from ESP8266 Arduino
#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>
// #include <DNSServer.h>
// #include <ESP8266mDNS.h>
// #include <ESP8266WebServer.h>

// other libraries
#include <WiFiManager.h>
#include <GFX.h>
#include <PNGdec.h>
#include <GxEPD2_BW.h>
#include <GxEPD2_BW.h>

#include <time.h>

#include "secrets_config.h"

#define DEBUG

#ifdef DEBUG
#define DEBUG_PRINT(...)      \
  Serial.printf(__VA_ARGS__); \
  Serial.print('\n')
#else
#define DEBUG_PRINT(...)
#endif

// Display is 800x480 B/W https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/
// aka WaveShare SKU 13187: https://www.waveshare.com/7.5inch-e-paper.htm
// aka GoodDisplay GDEW075T7 800x480 (EK79655 / GD7965)

// Driver board is https://www.laskakit.cz/waveshare-esp8266-e-paper-raw-panel-driver-board/
// aka WaveShare "ESP8266 e-Paper Raw Panel Driver Board"
// Driver board settings: switch set to "A" position (because the "B" position produced streaking and incompletely drawn content)

GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(GxEPD2_750_T7(/*CS=D8=GPIO15*/ SS, /*DC=D2=GPIO4*/ 4, /*RST=D4=GPIO2*/ 2, /*BUSY=D1=GPIO5*/ 5));

#include "Open_Sans_ExtraBold_60.h"
#include "Open_Sans_ExtraBold_120.h"

WiFiManager wifiManager;
WiFiClient wifiClient; // for HTTP requests

void stopWiFi()
{
  // if (WiFi.getMode() != WIFI_OFF)
  // {
  DEBUG_PRINT("Stopping WiFi");
  unsigned long start = millis();
  // WiFi.disconnect(true);
  // WiFi.mode(WIFI_OFF);
  // wifiManager.disconnect();

  WiFi.persistent(false); // do not reset settings (SSID/password) when disconnecting
  // WiFi.disconnect(true);   // no, this resets the password too (even when only in current session, it's enough to prevent WiFiManager to reconnect)
  WiFi.mode(WIFI_OFF); // this is sufficient
  WiFi.persistent(true);

  DEBUG_PRINT("WiFi shutdown took %lums", millis() - start);
  // }
}

bool startWiFi()
{
  bool res;

  DEBUG_PRINT("Connecting to WiFi");
  unsigned long start = millis();
  res = wifiManager.autoConnect();

  if (!res)
  {
    DEBUG_PRINT("Failed to connect");
    stopWiFi();
    return false;
  }
  DEBUG_PRINT("Connected.");
  DEBUG_PRINT("IP address: %s", WiFi.localIP().toString().c_str());
  DEBUG_PRINT("WiFi connection took %lums", millis() - start);
  return true;
}

// void error(std::initializer_list<String> message)
// {
//     DEBUG_PRINT("Sleeping with error");
//     stopWifi(); // Power down wifi before updating display to limit current draw from battery
//     display.error(message, true);
//     deepSleep(SECONDS_PER_HOUR);
// }

// void errorNoWifi()
// {
//     error({
//         "NO WIFI CONNECTION",
//         "",
//         "Your WiFi network is either down, out of range,",
//         "or you entered the wrong password.",
//         "",
//         "WiFi Name:",
//         "\"" WIFI_NAME "\""
//     });
// }

// const uint32_t partial_update_period_s = 2;
const uint32_t partial_update_period_s = 60;
const uint32_t full_update_period_s = 1 * 60 * 60;
const uint32_t speed_up_factor = 20; // for simulation
const uint32_t millis_per_second = 1000 / speed_up_factor;

uint32_t start_time;
uint32_t next_time;
uint32_t previous_time;
uint32_t previous_full_update;

uint32_t total_seconds = 0;
uint32_t seconds, minutes, hours, days;

void full_white()
{
  display.setFullWindow();
  display.firstPage();
  do
  {
    display.fillScreen(GxEPD_WHITE);
  } while (display.nextPage());
}

void full_black()
{
  display.setFullWindow();
  display.firstPage();
  do
  {
    display.fillScreen(GxEPD_BLACK);
  } while (display.nextPage());
}

void partial_white()
{
  display.setPartialWindow(0, 0, display.width(), display.height());
  display.firstPage();
  do
  {
    display.fillScreen(GxEPD_WHITE);
  } while (display.nextPage());
}

void partial_black()
{
  display.setPartialWindow(0, 0, display.width(), display.height());
  display.firstPage();
  do
  {
    display.fillScreen(GxEPD_BLACK);
  } while (display.nextPage());
}

void horizontal_stripes(uint16_t nr)
{
  uint16_t h = display.height() / nr;
  display.setPartialWindow(0, 0, display.width(), display.height());
  display.firstPage();
  do
  {
    uint16_t y = 0;
    do
    {
      display.fillRect(0, y, display.width(), h, GxEPD_BLACK);
      y += h;
      if (y >= display.height())
        break;
      display.fillRect(0, y, display.width(), h, GxEPD_WHITE);
      y += h;
    } while (y < display.height());
  } while (display.nextPage());
}

void vertical_stripes(uint16_t nr)
{
  uint16_t w = display.width() / nr;
  display.setPartialWindow(0, 0, display.width(), display.height());
  display.firstPage();
  do
  {
    uint16_t x = 0;
    do
    {
      display.fillRect(x, 0, w, display.height(), GxEPD_BLACK);
      x += w;
      if (x >= display.width())
        break;
      display.fillRect(x, 0, w, display.height(), GxEPD_WHITE);
      x += w;
    } while (x < display.width());
  } while (display.nextPage());
}

void showText(String str, int16_t bbx, int16_t bby, int16_t rotation)
{

  static uint16_t bbw = 0;
  static uint16_t bbh = 0;
  display.setRotation(rotation);
  display.setFont(&Open_Sans_ExtraBold_120);
  display.setTextColor(GxEPD_BLACK);
  int16_t tbx, tby;
  uint16_t tbw, tbh;
  display.getTextBounds(str, 0, 0, &tbx, &tby, &tbw, &tbh);
  // place the bounding box, cover any previous ones
  int16_t tx = max(0, ((display.width() - tbw) / 2));
  int16_t ty = max(0, ((display.height() - tbh) / 2));
  bbx = min(bbx, tx);
  bby = min(bby, ty);
  bbw = max(bbw, tbw);
  bbh = max(bbh, tbh);
  // calculate the cursor
  int16_t x = bbx - tbx;
  int16_t y = bby - tby;
  display.setPartialWindow(bbx, bby, bbw, bbh);
  display.firstPage();
  do
  {
    display.fillScreen(GxEPD_WHITE);
    // display.drawRect(bbx, bby, bbw, bbh, GxEPD_BLACK);
    display.setCursor(x, y);
    display.print(str);
  } while (display.nextPage());
  delay(1000);
}

void setup()
{
  Serial.begin(115200);
  Serial.println();
  Serial.println("setup");
  Serial.printf("width = %d\n", display.width());
  Serial.printf("height = %d\n", display.height());
  delay(100);
  display.init(115200);

  startWiFi();

  // cleanup
  // for (int i = 0; i < 1; i++) {
  //   full_black();
  //   delay(500);
  //   full_white();
  //   delay(500);
  // }

  // // stripes test
  // for (int i = 0; i < 1; i++) {
  //   horizontal_stripes(16);
  //   delay(1000);
  //   vertical_stripes(8);
  //   delay(1000);
  // }

  // full_white();
  // delay(1000);

  showText("test1", 200, 300, 0); // landscape
  showText("test2", 400, 100, 1); // portrait
  display.powerOff();
  // delay(3000);

  // full_white();
  // delay(1000);
  // display.powerOff();
  // stopWiFi();
  // delay(1000);
  // start_time = next_time = previous_time = previous_full_update = millis();
  Serial.println("setup done");
}

HTTPClient http;

void loop()
{
  // uncomment for continuous clock simulation test
  // clock_test();

  Serial.println("----------------------------------------");
  Serial.println("loop start");

  startWiFi();

  String url = CALENDAR_URL;
  http.begin(wifiClient, url);
  int httpResponseCode = http.GET();
  Serial.print("HTTP Response code: ");
  Serial.println(httpResponseCode);
  if (httpResponseCode == 200)
  {
    String payload = http.getString();
    Serial.print("Response length: ");
    Serial.println(payload.length());
  }
  http.end();

  // display the image
  // FIXME

  // sleep

  display.powerOff();
  stopWiFi();

  Serial.println("loop end (+wait)");
  Serial.println("");
  delay(100000);
}
