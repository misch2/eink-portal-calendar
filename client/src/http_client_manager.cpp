#include "http_client_manager.h"
#include <ArduinoJson.h>
#include <ArduinoOTA.h>

#include "board_config.h"
#include "logger.h"
#include "main.h"
#include "system_info.h"
#include "version.h"
#include "voltage.h"
#include "wdt_manager.h"

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

#define SLEEP_TIME_DEFAULT (SECONDS_PER_MINUTE * 5)
#define SLEEP_TIME_TEMPORARY_ERROR (SECONDS_PER_MINUTE * 5)
#define SLEEP_TIME_PERMANENT_ERROR (SECONDS_PER_HOUR * 1)

HTTPClientManager::HTTPClientManager(Logger& logger, WDTManager& wdtManager, VoltageReader& voltageReader,
                                   SystemInfo& systemInfo, HttpClient& httpClient, int& sleepTime,
                                   char* lastChecksum, const char* defined_color_type)
    : logger(logger), wdtManager(wdtManager), voltageReader(voltageReader),
      systemInfo(systemInfo), httpClient(httpClient), sleepTime(sleepTime),
      lastChecksum(lastChecksum), defined_color_type(defined_color_type) {
}

String HTTPClientManager::textStatusCode(int statusCode) {
  if (statusCode == HTTP_ERROR_CONNECTION_FAILED) {
    return "HTTP_ERROR_CONNECTION_FAILED - Could not connect to the server";
  } else if (statusCode == HTTP_ERROR_API) {
    return "HTTP_ERROR_API -  Usually indicates your code is using the class incorrectly";
  } else if (statusCode == HTTP_ERROR_TIMED_OUT) {
    return "HTTP_ERROR_TIMED_OUT - Spent too long waiting for a reply";
  } else if (statusCode == HTTP_ERROR_INVALID_RESPONSE) {
    return "HTTP_ERROR_INVALID_RESPONSE - The response from the server is invalid, is it definitely an HTTP server?";
  } else {
    return "";
  }
}

void HTTPClientManager::startHttpGetRequest(String path) {
  int statusCode = 0;
  bool ok = false;
  for (int attempt = 1; attempt <= 3; attempt++) {
    wdtManager.refresh();
    if (attempt > 1) {
      logger.debug("Retrying download, attempt #%d", attempt);
    }

    logger.debug(">>> GET %s", path.c_str());

    httpClient.beginRequest();
    httpClient.get(path);
    httpClient.endRequest();

    statusCode = httpClient.responseStatusCode();
    logger.debug("<<< HTTP response code: %d (%s)", statusCode, textStatusCode(statusCode).c_str());
    if (statusCode == 200) {
      ok = true;
      break;
    } else {
      logger.debug("HTTP response code: %d (%s), attempt #%d", statusCode, textStatusCode(statusCode).c_str(), attempt);
      httpClient.stop();
      delay(1000);
      continue;
    }
  }

  logger.debug("Connection status: connected=%d, available=%ld, headerAvailable=%d", 
              httpClient.connected(), httpClient.available(), httpClient.headerAvailable());
  logger.debug("Skipping response headers...");
  httpClient.skipResponseHeaders();
  logger.debug("Connection status: connected=%d, available=%ld, headerAvailable=%d", 
              httpClient.connected(), httpClient.available(), httpClient.headerAvailable());

  if (!ok) {
    sleepTime = SLEEP_TIME_TEMPORARY_ERROR;
    extern void error(String message);  // TODO: Should be passed as parameter or callback
    error(String("Unexpected HTTP response.\nURL: ") +
          String(CALENDAR_URL_HOST) + ":" + String(CALENDAR_URL_PORT) + path + "\n\nResponse code: " +
          String(statusCode) + "\n");
  }
}

void HTTPClientManager::loadConfigFromWeb(uint32_t& configLoadTime, bool& otaMode) {
  String path = "/config?mac=" + WiFi.macAddress()
                + "&adc=" + String(voltageReader.getAdcRaw())
                + "&v=" + String(voltageReader.getVoltageReal())
                + "&vmin=" + String(VOLTAGE_MIN)
                + "&vmax=" + String(VOLTAGE_MAX)
                + "&vlmin=" + String(VOLTAGE_LINEAR_MIN)
                + "&vlmax=" + String(VOLTAGE_LINEAR_MAX)
                + "&w=" + String(DISPLAY_WIDTH) + "&h=" + String(DISPLAY_HEIGHT)
                + "&c=" + String(defined_color_type)
                + "&fw=" + String(FIRMWARE_VERSION)
                + "&reset=" + systemInfo.resetReasonAsString()
                + "&wakeup=" + systemInfo.wakeupReasonAsString();

  configLoadTime = millis();
  startHttpGetRequest(path);
  String jsonText = httpClient.responseBody();

  DynamicJsonDocument response(1000);
  DeserializationError errorStr = deserializeJson(response, jsonText);

  if (errorStr) {
    logger.debug("error: %s", errorStr.c_str());
    sleepTime = SLEEP_TIME_TEMPORARY_ERROR;
    extern void error(String message);
    error("Can't parse response");
  }

  int tmpi = response["sleep"];
  logger.trace("sleepTime from JSON: %d", tmpi);
  if (tmpi != 0) {
    sleepTime = tmpi;
  }

  bool tmpb = response["ota_mode"];
  logger.trace("otaMode from JSON: %d", tmpb);
  otaMode = tmpb;
  if (otaMode) {
    logger.debug("Permanent OTA mode enabled in remote config");
    if (esp_reset_reason() == ESP_RST_SW || esp_reset_reason() == ESP_RST_DEEPSLEEP) {
      logger.debug("^ but last reset was a software one => not running OTA loop.");
      logger.debug("To force OTA mode again, reset the device manually.");
      otaMode = false;
    }
  }
}

int HTTPClientManager::httpReadStringUntil(char terminator, String &result) {
  result = "";

  wdtManager.refresh();
  int bytes = 0;
  while (httpClient.connected() && httpClient.available()) {
    ArduinoOTA.handle();
    int c = httpClient.read();
    if (c < 0) {
      logger.debug("Premature end of HTTP response");
      break;
    }
    bytes++;
    if (c == terminator) {
      break;
    }
    result += (char)c;
  }
  return bytes;
}

void HTTPClientManager::showRawBitmapFrom_HTTP(const char *path, int16_t x, int16_t y, int16_t w, int16_t h) {
#ifdef DISPLAY_TYPE_BW
  static unsigned char input_row_mono_buffer[DISPLAY_BUFFER_SIZE];
  static unsigned char input_row_color_buffer[DISPLAY_BUFFER_SIZE];
#endif

#ifdef DISPLAY_TYPE_3C
  static unsigned char input_row_mono_buffer[DISPLAY_BUFFER_SIZE];
  static unsigned char input_row_color_buffer[DISPLAY_BUFFER_SIZE];
#endif

  uint32_t startTime = millis();
  if ((x >= display.width()) || (y >= display.height())) {
    return;
  }

  String partial_uri = String(path) + "?mac=" + WiFi.macAddress();

  bool ok = false;
  String newChecksum = String(lastChecksum);
  for (int attempt = 1; attempt <= 5; attempt++) {
    if (attempt > 1) {
      logger.debug("Retrying download, attempt #%d", attempt);
    }

    startHttpGetRequest(partial_uri);
    logger.debug("Expected content length (from headers): %d", httpClient.contentLength());

    uint32_t bytes_read = 0;
    logger.debug("Reading bitmap header");
    wdtManager.refresh();
    String line;
    bytes_read += httpReadStringUntil('\n', line);
    logger.debug("Read %d bytes, line: %s", bytes_read, line.c_str());
    if (line != "MM") {
      sleepTime = SLEEP_TIME_PERMANENT_ERROR;
      extern void error(String message);
      error(String("Invalid bitmap received, doesn't start with a magic sequence:\n") + "Line: " + line + "\n");
    }

    logger.debug("Reading checksum");
    wdtManager.refresh();
    bytes_read += httpReadStringUntil('\n', line);

#ifndef GHOST_HUNTING
    logger.debug("Last checksum was: %s", lastChecksum);
    logger.debug("New checksum is: %s", line.c_str());
    if (line == String(lastChecksum)) {
      logger.debug("Not refreshing, image is unchanged");
      return;
    } else {
      logger.debug("Checksum has changed, reading image and refreshing the display");
    }
#endif

    newChecksum = line;
    logger.debug("Reading image data for %d rows", h);

    for (uint16_t row = 0; row < h; row++) {
      wdtManager.refresh();

      uint32_t local_bytes_read = 0;
#ifdef DISPLAY_TYPE_BW
      local_bytes_read = httpClient.read(input_row_mono_buffer, DISPLAY_BUFFER_SIZE);
      if (local_bytes_read != DISPLAY_BUFFER_SIZE) {
        logger.debug("WARNING(1): bytes read != bytes expected, skipped %d bytes on row %d", 
                    DISPLAY_BUFFER_SIZE - local_bytes_read, row);
#ifndef GHOST_HUNTING
        break;
#endif
      }
      bytes_read += local_bytes_read;
#endif

#ifdef DISPLAY_TYPE_3C
      local_bytes_read = httpClient.read(input_row_mono_buffer, DISPLAY_BUFFER_SIZE);
      if (local_bytes_read != DISPLAY_BUFFER_SIZE) {
        logger.debug("WARNING(2): bytes read != bytes expected, skipped %d bytes on row %d", 
                    DISPLAY_BUFFER_SIZE - local_bytes_read, row);
#ifndef GHOST_HUNTING
        break;
#endif
      }
      bytes_read += local_bytes_read;
      local_bytes_read = httpClient.read(input_row_color_buffer, DISPLAY_BUFFER_SIZE);
      if (local_bytes_read != DISPLAY_BUFFER_SIZE) {
        logger.debug("WARNING(3): bytes read != bytes expected, skipped %d bytes on row %d", 
                    DISPLAY_BUFFER_SIZE - local_bytes_read, row);
#ifndef GHOST_HUNTING
        break;
#endif
      }
      bytes_read += local_bytes_read;
#endif

#ifdef DISPLAY_TYPE_BW
      display.writeImage(input_row_mono_buffer, x, y + row, w, 1);
#endif
#ifdef DISPLAY_TYPE_3C
      display.writeImage(input_row_mono_buffer, input_row_color_buffer, x, y + row, w, 1);
#endif
    }

    logger.debug("Total bytes read: %d, total bytes expected: %d, %s", bytes_read, httpClient.contentLength(),
                bytes_read == httpClient.contentLength() ? "OK" : "ERROR");

#ifdef GHOST_HUNTING
    ok = 1;
    break;
#endif
    if (bytes_read == httpClient.contentLength()) {
      ok = 1;
      break;
    } else {
      logger.debug("WARNING(4): total bytes read != total bytes expected, skipped %d bytes", 
                  httpClient.contentLength() - bytes_read);
      httpClient.stop();
    }
  }
  logger.debug("Download time: %lu ms", millis() - startTime);

  if (!ok) {
    sleepTime = SLEEP_TIME_PERMANENT_ERROR;
    extern void error(String message);
    error("Failed to download image, there were errors in all attempts.");
  }

  logger.debug("Display refresh starting");
  wdtManager.refresh();
  startTime = millis();
  display.refresh();
  logger.debug("Display refresh time: %lu ms", millis() - startTime);

  strcpy(lastChecksum, newChecksum.c_str());
}
