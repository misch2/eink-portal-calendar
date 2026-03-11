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

const uint16_t DisplayManager::serverByteToGxEPDColor[8] = {
    // 1-bit (2 combinations) variants
    GxEPD_WHITE,  // 0 = white
    GxEPD_BLACK,  // 1 = black
    // 2-bit (4 combinations) variants
    GxEPD_RED,     // 2 = red
    GxEPD_YELLOW,  // 3 = yellow
    // 3-bit (8 combinations) variants
    GxEPD_BLUE,    // 4 = blue
    GxEPD_GREEN,   // 5 = green
    GxEPD_ORANGE,  // 6 = orange
    GxEPD_WHITE    // 7 = white (fallback)
};

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
    font = &Open_Sans_Regular_16;
  }

  const int16_t margin = 10;
  const int16_t lineSpacing = 4;
  const int16_t titleGap = 36;
  const GFXfont* titleFont = &Open_Sans_Regular_24;
  const char* titleText = "Error";

  // Measure title
  int16_t tbx, tby;
  uint16_t tbw, tbh;
  display.setFont(titleFont);
  display.getTextBounds(titleText, 0, 0, &tbx, &tby, &tbw, &tbh);
  int16_t titleHeight = tbh;
  int16_t titleBaselineOffset = -tby;  // distance from top of bounding box to baseline

  // Split message into lines
  String lines[20];
  int lineCount = 0;
  int start = 0;
  for (int i = 0; i <= (int)message.length(); i++) {
    if (i == (int)message.length() || message[i] == '\n') {
      lines[lineCount++] = message.substring(start, i);
      start = i + 1;
      if (lineCount >= 20) break;
    }
  }

  // Word-wrap lines that exceed the available width
  int16_t maxWidth = display.width() - 2 * margin;
  String wrappedLines[40];
  int wrappedCount = 0;
  display.setFont(font);
  for (int i = 0; i < lineCount && wrappedCount < 40; i++) {
    if (lines[i].length() == 0) {
      wrappedLines[wrappedCount++] = "";
      continue;
    }
    display.getTextBounds(lines[i], 0, 0, &tbx, &tby, &tbw, &tbh);
    if ((int16_t)tbw <= maxWidth) {
      wrappedLines[wrappedCount++] = lines[i];
    } else {
      // Wrap by words
      String remaining = lines[i];
      while (remaining.length() > 0 && wrappedCount < 40) {
        String candidate = "";
        int pos = 0;
        int lastSpace = -1;
        while (pos < (int)remaining.length()) {
          char c = remaining[pos];
          String test = candidate + c;
          display.getTextBounds(test, 0, 0, &tbx, &tby, &tbw, &tbh);
          if ((int16_t)tbw > maxWidth && candidate.length() > 0) break;
          candidate = test;
          if (c == ' ') lastSpace = pos;
          pos++;
        }
        if (pos < (int)remaining.length() && lastSpace > 0) {
          wrappedLines[wrappedCount++] = remaining.substring(0, lastSpace);
          remaining = remaining.substring(lastSpace + 1);
        } else {
          wrappedLines[wrappedCount++] = candidate;
          remaining = remaining.substring(pos);
        }
      }
    }
  }

  // Measure body line height
  display.setFont(font);
  display.getTextBounds("Ag", 0, 0, &tbx, &tby, &tbw, &tbh);
  int16_t bodyLineHeight = tbh + lineSpacing;
  int16_t bodyBaselineOffset = -tby;

  // Total height: title + gap + body lines
  int16_t totalHeight = titleHeight + titleGap + wrappedCount * bodyLineHeight;
  int16_t startY = (display.height() - totalHeight) / 2;
  if (startY < margin) startY = margin;

  wdt.ping();
  display.firstPage();
  do {
    ota.loop();
    display.fillScreen(GxEPD_WHITE);

    // Draw title centered
    display.setFont(titleFont);
    display.setTextColor(GxEPD_BLACK);
    display.getTextBounds(titleText, 0, 0, &tbx, &tby, &tbw, &tbh);
    int16_t tx = (display.width() - tbw) / 2 - tbx;
    display.setCursor(tx, startY + titleBaselineOffset);
    display.print(titleText);

    // Draw body lines, each horizontally centered
    display.setFont(font);
    display.setTextColor(GxEPD_BLACK);
    int16_t y = startY + titleHeight + titleGap + bodyBaselineOffset;
    for (int i = 0; i < wrappedCount; i++) {
      if (wrappedLines[i].length() > 0) {
        display.getTextBounds(wrappedLines[i], 0, 0, &tbx, &tby, &tbw, &tbh);
        int16_t lx = (display.width() - tbw) / 2 - tbx;
        if (lx < margin) lx = margin;
        display.setCursor(lx, y);
        display.print(wrappedLines[i]);
      }
      y += bodyLineHeight;
    }

    wdt.ping();
  } while (display.nextPage());
  wdt.ping();
}

int DisplayManager::displayWidth() { return display.width(); }

int DisplayManager::displayHeight() { return display.height(); }

int DisplayManager::bytesPerRow() {
#ifdef DISPLAY_TYPE_BW
  return DISPLAY_WIDTH / 8;  // 8 pixels per byte
#endif
#if defined(DISPLAY_TYPE_3C) || defined(DISPLAY_TYPE_4C)
  return DISPLAY_WIDTH / 4;  // 4 pixels per byte
#endif
#ifdef DISPLAY_TYPE_7C
  return DISPLAY_WIDTH / 2;  // 2 pixels per byte
#endif
}

void DisplayManager::beginBitmapDraw() {
  startTime = millis();
  display.fillScreen(GxEPD_WHITE);
  display.firstPage();
}

void DisplayManager::drawBitmapRow(unsigned char* data, int16_t y) {
  int16_t w = displayWidth();

  int byteIndex = 0;
  int16_t x = 0;
  uint16_t color;
  while (x < w) {
    uint8_t byte = data[byteIndex];

#ifdef DISPLAY_TYPE_BW
    // 1 bit per pixel = 8 pixels per byte
    color = serverByteToGxEPDColor[(byte >> 7) & 0x01];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[(byte >> 6) & 0x01];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[(byte >> 5) & 0x01];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[(byte >> 4) & 0x01];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[(byte >> 3) & 0x01];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[(byte >> 2) & 0x01];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[(byte >> 1) & 0x01];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[byte & 0x01];
    display.drawPixel(x, y, color);
    x++;
#endif
#if defined(DISPLAY_TYPE_3C) || defined(DISPLAY_TYPE_4C)
    // 2 bits per pixel = 4 pixels per byte
    color = serverByteToGxEPDColor[(byte >> 6) & 0x03];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[(byte >> 4) & 0x03];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[(byte >> 2) & 0x03];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[byte & 0x03];
    display.drawPixel(x, y, color);
    x++;
#endif
#ifdef DISPLAY_TYPE_7C
    // 3 bits per pixel = 2 pixels per byte (lower 3 bits used)
    color = serverByteToGxEPDColor[(byte >> 4) & 0x07];
    display.drawPixel(x, y, color);
    x++;
    color = serverByteToGxEPDColor[byte & 0x07];
    display.drawPixel(x, y, color);
    x++;
#endif
    byteIndex++;
  }
}

bool DisplayManager::nextPageBitmapDraw() {
  wdt.ping();
  logger.debug("Refreshing display page");
  return display.nextPage();
}

void DisplayManager::endBitmapDraw() {
  //
  logger.debug("Display refresh time: %lu ms", millis() - startTime);
}
