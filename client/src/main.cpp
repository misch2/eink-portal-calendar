// generic libraries
#include <Adafruit_GFX.h>
#include <Arduino.h>
#include <ArduinoHttpClient.h>
#include <ArduinoJson.h>
#include <ArduinoOTA.h>
#ifdef SPI_BUS
#include <SPI.h>
#endif
#include <Syslog.h>
#ifdef USE_WIFI_MANAGER
#include <WiFiManager.h>
#endif
#include <WiFiUdp.h>

// dynamically include board-specific config
// clang-format off
#define STRINGIFY(x) STR(x)
#define STR(x) #x
#define EXPAND(x) x
#define CONCAT3(a, b, c) STRINGIFY(EXPAND(a)EXPAND(b)EXPAND(c))
#include CONCAT3(boards/,BOARD_CONFIG,.h)
// clang-format on

#define DISPLAY_BUFFER_SIZE (DISPLAY_WIDTH * BITMAP_BPP / 8)

#ifdef DISPLAY_TYPE_BW
#include <GxEPD2_BW.h>
const char *defined_color_type = "BW";
#endif

#ifdef DISPLAY_TYPE_GRAYSCALE
const char *defined_color_type = "4G";
#ifdef USE_GRAYSCALE_BW_DISPLAY
#include <GxEPD2_4G_BW.h>
#else
#include <GxEPD2_4G_4G.h>
#endif
#endif

#ifdef DISPLAY_TYPE_3C
const char *defined_color_type = "3C";
#include <GxEPD2_3C.h>
#endif

// TODO add other display types too

#include "debug.h"
#include "main.h"
#include "version.h"

// macro to define "display" variable dynamically with the right type
DISPLAY_INSTANCE

/* RTC vars (survives deep sleep) */
RTC_DATA_ATTR int wakeupCount = 0;
RTC_DATA_ATTR char lastChecksum[64 + 1] = "<not_defined_yet>";  // TODO check the length in read loop to prevent overflow

// remotely configurable variables (via JSON)
int sleepTime = SECONDS_PER_HOUR;
bool otaMode = false;

// ordinary vars
#ifdef USE_WIFI_MANAGER
WiFiManager wifiManager;
#endif
WiFiClient wifiClient;
HttpClient httpClient = HttpClient(wifiClient, CALENDAR_URL_HOST, CALENDAR_URL_PORT);

uint32_t fullStartTime;

int readVoltage() {
  int rawVoltageADCReading;
#ifdef VOLTAGE_ADC_PIN
  rawVoltageADCReading = analogRead(VOLTAGE_ADC_PIN);
  DEBUG_PRINT("Voltage raw read (pin %d): %d", VOLTAGE_ADC_PIN, rawVoltageADCReading);
#else
  rawVoltageADCReading = 0;
  DEBUG_PRINT("Voltage not measured, no pin defined");
#endif
  return rawVoltageADCReading;
};

void startHttpGetRequest(String path) {
  DEBUG_PRINT(">>> GET %s", path.c_str());
  httpClient.beginRequest();
  httpClient.get(path);
  httpClient.endRequest();

  int statusCode = httpClient.responseStatusCode();
  DEBUG_PRINT("<<< HTTP response code: %d", statusCode);

  while (httpClient.connected() && httpClient.available() && httpClient.headerAvailable()) {
    String headerName = httpClient.readHeaderName();
    String headerValue = httpClient.readHeaderValue();
    DEBUG_PRINT("<<< HTTP header: %s: %s", headerName.c_str(), headerValue.c_str());
  };
  // httpClient.skipResponseHeaders();

  if (statusCode != 200) {
    error(String("Unexpected HTTP response.\n"
                 "URL: ") +
          String(CALENDAR_URL_HOST) + ":" + String(CALENDAR_URL_PORT) + path + "\n" +
          "\n"
          "Response code: " +
          String(statusCode) + "\n");
  };
}

void loadConfigFromWeb() {
  String path = "/config?mac=" + WiFi.macAddress()                                //
                + "&adc=" + readVoltage()                                         // read only once, because it discharges the 100nF capacitor
                + "&w=" + String(DISPLAY_WIDTH) + "&h=" + String(DISPLAY_HEIGHT)  //
                + "&c=" + String(defined_color_type)                              //
                + "&fw=" + String(FIRMWARE_VERSION);                              //
  startHttpGetRequest(path);
  String jsonText = httpClient.responseBody();

  DynamicJsonDocument response(1000);
  DeserializationError errorStr = deserializeJson(response, jsonText);

  if (errorStr) {
    DEBUG_PRINT("error: %s", errorStr.c_str());
    error("Can't parse response");
  }

  int tmpi = response["sleep"];
  if (tmpi != 0) {
    sleepTime = tmpi;
    DEBUG_PRINT("sleepTime set to %d seconds", tmpi);
  }

  bool tmpb = response["ota_mode"];
  otaMode = tmpb;
  if (otaMode) {
    DEBUG_PRINT("Permanent OTA mode enabled in remote config");
    if (esp_reset_reason() == ESP_RST_SW || esp_reset_reason() == ESP_RST_DEEPSLEEP) {
      DEBUG_PRINT("^ but last reset was a software one => not running OTA loop.");
      DEBUG_PRINT("To force OTA mode again, reset the device manually.");
      otaMode = false;
    }
  };
}

void basicInit() {
  fullStartTime = millis();
  ++wakeupCount;
  Serial.begin(115200);
}

void wakeupAndConnect() {
  initDisplay();
  if (!startWiFi()) {
    error("WiFi connect/login unsuccessful.");
  }
  logResetReason();
  initOTA();
  loadConfigFromWeb();
}

void logResetReason() {
  esp_sleep_wakeup_cause_t wakeup_reason = esp_sleep_get_wakeup_cause();
  esp_reset_reason_t reset_reason = esp_reset_reason();

  if (reset_reason == ESP_RST_UNKNOWN) {
    DEBUG_PRINT("Reset reason: UNKNOWN");
  } else if (reset_reason == ESP_RST_POWERON) {
    DEBUG_PRINT("Reset reason: POWERON");
  } else if (reset_reason == ESP_RST_SW) {
    DEBUG_PRINT("Reset reason: SW");
  } else if (reset_reason == ESP_RST_PANIC) {
    DEBUG_PRINT("Reset reason: PANIC");
  } else if (reset_reason == ESP_RST_INT_WDT) {
    DEBUG_PRINT("Reset reason: INT_WDT");
  } else if (reset_reason == ESP_RST_TASK_WDT) {
    DEBUG_PRINT("Reset reason: TASK_WDT");
  } else if (reset_reason == ESP_RST_WDT) {
    DEBUG_PRINT("Reset reason: _WDT");
  } else if (reset_reason == ESP_RST_DEEPSLEEP) {
    DEBUG_PRINT("Reset reason: DEEPSLEEP");
  } else if (reset_reason == ESP_RST_BROWNOUT) {
    DEBUG_PRINT("Reset reason: BROWNOUT");
  } else if (reset_reason == ESP_RST_SDIO) {
    DEBUG_PRINT("Reset reason: SDIO");
  } else {
    DEBUG_PRINT("Reset reason: ? (%d)", reset_reason);
  }

  if (wakeup_reason == ESP_SLEEP_WAKEUP_TIMER) {
    DEBUG_PRINT("Wakeup reason: TIMER");
  } else if (wakeup_reason == ESP_SLEEP_WAKEUP_UNDEFINED) {
    DEBUG_PRINT("Wakeup reason: UNDEFINED");
  } else {
    DEBUG_PRINT("Wakeup reason: ? (%d)", wakeup_reason);
  }

  DEBUG_PRINT("Boot count: %d, last image checksum: %s", wakeupCount, lastChecksum);
}

void disconnectAndHibernate() {
  logRuntimeStats();
  // ^ last syslog message before the WiFi disconnects
  stopDisplay();
  stopWiFi();
  boardSpecificDone();
  espDeepSleep(sleepTime);
}

int httpReadStringUntil(char terminator, String &result) {
  result = "";

  int bytes = 0;
  while (httpClient.connected() && httpClient.available()) {
    int c = httpClient.read();
    if (c < 0) {
      DEBUG("Premature end of HTTP response");
      break;
    }
    bytes++;
    if (c == terminator) {
      break;
    }
    result += (char)c;
  }
  return bytes;
}

void showRawBitmapFrom_HTTP(const char *path, int16_t x, int16_t y, int16_t w, int16_t h) {
  static const int input_buffer_pixels = DISPLAY_HEIGHT;
  static unsigned char input_row_mono_buffer[input_buffer_pixels];   // at most 1 byte per pixel
  static unsigned char input_row_color_buffer[input_buffer_pixels];  // at most 1 byte per pixel

  uint32_t startTime = millis();
  if ((x >= display.epd2.WIDTH) || (y >= display.epd2.HEIGHT)) {
    return;
  }

  String partial_uri = String(path) + "?mac=" + WiFi.macAddress();
  startHttpGetRequest(partial_uri);

  DEBUG_PRINT("Expected content length (from headers): %d", httpClient.contentLength());

  uint32_t bytes_read = 0;
  DEBUG_PRINT("Reading bitmap header");
  String line;
  bytes_read += httpReadStringUntil('\n', line);
  if (line != "MM")  // signature
  {
    error(String("Invalid bitmap received, doesn't start with a magic sequence:\n") + "Line: " + line + "\n");
  }

  DEBUG_PRINT("Reading checksum");
  bytes_read += httpReadStringUntil('\n', line);
  DEBUG_PRINT("Last checksum was: %s", lastChecksum);
  DEBUG_PRINT("New checksum is: %s", line.c_str());
  if (line == String(lastChecksum)) {
    DEBUG_PRINT("Not refreshing, image is unchanged");
    return;
  } else {
    DEBUG_PRINT("Checksum has changed, reading image and refreshing the display");
  };
  strcpy(lastChecksum, line.c_str());  // to survive a deep sleep

  DEBUG_PRINT("Reading image data for %d rows", h);

  for (uint16_t row = 0; row < h; row++) {
    // DEBUG_PRINT("Reading row %d, bytes_read=%d", row, bytes_read);

#ifdef DISPLAY_TYPE_BW
    bytes_read += httpClient.read(input_row_mono_buffer, DISPLAY_BUFFER_SIZE);
#endif
#ifdef DISPLAY_TYPE_3C
    bytes_read += httpClient.read(input_row_mono_buffer, DISPLAY_BUFFER_SIZE);
    bytes_read += httpClient.read(input_row_color_buffer, DISPLAY_BUFFER_SIZE);
#endif
    // TODO add other display types too

// #ifdef USE_GRAYSCALE_DISPLAY
//     // https://github.com/ZinggJM/GxEPD2_4G/blob/master/src/epd/GxEPD2_750_T7.cpp
//     display.writeImage_4G(input_row_mono_buffer, 8, x, y + row, w, 1, false,
//                           false, false);
// #endif
#ifdef DISPLAY_TYPE_BW
    display.writeImage(input_row_mono_buffer, x, y + row, w, 1);
#endif
#ifdef DISPLAY_TYPE_3C
    display.writeImage(input_row_mono_buffer, input_row_color_buffer, x, y + row, w, 1);
#endif
    // TODO add other display types too

  }  // end line
  DEBUG_PRINT("Bytes read: %d, bytes expected: %d, %s", bytes_read, httpClient.contentLength(), bytes_read == httpClient.contentLength() ? "OK" : "ERROR");
  if (bytes_read != httpClient.contentLength()) {
    DEBUG_PRINT("WARNING: bytes read != bytes expected, skipped %d bytes", httpClient.contentLength() - bytes_read);
  }

  display.refresh();  // full refresh
  DEBUG_PRINT("downloaded and displayed in %lu ms", millis() - startTime);
}

void stopWiFi() {
  unsigned long start = millis();

  // do not reset settings (SSID/password) when disconnecting
  WiFi.persistent(false);

  // WiFi.disconnect(true);   // no, this resets the password too (even when
  // only in current session, it's enough to prevent WiFiManager to reconnect)

  // this is sufficient to disconnect
  WiFi.mode(WIFI_OFF);
  WiFi.persistent(true);

  DEBUG_PRINT("WiFi shutdown took %lu ms", millis() - start);
  // }
}

bool startWiFi() {
  bool res;

  DEBUG_PRINT("Connecting to WiFi");
  unsigned long start = millis();

  // wifiManager.setFastConnectMode(true); // no difference
#ifdef USE_WIFI_MANAGER
  res = wifiManager.autoConnect();
  if (!res) {
    DEBUG_PRINT("Failed to connect");
    stopWiFi();
    return false;
  }
#else
  WiFi.setHostname(HOSTNAME);
  WiFi.config(NETWORK_IP_ADDRESS, NETWORK_IP_GATEWAY, NETWORK_IP_SUBNET, NETWORK_IP_DNS);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  while (WiFi.status() != WL_CONNECTED) {
    delay(100);
  }
#endif

  DEBUG_PRINT("---");
  // DEBUG_PRINT("Build date: %s %s", __DATE__, __TIME__);
  DEBUG_PRINT("Firmware version: %s", String(FIRMWARE_VERSION));
  DEBUG_PRINT("Connected to WiFi in %lu ms", millis() - start);
  DEBUG_PRINT("IP address: %s", WiFi.localIP().toString().c_str());
  DEBUG_PRINT("MAC address: %s", WiFi.macAddress().c_str());

  return true;
}

void displayText(String message, const GFXfont *font) {
  display.setRotation(3);

  if (font == NULL) {
    font = &Open_Sans_Regular_24;
  };
  display.setFont(font);
  display.setTextColor(GxEPD_BLACK);
  int16_t tbx, tby;
  uint16_t tbw, tbh;
  display.getTextBounds(message, 0, 0, &tbx, &tby, &tbw, &tbh);
  // center bounding box by transposition of origin:
  uint16_t x = ((display.width() - tbw) / 2) - tbx;
  uint16_t y = ((display.height() - tbh) / 2) - tby;  // y is base line!
  // DEBUG_PRINT("Text bounds for:\n\"%s\"\n are: [x=%d, y=%d][w=+%d,h=+%d]", message.c_str(), x, y, tbw, tbh);

  // rectangle make the window big enough to cover (overwrite) previous text
  // uint16_t wh = Open_Sans_Regular_24.yAdvance;
  // uint16_t wy = (display.height() / 2) - wh / 2;
  uint16_t wy = (display.height() / 4);
  uint16_t wh = (display.height() / 2);
  // display.setPartialWindow(0, wy, display.width(), wh);

  display.firstPage();
  do {
    display.fillScreen(GxEPD_WHITE);
    display.setCursor(x, y);
    display.print(message);
  } while (display.nextPage());
  display.refresh();  // full refresh
}

void initOTA() {
  ArduinoOTA.setHostname(HOSTNAME);
  // ArduinoOTA
  //     .onStart([]() {
  //       String type;
  //       DEBUG_PRINT("OTA: command %s", ArduinoOTA.getCommand())
  //       if (ArduinoOTA.getCommand() == U_FLASH)
  //         type = "sketch";
  //       else  // U_SPIFFS
  //         type = "filesystem";

  //       // NOTE: if updating SPIFFS this would be the place to unmount
  //       // SPIFFS using SPIFFS.end()
  //       DEBUG_PRINT("OTA: Start updating %s", type.c_str());
  //     })
  //     .onEnd([]() { DEBUG_PRINT("OTA: End"); })
  //     .onProgress([](unsigned int progress, unsigned int total) {
  //       Serial.printf("Progress: %u%%\r", (progress / (total / 100)));  // FIXME
  //     })
  //     .onError([](ota_error_t error) {
  //       DEBUG_PRINT("OTA: Error %u", error);
  //       if (error == OTA_AUTH_ERROR) {
  //         DEBUG_PRINT("OTA: Auth Failed");
  //       } else if (error == OTA_BEGIN_ERROR) {
  //         DEBUG_PRINT("OTA: Begin Failed");
  //       } else if (error == OTA_CONNECT_ERROR) {
  //         DEBUG_PRINT("OTA: Connect Failed");
  //       } else if (error == OTA_RECEIVE_ERROR) {
  //         DEBUG_PRINT("OTA: Receive Failed");
  //       } else if (error == OTA_END_ERROR) {
  //         DEBUG_PRINT("OTA: End Failed");
  //       }
  //     });
  ArduinoOTA.begin();
  DEBUG_PRINT("OTA: Ready on %s.local", HOSTNAME);
}

void error(String message) {
  strcpy(lastChecksum, "");  // to force reload of image next time
  DEBUG_PRINT("Displaying error: %s", message.c_str());
  displayText(message, &DejaVu_Sans_Mono_16);
  disconnectAndHibernate();
}

void espDeepSleep(uint64_t seconds) {
  DEBUG_PRINT("Sleeping for %lu s", seconds);
  esp_sleep_enable_timer_wakeup(seconds * uS_PER_S);
  esp_deep_sleep_start();
}

void initDisplay() {
  DEBUG_PRINT("Display setup start");
  DEBUG_PRINT("CS=%d, DC=%d, RST=%d, BUSY=%d", CS_PIN, DC_PIN, RST_PIN, BUSY_PIN);

  delay(100);

#ifdef SPI_BUS
  SPIClass *spi = new SPIClass(SPI_BUS);
#ifdef REMAP_SPI
  Serial.println("remapping SPI");
  // only CLK and MOSI are important for EPD
  spi->begin(PIN_SPI_CLK, PIN_SPI_MISO, PIN_SPI_MOSI, PIN_SPI_SS);  // swap pins
#endif
  Serial.println("remapped");
  /* 2ms reset for waveshare board */
  display.init(115200, false, 2, false, *spi, SPISettings(7000000, MSBFIRST, SPI_MODE0));
#else
  /* 2ms reset for waveshare board */
  display.init(115200, false, 2, false);
#endif

  DEBUG_PRINT("Display setup finished");
}

void stopDisplay() {
  //
  display.powerOff();
}

void logRuntimeStats() {
  DEBUG_PRINT("Total execution time: %lu ms", millis() - fullStartTime);
  DEBUG_PRINT("Going to hibernate for %d seconds", sleepTime);
}

void setup() {
  basicInit();
  boardSpecificInit();
  wakeupAndConnect();

  if (otaMode) {
    DEBUG_PRINT("Running OTA loop on %s (%s.local)", WiFi.localIP().toString().c_str(), HOSTNAME);
    while (true) {
      ArduinoOTA.handle();
      delay(5);  // msec
    }
  };

  showRawBitmapFrom_HTTP("/calendar/bitmap/epaper", 0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT);
  disconnectAndHibernate();
}

void loop() {
  // Shouldn't get here at all due to the deep sleep called in setup
}
