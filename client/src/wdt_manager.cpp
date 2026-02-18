#include "wdt_manager.h"
#include "board_config.h"
#include "logger.h"

#ifdef USE_WDT
#include <esp_task_wdt.h>
#endif

WDTManager::WDTManager(Logger& logger) : logger(logger), enabled(false) {
}

void WDTManager::init() {
#ifdef USE_WDT
  logger.debug("Configuring WDT for %d seconds", WDT_TIMEOUT);
  esp_task_wdt_init(WDT_TIMEOUT, true);
  esp_task_wdt_add(NULL);
  enabled = true;
#endif
}

void WDTManager::refresh() {
#ifdef USE_WDT
  if (enabled) {
    logger.trace("(WDT ping)");
    esp_task_wdt_reset();
  }
#endif
}

void WDTManager::stop() {
#ifdef USE_WDT
  if (enabled) {
    logger.debug("Stopping WDT...");
    esp_task_wdt_deinit();
    enabled = false;
  }
#endif
}
