#define DEBUG
// #define USE_GxEPD2_4G
// #define USE_GRAYSCALE_DISPLAY

#include "secrets_config.h"

// generic libraries
#include <Arduino.h>
#include <ArduinoJson.h>
#include <ArduinoOTA.h>
#include <HTTPClient.h>
#include <SPI.h>
#include <Syslog.h>
#include <WiFiManager.h>
#include <WiFiUdp.h>

#ifdef USE_GxEPD2_4G
#ifdef USE_GRAYSCALE_DISPLAY
#include <GxEPD2_4G_4G.h>
#else
#include <GxEPD2_4G_BW.h>
#endif
#else
#include <GxEPD2_BW.h>
#endif

#include "debug.h"
#include "main.h"
#include "settings.h"

/* local vars */
#ifdef USE_GxEPD2_4G
#ifdef USE_GRAYSCALE_DISPLAY
GxEPD2_4G_4G<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(
    GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
#else
GxEPD2_4G_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(
    GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
#endif
#else
GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
#endif

WiFiManager wifiManager;
WiFiClient wifiClient;
HTTPClient http;
uint32_t fullStartTime;
static const uint16_t input_buffer_pixels = DISPLAY_HEIGHT;
uint8_t input_row_mono_buffer[input_buffer_pixels];  // at most 1 byte per pixel
String serverURLBase = String("http://") + CALENDAR_URL_HOST + ":" + CALENDAR_URL_PORT;

// configurable remotely
uint64_t sleepTime = SECONDS_PER_HOUR;
float criticalVoltage = 1.1;
bool otaMode = false;

/* RTC vars (survives deep sleep) */
RTC_DATA_ATTR int bootCount = 0;
// TODO check the length in read loop to prevent overflow
RTC_DATA_ATTR char lastChecksum[64 + 1] = "<not_defined_yet>";

void setup() {
  basicInit();
  wakeupAndConnect();

  if (otaMode) {
    runInfinoteOTALoopInsteadOfUsualFunctionality();
  };

  checkVoltage();
  fetchAndDrawImageIfNeeded();
  disconnectAndHibernate();
}

void loop() {
  // Shouldn't get here at all due to the deep sleep called in setup
}

float getVoltage() {
  int value = analogRead(SINGLE_AAA_VOLTAGE_PIN);
  float volts = value * 3.3 / 4095.0;
  DEBUG_PRINT("Voltage raw read (pin %d): %d", SINGLE_AAA_VOLTAGE_PIN, value);
  DEBUG_PRINT("Voltage: %f", volts);
  return volts;
};

void checkVoltage() {
  float voltage = getVoltage();
  if (voltage < criticalVoltage) {
    error(String("Voltage ") + voltage + "V critical, below " +
          criticalVoltage + "V");
  };
};

void loadConfigFromWeb() {
  String jsonURL = serverURLBase + "/config?voltage=" + getVoltage();
  DEBUG_PRINT("Loading config from web");

  String jsonText = httpGETRequestAsString(jsonURL.c_str());

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

  float tmpf = response["critical_voltage"];
  if (tmpf != 0) {
    criticalVoltage = tmpf;
    DEBUG_PRINT("criticalVoltage set to %f volts", tmpf);
  }

  bool tmpb = response["ota_mode"];
  otaMode = tmpb;
  if (otaMode) {
    DEBUG_PRINT("OTA mode enabled in remote config");
    if (esp_reset_reason() == ESP_RST_SW) {
      DEBUG_PRINT("^ but last reset was a software one => not running OTA loop.");
      DEBUG_PRINT("To force OTA mode again, reset the device manually.");
      otaMode = false;
    }
  } else {
    DEBUG_PRINT("OTA mode disabled in JSON config");
  };
}

void basicInit() {
  fullStartTime = millis();
  ++bootCount;
  Serial.begin(115200);
}

void wakeupAndConnect() {
  initDisplay();
  if (!startWiFi()) {
    errorNoWifi();
  }
  logResetReason();
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

  DEBUG_PRINT("Boot count: %d, last image checksum: %s", bootCount,
              lastChecksum);
}

void disconnectAndHibernate() {
  logRuntimeStats();
  // ^ last syslog message before the WiFi disconnects
  stopDisplay();
  stopWiFi();
  espDeepSleep(sleepTime);
}

void fetchAndDrawImageIfNeeded() {
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

void showRawBitmapFrom_HTTP(const char* host, int port, const char* path,
                            int16_t x, int16_t y, int16_t w, int16_t h,
                            int16_t bytes_per_row, int16_t rows_at_once) {
  bool connection_ok = false;
  uint32_t startTime = millis();
  if ((x >= display.epd2.WIDTH) || (y >= display.epd2.HEIGHT)) return;

  DEBUG_PRINT("Downloading http://%s:%d%s", host, port, path);
  if (!wifiClient.connect(host, port)) {
    DEBUG_PRINT("HTTP connection failed");
    error("Connection to HTTP server failed.");
    return;
  }

  wifiClient.print(String("GET ") + path + " HTTP/1.1\r\n" + "Host: " + host +
                   "\r\n" + "User-Agent: Portal_Calendar_on_ESP\r\n" +
                   "Connection: close\r\n\r\n");
  String line = "<not read anything yet>";
  while (wifiClient.connected()) {
    line = wifiClient.readStringUntil('\n');
    DEBUG_PRINT(" read line: [%s\n]\n", line.c_str());
    if (!connection_ok) {
      DEBUG_PRINT("Waiting for OK response from server. Current line: %s",
                  line.c_str());
      connection_ok = line.startsWith("HTTP/1.1 200 OK");
      //   if (connection_ok)
      //     DEBUG_PRINT("line: %s", line.c_str());
      //   // if (!connection_ok) Serial.println(line);
    }
    if (!connection_ok) {
      DEBUG_PRINT("Unexpected first line: %s", line.c_str());
      // break;
    }
    if ((line == "\r") || (line == "")) {
      DEBUG_PRINT("All headers received");
      break;
    }
  };

  if (!connection_ok) {
    error(
        "Unexpected HTTP response, didn't found '200 OK'.\nLast line "
        "was:\n" +
        line);
    return;
  }

  DEBUG_PRINT("Reading bitmap header");
  line = wifiClient.readStringUntil('\n');
  if (line != "MM")  // signature
  {
    error("Invalid bitmap received.");
  }

  DEBUG_PRINT("Reading checksum");
  line = wifiClient.readStringUntil('\n');  // checksum
  DEBUG_PRINT("Last checksum was: %s", lastChecksum);
  DEBUG_PRINT("New checksum is: %s", line.c_str());
  if (line == String(lastChecksum)) {
    DEBUG_PRINT("Not refreshing, image is unchanged");
    return;
  } else {
    DEBUG_PRINT(
        "Checksum has changed, reading image and refreshing the display");
  };
  strcpy(lastChecksum, line.c_str());  // to survive a reboot

  DEBUG_PRINT("Reading image data");
  uint32_t bytes_read = 0;                              // read so far
  for (uint16_t row = 0; row < h; row += rows_at_once)  // for each line
  {
    if (!connection_ok || !(wifiClient.connected() || wifiClient.available()))
      break;
    yield();  // prevent WDT

    uint32_t got =
        read8n(wifiClient, input_row_mono_buffer, bytes_per_row * rows_at_once);
    bytes_read += got;

    if (!connection_ok) {
      DEBUG_PRINT("Bytes read so far: %d", bytes_read);
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
  DEBUG_PRINT("Bytes read: %d", bytes_read);

  display.refresh();  // full refresh
  DEBUG_PRINT("downloaded and displayed in %lu ms", millis() - startTime);

  wifiClient.stop();
}

String httpGETRequestAsString(const char* url) {
  // Your IP address with path or Domain name with URL path
  DEBUG_PRINT("connecting to %s", url);
  http.setConnectTimeout(10000);
  http.begin(wifiClient, url);

  DEBUG_PRINT("calling GET");
  int httpResponseCode = http.GET();

  String payload = "";
  if (httpResponseCode == 200) {
    payload = http.getString();
  }

  DEBUG_PRINT("end, response=%d", httpResponseCode);
  http.end();

  return payload;
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

  DEBUG_PRINT("WiFi shutdown took %lums", millis() - start);
  // }
}

bool startWiFi() {
  bool res;

  DEBUG_PRINT("Connecting to WiFi");
  unsigned long start = millis();

  // wifiManager.setFastConnectMode(true); // no difference
  res = wifiManager.autoConnect();

  if (!res) {
    DEBUG_PRINT("Failed to connect");
    stopWiFi();
    return false;
  }
  DEBUG_PRINT("---");
  DEBUG_PRINT("Build version: %s %s", __DATE__, __TIME__);
  DEBUG_PRINT("Connected to WiFi in %lu ms", millis() - start);
  DEBUG_PRINT("IP address: %s", WiFi.localIP().toString().c_str());
  return true;
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
    if (millis() - start > 2000) break;  // don't hang forever
  }
  return bytes - remain;
}

void displayText(String message) {
  display.setRotation(3);
  display.setFont(&Open_Sans_Regular_24);
  display.setTextColor(GxEPD_BLACK);
  int16_t tbx, tby;
  uint16_t tbw, tbh;
  display.getTextBounds(message, 0, 0, &tbx, &tby, &tbw, &tbh);
  // center bounding box by transposition of origin:
  uint16_t x = ((display.width() - tbw) / 2) - tbx;
  uint16_t y = ((display.height() - tbh) / 2) - tby;  // y is base line!
  DEBUG_PRINT("Text bounds for:\n\"%s\"\n are: [x=%d, y=%d][w=+%d,h=+%d]",
              message.c_str(), x, y, tbw, tbh);

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

void runInfinoteOTALoopInsteadOfUsualFunctionality() {
  DEBUG_PRINT("Running OTA loop");

  // ArduinoOTA.handle();

  ArduinoOTA.setHostname(SYSLOG_MYHOSTNAME);
  ArduinoOTA
      .onStart([]() {
        String type;
        DEBUG_PRINT("OTA: command %s", ArduinoOTA.getCommand())
        if (ArduinoOTA.getCommand() == U_FLASH)
          type = "sketch";
        else  // U_SPIFFS
          type = "filesystem";

        // NOTE: if updating SPIFFS this would be the place to unmount
        // SPIFFS using SPIFFS.end()
        DEBUG_PRINT("OTA: Start updating %s", type.c_str());
      })
      .onEnd([]() {
        DEBUG_PRINT("OTA: End");
        DEBUG_PRINT("OTA: Rebooting...");
      })
      .onProgress([](unsigned int progress, unsigned int total) {
        // Serial.printf("Progress: %u%%\r", (progress / (total / 100)));
      })
      .onError([](ota_error_t error) {
        DEBUG_PRINT("OTA: Error %u", error);
        if (error == OTA_AUTH_ERROR) {
          DEBUG_PRINT("OTA: Auth Failed");
        } else if (error == OTA_BEGIN_ERROR) {
          DEBUG_PRINT("OTA: Begin Failed");
        } else if (error == OTA_CONNECT_ERROR) {
          DEBUG_PRINT("OTA: Connect Failed");
        } else if (error == OTA_RECEIVE_ERROR) {
          DEBUG_PRINT("OTA: Receive Failed");
        } else if (error == OTA_END_ERROR) {
          DEBUG_PRINT("OTA: End Failed");
        }
      });

  ArduinoOTA.begin();
  while (true) {
    ArduinoOTA.handle();
    delay(100);
  }

  DEBUG_PRINT("OTA loop done");
}

void error(String message) {
  strcpy(lastChecksum, "");  // to force reload of image next time
  DEBUG_PRINT("Displaying error: %s", message.c_str());
  displayText(message);
  disconnectAndHibernate();
}

void errorNoWifi() { error("WiFi connect/login unsuccessful."); }

void espDeepSleep(uint64_t seconds) {
  DEBUG_PRINT("Sleeping for %llus", seconds);
  esp_sleep_enable_timer_wakeup(seconds * uS_PER_S);
  esp_deep_sleep_start();
}

void initDisplay() {
  DEBUG_PRINT("display setup start");
  DEBUG_PRINT("CS=%d, DC=%d, RST=%d, BUSY=%d", CS_PIN, DC_PIN, RST_PIN,
              BUSY_PIN);
  delay(100);
  SPIClass* spi = new SPIClass(SPI_BUS);
  /* 2ms reset for waveshare board */
  display.init(115200, false, 2, false, *spi,
               SPISettings(7000000, MSBFIRST, SPI_MODE0));
  DEBUG_PRINT("display setup finished");
}

void stopDisplay() {
  //
  display.powerOff();
}

void logRuntimeStats() {
  DEBUG_PRINT("Total execution time: %lu ms", millis() - fullStartTime);
  DEBUG_PRINT("Going to hibernate for %lu seconds", sleepTime);
}