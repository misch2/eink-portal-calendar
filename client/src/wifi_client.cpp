#include "wifi_client.h"

#include <Arduino.h>
#include <ArduinoOTA.h>

#include "display_manager.h"
#include "fonts/Open_Sans_Regular_16.h"
#include "fonts/Open_Sans_Regular_24.h"
#include "hw_config.h"
#include "logger.h"
#include "ota_manager.h"
#include "version.h"
#include "wdt_manager.h"

#ifdef USE_WIFI_MANAGER
#include <WiFiManager.h>
extern WiFiManager wifiManager;
#endif
extern DisplayManager displayManager;

#ifdef USE_WIFI_MANAGER
namespace {
WiFiConnectionManager* activeConnectionManager = nullptr;

void showConfigPortalOnDisplay(WiFiManager* manager) {
  if (activeConnectionManager == nullptr) {
    return;
  }

  activeConnectionManager->handleConfigPortalStarted(manager->getConfigPortalSSID(), WiFi.softAPIP());
}
}  // namespace
#endif

// WiFiClientWithBlockingReads implementation
void WiFiClientWithBlockingReads::setOTAManager(OTAManager* manager) { otaManager = manager; }

int WiFiClientWithBlockingReads::blocking_read(uint8_t* buffer, size_t bytes) {
  int remain = bytes;
  uint32_t start = millis();

  while ((WiFiClient::connected() || WiFiClient::available()) && (remain > 0)) {
    if (otaManager) {
      otaManager->loop();
    }
    if (WiFiClient::available()) {
      uint8_t data = 0;
      int res = WiFiClient::read(&data, 1);
      if (res <= 0) {
        return res;
      }
      if (buffer) {
        *buffer++ = data;
      }
      remain--;
    } else {
      delay(1);
    }
    if (millis() - start > blockingReadTimeout) {
      return -1;
    }
  }

  return bytes - remain;
}

void WiFiClientWithBlockingReads::setBlockingReadTimeout(uint32_t timeout) { blockingReadTimeout = timeout; }

int WiFiClientWithBlockingReads::read() {
  uint8_t data;
  int res = blocking_read(&data, 1);

  if (res < 0) {
    return res;
  } else if (res == 0) {
    return -1;
  }

  return data;
}

int WiFiClientWithBlockingReads::read(uint8_t* buf, size_t size) {
  int res = blocking_read(buf, size);
  return res;
}

// WiFiConnectionManager implementation
WiFiConnectionManager::WiFiConnectionManager(Logger& logger, WDTManager& wdtManager) : logger(logger), wdtManager(wdtManager) {}

bool WiFiConnectionManager::init() {
  bool res;

  logger.debug("Connecting to WiFi");
  unsigned long start = millis();
  configPortalStarted = false;
  lastConfigPortalSsid = "";
  lastConfigPortalIp = IPAddress();

#ifdef USE_WIFI_MANAGER
  wdtManager.stop();
  wifiManager.setHostname(HOSTNAME);
  wifiManager.setConnectRetries(3);
  wifiManager.setConnectTimeout(15);
  wifiManager.setConfigPortalTimeout(10 * 60);
  activeConnectionManager = this;
  wifiManager.setAPCallback(showConfigPortalOnDisplay);
  res = wifiManager.autoConnect();
  activeConnectionManager = nullptr;
  wdtManager.init();
  if (!res) {
    logger.debug("Failed to connect");
    stop();
    return false;
  }
#else
  wdtManager.refresh();
  WiFi.setHostname(HOSTNAME);
#ifdef NETWORK_IP_ADDRESS
  WiFi.config(NETWORK_IP_ADDRESS, NETWORK_IP_GATEWAY, NETWORK_IP_SUBNET, NETWORK_IP_DNS);
#endif
#ifdef WIFI_SSID
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
#endif
  while (WiFi.status() != WL_CONNECTED) {
    delay(100);
  }
#endif

  logger.debug("---");
  logger.debug("Firmware version: %s", String(FIRMWARE_VERSION).c_str());
  logger.debug("Connected to WiFi in %lu ms", millis() - start);
  logger.debug("IP address: %s", WiFi.localIP().toString().c_str());
  logger.debug("MAC address: %s", WiFi.macAddress().c_str());

  return true;
}

void WiFiConnectionManager::handleConfigPortalStarted(const String& ssid, const IPAddress& ip) {
  configPortalStarted = true;
  lastConfigPortalSsid = ssid;
  lastConfigPortalIp = ip;

  String message = "Connect to AP:\n" + lastConfigPortalSsid + "\n\nOpen:\nhttp://" + lastConfigPortalIp.toString();
  displayManager.displayText("WiFi Setup", message, &Open_Sans_Regular_24);
}

String WiFiConnectionManager::getAutoconnectFailureMessage() const {
#ifdef USE_WIFI_MANAGER
  if (configPortalStarted) {
    return "WiFi setup timed out.\n\nConnect to AP:\n" + lastConfigPortalSsid + "\n\nOpen:\nhttp://" + lastConfigPortalIp.toString();
  }
#endif
  return "WiFi connect/login unsuccessful.";
}

void WiFiConnectionManager::stop() {
  logger.trace("Disconnecting WiFi");

  unsigned long start = millis();
  wdtManager.ping();

  WiFi.persistent(false);
  WiFi.mode(WIFI_OFF);
  WiFi.persistent(true);

  logger.debug("WiFi shutdown took %lu ms", millis() - start);
}
