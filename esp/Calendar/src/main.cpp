#include <Arduino.h>

#define DEBUG
// #define DEBUG_ESP_HTTP_CLIENT
// #define DEBUG_ESP_PORT Serial

// #define USE_GxEPD2_4G
// #define USE_GRAYSCALE_DISPLAY

// core libraries from ESP8266 Arduino
#include <ESP8266WiFi.h>
// #include <ESP8266HTTPClient.h>
// #include <DNSServer.h>
// #include <ESP8266mDNS.h>
// #include <ESP8266WebServer.h>

// other libraries
#include <WiFiManager.h>
#include <GFX.h>
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


#include "secrets_config.h"

#ifdef DEBUG
#define DEBUG_PRINT(...)      \
  Serial.printf(__VA_ARGS__); \
  Serial.print('\n')
#else
#define DEBUG_PRINT(...)
#endif

#define DISPLAY_WIDTH 480
#define DISPLAY_HEIGHT 800

// Display is 800x480 B/W https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/
// aka WaveShare SKU 13187: https://www.waveshare.com/7.5inch-e-paper.htm
// aka GoodDisplay GDEW075T7 800x480 (EK79655 / GD7965)

// Driver board is https://www.laskakit.cz/waveshare-esp8266-e-paper-raw-panel-driver-board/
// aka WaveShare "ESP8266 e-Paper Raw Panel Driver Board"
// Driver board settings: switch set to "A" position (because the "B" position produced streaking and incompletely drawn content)

// michals: 800x480
#ifdef USE_GxEPD2_4G
#ifdef USE_GRAYSCALE_DISPLAY
// FIXME GxEPD2_4G_4G<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(GxEPD2_750_T7(/*CS=D8=GPIO15*/ SS, /*DC=D2=GPIO4*/ 4, /*RST=D4=GPIO2*/ 2, /*BUSY=D1=GPIO5*/ 5)); // GDEW075T7 800x480, EK79655
GxEPD2_4G_4G<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(GxEPD2_750_T7(SS, 4, 2, 5));
#else
GxEPD2_4G_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(GxEPD2_750_T7(/*CS=D8=GPIO15*/ SS, /*DC=D2=GPIO4*/ 4, /*RST=D4=GPIO2*/ 2, /*BUSY=D1=GPIO5*/ 5)); // GDEW075T7 800x480, EK79655 (GD7965)
#endif
#else
GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(GxEPD2_750_T7(/*CS=D8=GPIO15*/ SS, /*DC=D2=GPIO4*/ 4, /*RST=D4=GPIO2*/ 2, /*BUSY=D1=GPIO5*/ 5)); // GDEW075T7 800x480, EK79655 (GD7965)
#endif

// #include "Open_Sans_ExtraBold_60.h"
// #include "Open_Sans_ExtraBold_120.h"

WiFiManager wifiManager;
WiFiClient wifiClient; // for HTTP requests

void stopWiFi();
bool startWiFi();
void drawImageFromServer();
void showRawBitmapFrom_HTTP(const char *host, int port, const char *path, int16_t x, int16_t y, int16_t w, int16_t h, int16_t bytes_per_row, int16_t rows_at_once);
uint32_t read8n(WiFiClient &client, uint8_t *buffer, int32_t bytes);
uint32_t skip(WiFiClient &client, int32_t bytes);
uint32_t read32(WiFiClient &client);
uint16_t read16(WiFiClient &client);

uint32_t start_time;
uint32_t next_time;
uint32_t previous_time;
uint32_t previous_full_update;

uint32_t total_seconds = 0;
uint32_t seconds, minutes, hours, days;

void setup()
{
  Serial.begin(115200);
  Serial.println();
  Serial.println("setup");
  Serial.printf("width = %d\n", display.width());
  Serial.printf("height = %d\n", display.height());
  delay(100);
  display.init(115200);

  // Serial.printf("hasFastPartialUpdate=%d\n", display.epd2.hasFastPartialUpdate);
  // Serial.printf("hasPartialUpdate=%d\n", display.epd2.hasPartialUpdate);
  Serial.println("setup done");
}

void loop()
{
  Serial.println("----------------------------------------");
  Serial.println("loop start");

  startWiFi();
  drawImageFromServer();
  display.powerOff();
  stopWiFi();

  Serial.println("sleeping for 1 hour");
  delay(1000 * 60 * 60); // 1 hour
}

void drawImageFromServer()
{
#ifdef USE_GRAYSCALE_DISPLAY
  showRawBitmapFrom_HTTP(CALENDAR_URL_HOST, CALENDAR_URL_PORT, "/calendar/bitmap/epapergray", 0, 0, 800, 480, 800, 1);
#else
  showRawBitmapFrom_HTTP(CALENDAR_URL_HOST, CALENDAR_URL_PORT, "/calendar/bitmap/epapermono", 0, 0, 800, 480, /* bpp */ 800 / 8, /* rows at once */ 8);
#endif
}

static const uint16_t input_buffer_pixels = 800;    // may affect performance
uint8_t input_row_mono_buffer[input_buffer_pixels]; // 1 byte per pixel

void showRawBitmapFrom_HTTP(const char *host, int port, const char *path, int16_t x, int16_t y, int16_t w, int16_t h, int16_t bytes_per_row, int16_t rows_at_once)
{
  WiFiClient client;
  bool connection_ok = false;
  bool valid = false; // valid format to be handled
  uint32_t startTime = millis();
  if ((x >= display.epd2.WIDTH) || (y >= display.epd2.HEIGHT))
    return;
  Serial.println();
  Serial.print("connecting to ");
  Serial.println(host);
  if (!client.connect(host, port))
  {
    Serial.println("HTTP connection failed");
    return;
  }
  Serial.print("requesting URL: ");
  Serial.println(String("http://") + host + path);
  client.print(String("GET ") + path + " HTTP/1.1\r\n" +
               "Host: " + host + "\r\n" +
               "User-Agent: GxEPD2_WiFi_Example\r\n" +
               "Connection: close\r\n\r\n");
  Serial.println("request sent");
  while (client.connected())
  {
    String line = client.readStringUntil('\n');
    if (!connection_ok)
    {
      connection_ok = line.startsWith("HTTP/1.1 200 OK");
      if (connection_ok)
        Serial.println(line);
      // if (!connection_ok) Serial.println(line);
    }
    if (!connection_ok)
      Serial.println(line);
    // Serial.println(line);
    if (line == "\r")
    {
      Serial.println("headers received");
      break;
    }
  }
  if (!connection_ok)
    return;

  Serial.println("Parsing bitmap header");
  if (read16(client) == 0x4D4D) // "MM" signature
  {
    uint32_t bytes_read = 2;                            // read so far
    Serial.println(String("") + "w=" + w + ", h=" + h); // OK=[99, 100]

    valid = true;
    // display.clearScreen();

    for (uint16_t row = 0; row < h; row += rows_at_once) // for each line
    {
      if (!connection_ok || !(client.connected() || client.available()))
        break;
      delay(1); // yield() to avoid WDT
      yield();

      uint32_t got = read8n(client, input_row_mono_buffer, bytes_per_row * rows_at_once);
      bytes_read += got;

      if (!connection_ok)
      {
        Serial.print("Error: got no more after ");
        Serial.print(bytes_read);
        Serial.println(" bytes read!");
        break;
      }

#ifdef USE_GRAYSCALE_DISPLAY
      // https://github.com/ZinggJM/GxEPD2_4G/blob/master/src/epd/GxEPD2_750_T7.cpp
      display.writeImage_4G(input_row_mono_buffer, 8, x, y + row, w, 1, false, false, false);
#else
      display.writeImage(input_row_mono_buffer, x, y + row, w, rows_at_once);
#endif
    } // end line
    Serial.print("downloaded and displayed in ");
    Serial.print(millis() - startTime);
    Serial.println(" ms");
    display.refresh();

    Serial.print("bytes read ");
    Serial.println(bytes_read);
  }
  client.stop();
  if (!valid)
  {
    Serial.println("bitmap format not handled.");
  }
}

void stopWiFi()
{
  DEBUG_PRINT("Stopping WiFi");
  unsigned long start = millis();

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

uint16_t read16(WiFiClient &client)
{
  // BMP data is stored little-endian, same as Arduino.
  uint16_t result;
  ((uint8_t *)&result)[0] = client.read(); // LSB
  ((uint8_t *)&result)[1] = client.read(); // MSB
  return result;
}

uint32_t read32(WiFiClient &client)
{
  // BMP data is stored little-endian, same as Arduino.
  uint32_t result;
  ((uint8_t *)&result)[0] = client.read(); // LSB
  ((uint8_t *)&result)[1] = client.read();
  ((uint8_t *)&result)[2] = client.read();
  ((uint8_t *)&result)[3] = client.read(); // MSB
  return result;
}

uint32_t skip(WiFiClient &client, int32_t bytes)
{
  int32_t remain = bytes;
  uint32_t start = millis();
  while ((client.connected() || client.available()) && (remain > 0))
  {
    if (client.available())
    {
      client.read();
      remain--;
    }
    else
      delay(1);
    if (millis() - start > 2000)
      break; // don't hang forever
  }
  return bytes - remain;
}

uint32_t read8n(WiFiClient &client, uint8_t *buffer, int32_t bytes)
{
  int32_t remain = bytes;
  uint32_t start = millis();
  while ((client.connected() || client.available()) && (remain > 0))
  {
    if (client.available())
    {
      int16_t v = client.read();
      *buffer++ = uint8_t(v);
      remain--;
    }
    else
      delay(1);
    if (millis() - start > 2000)
      break; // don't hang forever
  }
  return bytes - remain;
}
