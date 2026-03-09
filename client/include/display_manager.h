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
  WDTManager& wdt;
  OTAManager& ota;
  static const uint16_t serverByteToGxEPDColor[8];
  uint32_t startTime;

 public:
  DisplayManager(Logger& logger, WDTManager& wdtManager, OTAManager& otaManager);

  void init();
  void stop();
  void displayText(String message, const GFXfont* font = nullptr);
  int displayWidth();
  int displayHeight();

  // Bitmap drawing methods
  int bytesPerRow();
  void beginBitmapDraw();
  void drawBitmapRow(unsigned char* data, int16_t y);
  bool nextPageBitmapDraw();
  void endBitmapDraw();
};
