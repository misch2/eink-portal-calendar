// generic libraries
#include <Adafruit_GFX.h>
#include <Arduino.h>
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

#include "debug.h"
#include "display_manager.h"
#include "http_client_manager.h"
#include "logger.h"
#include "main.h"
#include "ota_manager.h"
#include "system_info.h"
#include "version.h"
#include "voltage.h"
#include "wdt_manager.h"
#include "wifi_client.h"

#ifdef DISPLAY_TYPE_BW
#include <GxEPD2_BW.h>
const char* defined_color_type = "BW";
#endif
#ifdef DISPLAY_TYPE_GRAYSCALE
const char* defined_color_type = "4G";
#ifdef USE_GRAYSCALE_BW_DISPLAY
#include <GxEPD2_4G_BW.h>
#else
#include <GxEPD2_4G_4G.h>
#endif
#endif
#ifdef DISPLAY_TYPE_3C
const char* defined_color_type = "3C";
#include <GxEPD2_3C.h>
#endif

// macro to define "display" variable dynamically with the right type
DISPLAY_INSTANCE

/* RTC vars (survives deep sleep) */
RTC_DATA_ATTR int wakeupCount = 0;
RTC_DATA_ATTR char lastChecksum[64 + 1] = "<not_defined_yet>";

#define SLEEP_TIME_DEFAULT (SECONDS_PER_MINUTE * 5)

// remotely configurable variables (via JSON)
int nextSleepTime = SLEEP_TIME_DEFAULT;
bool otaDebugModeNoSleep = false;

// ordinary vars
#ifdef USE_WIFI_MANAGER
WiFiManager wifiManager;
#endif

WiFiClientWithBlockingReads wifiClient;

#ifdef SYSLOG_SERVER
WiFiUDP udpClient;
Syslog syslog(udpClient, SYSLOG_SERVER, SYSLOG_PORT, HOSTNAME, SYSLOG_MYAPPNAME, LOG_KERN);
Logger logger(udpClient, syslog);
#else
Logger logger;
#endif

WDTManager wdtManager(logger);
OTAManager otaManager(logger, wdtManager);
DisplayManager displayManager(logger, wdtManager, otaManager);
WiFiConnectionManager wifiConnectionManager(logger, wdtManager);
SystemInfo systemInfo(logger, wakeupCount);
VoltageReader voltageReader(logger);
HTTPClientManager httpClientManager(logger, wdtManager, otaManager, voltageReader, systemInfo, displayManager, nextSleepTime, lastChecksum, defined_color_type);

class TimingInfo {
 public:
  uint32_t fullStartTime;
  uint32_t configLoadTime;

  TimingInfo() : fullStartTime(0), configLoadTime(0) {}

  void logStats() { DEBUG_PRINT("Total execution time: %lu ms", millis() - fullStartTime); }
};
TimingInfo timing;

void minimalHardwareInit() {
  timing.fullStartTime = millis();
  ++wakeupCount;
  Serial.begin(115200);
  wdtManager.init();
  DEBUG_PRINT("Started");
}

void wakeupDisplayAndConnectWiFi() {
  displayManager.init();

  if (!wifiConnectionManager.init()) {
    nextSleepTime = SECONDS_PER_HOUR * 1;
    showErrorOnDisplay("WiFi connect/login unsuccessful.");
  }

  otaManager.init();
  wifiClient.setOTAManager(&otaManager);
  wifiClient.setBlockingReadTimeout(5000);

  systemInfo.logResetReason(lastChecksum);

#ifdef USE_WDT
  if (esp_reset_reason() == ESP_RST_TASK_WDT) {
    nextSleepTime = SECONDS_PER_HOUR * 1;
    showErrorOnDisplay("Watchdog issue. Please report this to the developer.");
  }
#endif

  voltageReader.read();
  otaManager.loop();
  httpClientManager.loadConfigFromWeb(timing.configLoadTime, otaDebugModeNoSleep);
  otaManager.loop();
  if (voltageReader.getVoltageReal() > 0 && voltageReader.getVoltageReal() < VOLTAGE_MIN) {
    nextSleepTime = SECONDS_PER_HOUR * 1;
    showErrorOnDisplay(String("Battery voltage too low: ") + String(voltageReader.getVoltageReal()) + " V\n" + "Minimum is: " + String(VOLTAGE_MIN) + " V\n" +
                       "Please charge the battery and try again.");
  }
}

void disconnectWiFiAndHibernateAll() {
  timing.logStats();
  displayManager.stop();
  wdtManager.stop();

  nextSleepTime -= (millis() - timing.configLoadTime) / 1000;
  if (nextSleepTime < 10) {
    DEBUG_PRINT("SleepTime is too low (%d seconds), resetting to a sane value", nextSleepTime);
    nextSleepTime = 300;
  }

  DEBUG_PRINT("Going to hibernate for %d seconds", nextSleepTime);

  wifiConnectionManager.stop();
  boardSpecificDone();
  espDeepSleep(nextSleepTime);
}

void showErrorOnDisplay(String message) {
  strcpy(lastChecksum, "");
  DEBUG_PRINT("Displaying error: %s", message.c_str());
  displayManager.displayText(message + "\n\nRetrying after " + String(nextSleepTime / 60) + " minutes.", &DejaVu_Sans_Mono_16);
  disconnectWiFiAndHibernateAll();
}

void espDeepSleep(uint64_t seconds) {
  wdtManager.stop();
  TRACE_PRINT("Going to deep sleep for %lu s", seconds);
  esp_sleep_enable_timer_wakeup(seconds * uS_PER_S);
  esp_deep_sleep_start();
}

void setup() {
  minimalHardwareInit();

  boardSpecificInit();
  wakeupDisplayAndConnectWiFi();

  if (otaDebugModeNoSleep) {
    DEBUG_PRINT("Running OTA loop on %s (%s.local)", WiFi.localIP().toString().c_str(), HOSTNAME);
    wdtManager.stop();
    while (true) {
      otaManager.loop();
      delay(5);
    }
  };
}

void loop() {
  httpClientManager.showRawBitmapFrom_HTTP("/calendar/bitmap/epaper", 0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT);

  disconnectWiFiAndHibernateAll();
}
