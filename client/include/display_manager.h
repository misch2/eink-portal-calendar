#pragma once

#include <Adafruit_GFX.h>
#include <Arduino.h>

// Forward declarations
class Logger;
class WDTManager;

class DisplayManager {
 private:
  Logger& logger;
  WDTManager& wdtManager;

 public:
  DisplayManager(Logger& logger, WDTManager& wdtManager);

  void init();
  void stop();
  void displayText(String message, const GFXfont* font = nullptr);
};
