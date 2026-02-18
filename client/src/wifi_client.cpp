#include "wifi_client.h"
#include <Arduino.h>
#include <ArduinoOTA.h>

#include "board_config.h"
#include "logger.h"
#include "version.h"
#include "wdt_manager.h"

#ifdef USE_WIFI_MANAGER
#include <WiFiManager.h>
extern WiFiManager wifiManager;
#endif

// WiFiClientWithBlockingReads implementation
int WiFiClientWithBlockingReads::blocking_read(uint8_t *buffer, size_t bytes) {
  int remain = bytes;
  uint32_t start = millis();

  while ((WiFiClient::connected() || WiFiClient::available()) && (remain > 0)) {
    ArduinoOTA.handle();
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

void WiFiClientWithBlockingReads::setBlockingReadTimeout(uint32_t timeout) { 
  blockingReadTimeout = timeout; 
}

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

int WiFiClientWithBlockingReads::read(uint8_t *buf, size_t size) {
  int res = blocking_read(buf, size);
  return res;
}

// WiFiConnectionManager implementation
WiFiConnectionManager::WiFiConnectionManager(Logger& logger, WDTManager& wdtManager)
    : logger(logger), wdtManager(wdtManager) {
}

bool WiFiConnectionManager::connect() {
  bool res;

  logger.debug("Connecting to WiFi");
  unsigned long start = millis();

#ifdef USE_WIFI_MANAGER
  wdtManager.stop();
  wifiManager.setHostname(HOSTNAME);
  wifiManager.setConnectRetries(3);
  wifiManager.setConnectTimeout(15);
  wifiManager.setConfigPortalTimeout(10 * 60);
  res = wifiManager.autoConnect();
  wdtManager.init();
  if (!res) {
    logger.debug("Failed to connect");
    disconnect();
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

void WiFiConnectionManager::disconnect() {
  logger.trace("Disconnecting WiFi");

  unsigned long start = millis();
  wdtManager.refresh();

  WiFi.persistent(false);
  WiFi.mode(WIFI_OFF);
  WiFi.persistent(true);

  logger.debug("WiFi shutdown took %lu ms", millis() - start);
}
