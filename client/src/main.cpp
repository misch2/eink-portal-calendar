// generic libraries
#include <Adafruit_GFX.h>
#include <Arduino.h>
#include <ArduinoHttpClient.h>
#include <ArduinoJson.h>
#include <ArduinoOTA.h>
#include <WiFi.h>
#include <WiFiUdp.h>

#ifdef SYSLOG_SERVER
#include <Syslog.h>
#endif

// dynamically include board-specific config
#include "board_config.h"

// conditionally included libraries
#ifdef SPI_BUS
#include <SPI.h>
#endif
#ifdef USE_WIFI_MANAGER
#define WM_MDNS
#include <WiFiManager.h>
#endif

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

#include "logger.h"
#include "debug.h"
#include "display_manager.h"
#include "http_client_manager.h"
#include "main.h"
#include "ota_manager.h"
#include "system_info.h"
#include "version.h"
#include "voltage.h"
#include "wdt_manager.h"
#include "wifi_client.h"

// macro to define "display" variable dynamically with the right type
DISPLAY_INSTANCE

/* RTC vars (survives deep sleep) */
RTC_DATA_ATTR int wakeupCount = 0;
RTC_DATA_ATTR char lastChecksum[64 + 1] = "<not_defined_yet>";

#define SLEEP_TIME_DEFAULT (SECONDS_PER_MINUTE * 5)

// remotely configurable variables (via JSON)
int sleepTime = SLEEP_TIME_DEFAULT;
bool otaMode = false;

// ordinary vars
#ifdef USE_WIFI_MANAGER
WiFiManager wifiManager;
#endif

WiFiClientWithBlockingReads wifiClient;
HttpClient httpClient = HttpClient(wifiClient, CALENDAR_URL_HOST, CALENDAR_URL_PORT);

#ifdef SYSLOG_SERVER
WiFiUDP udpClient;
Syslog syslog(udpClient, SYSLOG_SERVER, SYSLOG_PORT, HOSTNAME, SYSLOG_MYAPPNAME, LOG_KERN);
Logger logger(udpClient, syslog);
#else
Logger logger;
#endif

WDTManager wdtManager(logger);
DisplayManager displayManager(logger, wdtManager);
WiFiConnectionManager wifiConnectionManager(logger, wdtManager);
OTAManager otaManager(logger, wdtManager);
SystemInfo systemInfo(logger, wakeupCount);
VoltageReader voltageReader(logger);
HTTPClientManager httpClientManager(logger, wdtManager, voltageReader, systemInfo, httpClient, sleepTime, lastChecksum, defined_color_type);

uint32_t fullStartTime;
uint32_t configLoadTime;

void basicInit() {
  fullStartTime = millis();
  ++wakeupCount;
  Serial.begin(115200);
  DEBUG_PRINT("Started");
}

void wakeupAndConnect() {
  displayManager.init();
  if (!wifiConnectionManager.connect()) {
    sleepTime = SECONDS_PER_HOUR * 1;
    error("WiFi connect/login unsuccessful.");
  }
  wifiClient.setBlockingReadTimeout(5000);
  systemInfo.logResetReason(lastChecksum);

#ifdef USE_WDT
  if (esp_reset_reason() == ESP_RST_TASK_WDT) {
    sleepTime = SECONDS_PER_HOUR * 1;
    error("Watchdog issue. Please report this to the developer.");
  }
#endif

  otaManager.init();

  voltageReader.read();
  ArduinoOTA.handle();
  httpClientManager.loadConfigFromWeb(configLoadTime, otaMode);
  ArduinoOTA.handle();
  if (voltageReader.getVoltageReal() > 0 && voltageReader.getVoltageReal() < VOLTAGE_MIN) {
    sleepTime = SECONDS_PER_HOUR * 1;
    error(String("Battery voltage too low: ") + String(voltageReader.getVoltageReal()) + " V\n" + "Minimum is: " + String(VOLTAGE_MIN) + " V\n" +
          "Please charge the battery and try again.");
  }
}

void disconnectAndHibernate() {
  logRuntimeStats();
  displayManager.stop();

#ifdef GHOST_HUNTING
  sleepTime = 15;
#else
  sleepTime -= (millis() - configLoadTime) / 1000;
  if (sleepTime < 10) {
    DEBUG_PRINT("SleepTime is too low (%d seconds), resetting to a sane value", sleepTime);
    sleepTime = 300;
  }
#endif
  DEBUG_PRINT("Going to hibernate for %d seconds", sleepTime);
  wdtManager.stop();

  wifiConnectionManager.disconnect();
  boardSpecificDone();
  espDeepSleep(sleepTime);
}

void error(String message) {
  strcpy(lastChecksum, "");
  DEBUG_PRINT("Displaying error: %s", message.c_str());
  displayManager.displayText(message + "\n\nRetrying after " + String(sleepTime / 60) + " minutes.", &DejaVu_Sans_Mono_16);
  disconnectAndHibernate();
}

void espDeepSleep(uint64_t seconds) {
  wdtManager.stop();
  TRACE_PRINT("Going to deep sleep for %lu s", seconds);
  esp_sleep_enable_timer_wakeup(seconds * uS_PER_S);
  esp_deep_sleep_start();
}

void logRuntimeStats() {
  TRACE_PRINT("logRuntimeStats()");
  DEBUG_PRINT("Total execution time: %lu ms", millis() - fullStartTime);
}

void setup() {
  basicInit();
  wdtManager.init();
  boardSpecificInit();
  wakeupAndConnect();

#ifdef GHOST_HUNTING
  DEBUG_PRINT("Ghost hunting mode enabled, not going to sleep");
#endif

  if (otaMode) {
    wdtManager.stop();
    DEBUG_PRINT("Running OTA loop on %s (%s.local)", WiFi.localIP().toString().c_str(), HOSTNAME);
    while (true) {
      ArduinoOTA.handle();
      delay(5);
    }
  };

  httpClientManager.showRawBitmapFrom_HTTP("/calendar/bitmap/epaper", 0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT);
  disconnectAndHibernate();
}

void loop() {
  // Shouldn't get here at all due to the deep sleep called in setup
}
