#include <Arduino.h>

// #define DEBUG

// #define USE_GxEPD2_4G
// #define USE_GRAYSCALE_DISPLAY

// core libraries from ESP8266 Arduino
#include <ESP8266WiFi.h>
// #include <ESP8266HTTPClient.h>
// #include <DNSServer.h>
// #include <ESP8266mDNS.h>
// #include <ESP8266WebServer.h>

// other libraries
#include <GFX.h>
#include <WiFiManager.h>
#include <time.h>

#ifdef USE_GxEPD2_4G
#ifdef USE_GRAYSCALE_DISPLAY
#include <GxEPD2_4G_4G.h>
#else
#include <GxEPD2_4G_BW.h>
#endif
#else
#include <GxEPD2_BW.h>
#endif

#include "display_settings.h"
#include "main.h"
#include "secrets_config.h"

#ifdef DEBUG
#define DEBUG_PRINT(...)      \
  Serial.printf(__VA_ARGS__); \
  Serial.print('\n')
#else
#define DEBUG_PRINT(...)
#endif

// Template needs page_height as 2nd parameter, it's set to half of the display
// height
#ifdef USE_GxEPD2_4G
#ifdef USE_GRAYSCALE_DISPLAY
GxEPD2_4G_4G<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(
    GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
#else
GxEPD2_4G_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(
    GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
#endif
#else
GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(
    GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
#endif

#include "Open_Sans_Regular_16.h"
#include "Open_Sans_Regular_24.h"

WiFiManager wifiManager;
WiFiClient wifiClient;  // for HTTP requests

void setup() {
  Serial.begin(115200);
  Serial.println();

  DEBUG_PRINT("setup display");
  delay(100);
  display.init(115200);
  // display.clearScreen();
  display_text_fast("Starting...");

  DEBUG_PRINT("hasFastPartialUpdate=%d", display.epd2.hasFastPartialUpdate);
  DEBUG_PRINT("hasPartialUpdate=%d", display.epd2.hasPartialUpdate);
  DEBUG_PRINT("setup done");
}

void loop() {
  DEBUG_PRINT("----------------------------------------");
  DEBUG_PRINT("loop start");

  if (!startWiFi()) {
    errorNoWifi();
  }
  drawImageFromServer();
  display.powerOff();
  stopWiFi();

  DEBUG_PRINT("sleeping for 1 hour");
  deepSleep(SECONDS_PER_HOUR);
}

void drawImageFromServer() {
#ifdef USE_GRAYSCALE_DISPLAY
  showRawBitmapFrom_HTTP(CALENDAR_URL_HOST, CALENDAR_URL_PORT,
                         "/calendar/bitmap/epapergray", 0, 0, DISPLAY_HEIGHT,
                         DISPLAY_WIDTH, /* bpp */ DISPLAY_HEIGHT, 1);
#else
  showRawBitmapFrom_HTTP(CALENDAR_URL_HOST, CALENDAR_URL_PORT,
                         "/calendar/bitmap/epapermono", 0, 0, DISPLAY_HEIGHT,
                         DISPLAY_WIDTH,
                         /* bpp */ DISPLAY_HEIGHT / 8, /* rows at once */ 8);
#endif
}

static const uint16_t input_buffer_pixels = DISPLAY_HEIGHT;
uint8_t input_row_mono_buffer[input_buffer_pixels];  // at most 1 byte per pixel

void showRawBitmapFrom_HTTP(const char* host,
                            int port,
                            const char* path,
                            int16_t x,
                            int16_t y,
                            int16_t w,
                            int16_t h,
                            int16_t bytes_per_row,
                            int16_t rows_at_once) {
  bool connection_ok = false;
  bool valid = false;  // valid format to be handled
  uint32_t startTime = millis();
  if ((x >= display.epd2.WIDTH) || (y >= display.epd2.HEIGHT))
    return;
  DEBUG_PRINT("-");
  DEBUG_PRINT("connecting to %s", host);
  if (!wifiClient.connect(host, port)) {
    DEBUG_PRINT("HTTP connection failed");
    error("Connection to HTTP server failed.");
    return;
  }
  DEBUG_PRINT("Downloading http://%s:%d%s", host, port, path);
  wifiClient.print(String("GET ") + path + " HTTP/1.1\r\n" + "Host: " + host +
                   "\r\n" + "User-Agent: GxEPD2_WiFi_Example\r\n" +
                   "Connection: close\r\n\r\n");
  DEBUG_PRINT("request sent");
  while (wifiClient.connected()) {
    String line = wifiClient.readStringUntil('\n');
    DEBUG_PRINT("read line:\n[%s]\n", line.c_str());
    if (!connection_ok) {
      DEBUG_PRINT("Waiting for OK response from server. Current line: %s",
                  line.c_str());
      connection_ok = line.startsWith("HTTP/1.1 200 OK");
      //   if (connection_ok)
      //     DEBUG_PRINT("line: %s", line.c_str());
      //   // if (!connection_ok) Serial.println(line);
    }
    if (!connection_ok) {
      Serial.println("Unexpected first line: ");
      Serial.print(line.c_str());
    }
    if ((line == "\r") || (line == "")) {
      DEBUG_PRINT("all headers received");
      break;
    }
  };
  if (!connection_ok) {
    error("Unexpected HTTP response.");
    return;
  }

  DEBUG_PRINT("Parsing bitmap header");
  if (read16(wifiClient) == 0x4D4D)  // "MM" signature
  {
    uint32_t bytes_read = 2;  // read so far
    DEBUG_PRINT("w=%d, h=%d", w, h);

    valid = true;
    // display.clearScreen();

    for (uint16_t row = 0; row < h; row += rows_at_once)  // for each line
    {
      if (!connection_ok || !(wifiClient.connected() || wifiClient.available()))
        break;
      delay(1);  // yield() to avoid WDT
      yield();

      uint32_t got = read8n(wifiClient, input_row_mono_buffer,
                            bytes_per_row * rows_at_once);
      bytes_read += got;

      if (!connection_ok) {
        Serial.print("Error: got no more after ");
        Serial.print(bytes_read);
        Serial.println(" bytes read!");
        error("Read from HTTP server failed.");
        break;
      }

#ifdef USE_GRAYSCALE_DISPLAY
      // https://github.com/ZinggJM/GxEPD2_4G/blob/master/src/epd/GxEPD2_750_T7.cpp
      display.writeImage_4G(input_row_mono_buffer, 8, x, y + row, w, 1, false,
                            false, false);
#else
      display.writeImage(input_row_mono_buffer, x, y + row, w, rows_at_once);
#endif
    }  // end line
    Serial.print("downloaded and displayed in ");
    Serial.print(millis() - startTime);
    Serial.println(" ms");
    display.refresh();

    Serial.print("bytes read ");
    Serial.println(bytes_read);
  }
  wifiClient.stop();
  if (!valid) {
    Serial.println("bitmap format not handled.");
    error("Invalid bitmap received.");
  }
}

void stopWiFi() {
  DEBUG_PRINT("Stopping WiFi");
  unsigned long start = millis();

  WiFi.persistent(
      false);  // do not reset settings (SSID/password) when disconnecting
  // WiFi.disconnect(true);   // no, this resets the password too (even when
  // only in current session, it's enough to prevent WiFiManager to reconnect)
  WiFi.mode(WIFI_OFF);  // this is sufficient
  WiFi.persistent(true);

  DEBUG_PRINT("WiFi shutdown took %lums", millis() - start);
  // }
}

bool startWiFi() {
  bool res;

  DEBUG_PRINT("Connecting to WiFi");
  unsigned long start = millis();
  res = wifiManager.autoConnect();

  if (!res) {
    DEBUG_PRINT("Failed to connect");
    stopWiFi();
    return false;
  }
  DEBUG_PRINT("Connected.");
  DEBUG_PRINT("IP address: %s", WiFi.localIP().toString().c_str());
  DEBUG_PRINT("WiFi connection took %lums", millis() - start);
  return true;
}

uint16_t read16(WiFiClient& client) {
  // BMP data is stored little-endian, same as Arduino.
  uint16_t result;
  ((uint8_t*)&result)[0] = client.read();  // LSB
  ((uint8_t*)&result)[1] = client.read();  // MSB
  return result;
}

uint32_t read32(WiFiClient& client) {
  // BMP data is stored little-endian, same as Arduino.
  uint32_t result;
  ((uint8_t*)&result)[0] = client.read();  // LSB
  ((uint8_t*)&result)[1] = client.read();
  ((uint8_t*)&result)[2] = client.read();
  ((uint8_t*)&result)[3] = client.read();  // MSB
  return result;
}

uint32_t skip(WiFiClient& client, int32_t bytes) {
  int32_t remain = bytes;
  uint32_t start = millis();
  while ((client.connected() || client.available()) && (remain > 0)) {
    if (client.available()) {
      client.read();
      remain--;
    } else
      delay(1);
    if (millis() - start > 2000)
      break;  // don't hang forever
  }
  return bytes - remain;
}

uint32_t read8n(WiFiClient& client, uint8_t* buffer, int32_t bytes) {
  int32_t remain = bytes;
  uint32_t start = millis();
  while ((client.connected() || client.available()) && (remain > 0)) {
    if (client.available()) {
      int16_t v = client.read();
      *buffer++ = uint8_t(v);
      remain--;
    } else
      delay(1);
    if (millis() - start > 2000)
      break;  // don't hang forever
  }
  return bytes - remain;
}

void display_text_fast(String message) {
  display.setRotation(3);
  display.setFont(&Open_Sans_Regular_24);
  display.setTextColor(GxEPD_BLACK);
  int16_t tbx, tby;
  uint16_t tbw, tbh;
  display.getTextBounds(message, 0, 0, &tbx, &tby, &tbw, &tbh);
  DEBUG_PRINT("Text bounds for \"%s\" are: [%d, %d][+%d,+%d]",
              (String("\n") + message).c_str(), tbx, tby, tbw, tbx);
  // center bounding box by transposition of origin:
  uint16_t x = ((display.width() - tbw) / 2) - tbx;
  uint16_t y = ((display.height() - tbh) / 2) - tby;

  // display.clearScreen();
  // display.setFullWindow();
  display.firstPage();
  do {
    display.fillScreen(GxEPD_WHITE);
    display.setCursor(x, y);
    display.print(message);
  } while (display.nextPage());
}

void error(String message) {
  DEBUG_PRINT("Displaying error: %s", message.c_str());
  stopWiFi();  // Power down wifi before updating display to limit current draw
               // from battery
  display.setFullWindow();
  display_text_fast(message);
  display.powerOff();

  DEBUG_PRINT("sleeping...");
  deepSleep(SECONDS_PER_HOUR);
}

void errorNoWifi() {
  error("WiFi connect/login unsuccessful.");
}

void deepSleep(uint64_t seconds) {
  DEBUG_PRINT("Sleeping for %llus", seconds);
  unsigned long start = millis();  // Stopping wifi can take time
  stopWiFi();
  uint64_t duration = millis() - start;
#ifdef ESP8266
  ESP.deepSleep(seconds * uS_PER_S - duration * 1000);
#else
  esp_sleep_enable_timer_wakeup(seconds * uS_PER_S - duration * 1000);
  esp_deep_sleep_start();
#endif
}
