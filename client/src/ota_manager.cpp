#include "ota_manager.h"

#include <ArduinoOTA.h>

#include "board_config.h"
#include "logger.h"
#include "wdt_manager.h"

OTAManager::OTAManager(Logger& logger, WDTManager& wdtManager) : logger(logger), wdtManager(wdtManager) {}

void OTAManager::init() {
  ArduinoOTA.setHostname(HOSTNAME);
  ArduinoOTA.onStart([this]() {
    wdtManager.stop();
    logger.debug("OTA start");
  });
  ArduinoOTA.onEnd([this]() {
    logger.debug("OTA end");
  });

  ArduinoOTA.begin();
  logger.debug("OTA: Ready on %s.local", HOSTNAME);
}

void OTAManager::loop() { ArduinoOTA.handle(); }