#pragma once

#include <Adafruit_GFX.h>
#include <Arduino.h>

// Forward declarations
class Logger;
class WDTManager;
class OTAManager;

class DisplayManager {
 private:
  Logger& logger;
  WDTManager& wdtManager;
  OTAManager& otaManager;

 public:
  DisplayManager(Logger& logger, WDTManager& wdtManager, OTAManager& otaManager);

  void init();
  void stop();
  void displayText(String message, const GFXfont* font = nullptr);
  int displayWidth();
  int displayHeight();
};
