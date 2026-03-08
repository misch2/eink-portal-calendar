#include "display_manager.h"

#include <ArduinoOTA.h>

#include "hw_config.h"
#include "logger.h"
#include "main.h"
#include "ota_manager.h"
#include "wdt_manager.h"

#ifdef SPI_BUS
#include <SPI.h>
#endif

extern DISPLAY_CLASS_TYPE display;

DisplayManager::DisplayManager(Logger& logger, WDTManager& wdtManager, OTAManager& otaManager) : logger(logger), wdt(wdtManager), ota(otaManager) {}

void DisplayManager::init() {
  logger.debug("Display setup start");
  logger.trace("CS=%d, DC=%d, RST=%d, BUSY=%d", CS_PIN, DC_PIN, RST_PIN, BUSY_PIN);

  delay(100);

#ifdef SPI_BUS
  SPIClass* spi = new SPIClass(SPI_BUS);
#ifdef REMAP_SPI
  logger.debug("remapping SPI");
  spi->begin(PIN_SPI_CLK, PIN_SPI_MISO, PIN_SPI_MOSI, PIN_SPI_SS);
#endif
  logger.debug("remapped, now initialising SPI");
  display.init(115200, false, 2, false, *spi, SPISettings(7000000, MSBFIRST, SPI_MODE0));
#else
  display.init(115200, false, 2, false);
#endif

  logger.debug("Display setup finished");
}

void DisplayManager::stop() {
  logger.debug("stopDisplay()");
  wdt.ping();
  display.powerOff();
  wdt.ping();
}

void DisplayManager::displayText(String message, const GFXfont* font) {
  display.setRotation(DISPLAY_ROTATION);  // see hw_config.h for details

  if (font == nullptr) {
    font = &Open_Sans_Regular_24;
  }

  display.setFont(font);
  display.setTextColor(GxEPD_BLACK);
  int16_t tbx, tby;
  uint16_t tbw, tbh;
  display.getTextBounds(message, 0, 0, &tbx, &tby, &tbw, &tbh);
  uint16_t x = ((display.width() - tbw) / 2) - tbx;
  uint16_t y = ((display.height() - tbh) / 2) - tby;

  wdt.ping();
  display.firstPage();
  do {
    ota.loop();
    display.fillScreen(GxEPD_WHITE);
    display.setCursor(x, y);
    display.print(message);
    wdt.ping();
  } while (display.nextPage());
  wdt.ping();
}

int DisplayManager::displayWidth() { return display.width(); }

int DisplayManager::displayHeight() { return display.height(); }

int DisplayManager::bytesPerRow() {
#ifdef DISPLAY_TYPE_3C
  return DISPLAY_BUFFER_SIZE * 2;
#else
  return DISPLAY_BUFFER_SIZE;
#endif
}

void DisplayManager::beginBitmapDraw() {
#ifdef DISPLAY_USE_PIXEL_DRAW
  display.fillScreen(GxEPD_WHITE);
#endif
}

void DisplayManager::drawBitmapRow(unsigned char* data, int16_t x, int16_t y, int16_t w) {
#ifdef DISPLAY_TYPE_BW
  display.writeImage(data, x, y, w, 1);
#endif

#ifdef DISPLAY_TYPE_3C
  unsigned char* monoData = data;
  unsigned char* colorData = data + DISPLAY_BUFFER_SIZE;
#ifdef DISPLAY_USE_PIXEL_DRAW
  for (int i = 0; i < DISPLAY_BUFFER_SIZE; i++) {
    uint8_t monoByte = monoData[i];
    uint8_t colorByte = colorData[i];

    for (int bit = 7; bit >= 0; bit--) {
      uint16_t color;
      bool isBlack = (monoByte & (1 << bit)) == 0;
      bool isColor = (colorByte & (1 << bit)) == 0;

      if (isBlack) {
        color = GxEPD_BLACK;
      } else if (isColor) {
        color = GxEPD_RED;
      } else {
        color = GxEPD_WHITE;
      }

      int pixelX = x + (i * 8 + (7 - bit));
      display.drawPixel(pixelX, y, color);
    }
  }
#else
  display.writeImage(monoData, colorData, x, y, w, 1);
#endif
#endif

#ifdef DISPLAY_TYPE_4C
  display.writeNative(data, nullptr, x, y, w, 1, false, false, false);
#endif
}

void DisplayManager::endBitmapDraw() {
  logger.debug("Refreshing display");
  wdt.ping();
  uint32_t startTime = millis();
#ifdef DISPLAY_USE_PIXEL_DRAW
  display.display();
#else
  display.refresh();
#endif
  logger.debug("Display refresh time: %lu ms", millis() - startTime);
}
