#include "display_manager.h"
#include <ArduinoOTA.h>

#include "board_config.h"
#include "logger.h"
#include "main.h"
#include "wdt_manager.h"

#ifdef SPI_BUS
#include <SPI.h>
#endif

#ifdef DISPLAY_TYPE_BW
#include <GxEPD2_BW.h>
extern GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display;
#endif

#ifdef DISPLAY_TYPE_GRAYSCALE
#ifdef USE_GRAYSCALE_BW_DISPLAY
#include <GxEPD2_4G_BW.h>
#else
#include <GxEPD2_4G_4G.h>
#endif
#endif

#ifdef DISPLAY_TYPE_3C
#include <GxEPD2_3C.h>
#endif

DisplayManager::DisplayManager(Logger& logger, WDTManager& wdtManager)
    : logger(logger), wdtManager(wdtManager) {
}

void DisplayManager::init() {
  logger.debug("Display setup start");
  logger.trace("CS=%d, DC=%d, RST=%d, BUSY=%d", CS_PIN, DC_PIN, RST_PIN, BUSY_PIN);

  delay(100);

#ifdef SPI_BUS
  SPIClass *spi = new SPIClass(SPI_BUS);
#ifdef REMAP_SPI
  Serial.println("remapping SPI");
  spi->begin(PIN_SPI_CLK, PIN_SPI_MISO, PIN_SPI_MOSI, PIN_SPI_SS);
#endif
  Serial.println("remapped");
  display.init(115200, false, 2, false, *spi, SPISettings(7000000, MSBFIRST, SPI_MODE0));
#else
  display.init(115200, false, 2, false);
#endif

  logger.debug("Display setup finished");
}

void DisplayManager::stop() {
  logger.debug("stopDisplay()");
  wdtManager.refresh();
  display.powerOff();
  wdtManager.refresh();
}

void DisplayManager::displayText(String message, const GFXfont *font) {
  display.setRotation(3);

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

  wdtManager.refresh();
  display.firstPage();
  do {
    ArduinoOTA.handle();
    display.fillScreen(GxEPD_WHITE);
    display.setCursor(x, y);
    display.print(message);
    wdtManager.refresh();
  } while (display.nextPage());
  display.refresh();
  wdtManager.refresh();
}
