// use class GFX of library GFX_Root instead of Adafruit_GFX
#include <GFX.h>

#include <GxEPD2_BW.h>

// Display is https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/
// aka WaveShare SKU 13187: https://www.waveshare.com/7.5inch-e-paper.htm
// aka GoodDisplay GDEW075T7 800x480 (EK79655 / GD7965)

// Driver board is https://www.laskakit.cz/waveshare-esp8266-e-paper-raw-panel-driver-board/
// aka WaveShare "ESP8266 e-Paper Raw Panel Driver Board"
// Driver board settings: switch set to "A" position (because the "B" position produced streaking and incompletely drawn content)

// 800x480
GxEPD2_BW< GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2 > display(GxEPD2_750_T7(/*CS=D8=GPIO15*/ SS, /*DC=D2=GPIO4*/ 4, /*RST=D4=GPIO2*/ 2, /*BUSY=D1=GPIO5*/ 5));

#include "Open_Sans_ExtraBold_120.h"

//const uint32_t partial_update_period_s = 2;
const uint32_t partial_update_period_s = 60;
const uint32_t full_update_period_s = 1 * 60 * 60;
const uint32_t speed_up_factor = 20;  // for simulation
const uint32_t millis_per_second = 1000 / speed_up_factor;

uint32_t start_time;
uint32_t next_time;
uint32_t previous_time;
uint32_t previous_full_update;

uint32_t total_seconds = 0;
uint32_t seconds, minutes, hours, days;

void setup() {
  Serial.begin(115200);
  Serial.println();
  Serial.println("setup");
  delay(100);
  display.init(115200);
  // cleanup
  for (int i = 0; i < 1; i++) {
    full_black();
    delay(500);
    full_white();
    delay(500);
  }
  // stripes test
  for (int i = 0; i < 1; i++) {
    horizontal_stripes(8);
    delay(1000);
    vertical_stripes(8);
    delay(3000);
  }
  full_white();
  delay(1000);
  // clock simulation test, for limited length
  start_time = next_time = previous_time = previous_full_update = millis();
  for (int i = 0; i < 50; i++) {
    clock_test();
  }
  delay(3000);
  full_white();
  delay(1000);
  display.powerOff();
  delay(1000);
  start_time = next_time = previous_time = previous_full_update = millis();
  Serial.println("setup done");
}

void loop() {
  // uncomment for continuous clock simulation test
  clock_test();
}

void full_white() {
  display.setFullWindow();
  display.firstPage();
  do {
    display.fillScreen(GxEPD_WHITE);
  } while (display.nextPage());
}

void full_black() {
  display.setFullWindow();
  display.firstPage();
  do {
    display.fillScreen(GxEPD_BLACK);
  } while (display.nextPage());
}

void partial_white() {
  display.setPartialWindow(0, 0, display.width(), display.height());
  display.firstPage();
  do {
    display.fillScreen(GxEPD_WHITE);
  } while (display.nextPage());
}

void partial_black() {
  display.setPartialWindow(0, 0, display.width(), display.height());
  display.firstPage();
  do {
    display.fillScreen(GxEPD_BLACK);
  } while (display.nextPage());
}

void horizontal_stripes(uint16_t nr) {
  uint16_t h = display.height() / nr;
  display.setPartialWindow(0, 0, display.width(), display.height());
  display.firstPage();
  do {
    uint16_t y = 0;
    do {
      display.fillRect(0, y, display.width(), h, GxEPD_BLACK);
      y += h;
      if (y >= display.height()) break;
      display.fillRect(0, y, display.width(), h, GxEPD_WHITE);
      y += h;
    } while (y < display.height());
  } while (display.nextPage());
}

void vertical_stripes(uint16_t nr) {
  uint16_t w = display.width() / nr;
  display.setPartialWindow(0, 0, display.width(), display.height());
  display.firstPage();
  do {
    uint16_t x = 0;
    do {
      display.fillRect(x, 0, w, display.height(), GxEPD_BLACK);
      x += w;
      if (x >= display.width()) break;
      display.fillRect(x, 0, w, display.height(), GxEPD_WHITE);
      x += w;
    } while (x < display.width());
  } while (display.nextPage());
}

void clock_test() {
  uint32_t actual = millis();
  while (actual < next_time) {
    // the "BlinkWithoutDelay" method works also for overflowed millis
    if ((actual - previous_time) > (partial_update_period_s * 1000)) {
      //Serial.print(actual - previous_time); Serial.print(" > "); Serial.println(partial_update_period_s * 1000);
      break;
    }
    delay(100);
    actual = millis();
  }
  if ((actual - previous_full_update) > full_update_period_s * millis_per_second) {
    //display.clearScreen();
    previous_full_update = actual;
  }
  previous_time = actual;
  next_time += uint32_t(partial_update_period_s * millis_per_second);
  total_seconds += partial_update_period_s;
  seconds = total_seconds % 60;
  minutes = (total_seconds / 60) % 60;
  hours = (total_seconds / 3600) % 24;
  days = (total_seconds / 3600) / 24;
  if ((display.width() >= 400) && (display.height() >= 300)) {
    showDays();
    showTime();
  } else showMinutes();
  display.powerOff();
}

String str02(uint32_t v) {
  return (v < 10 ? String(0) + String(v) : String(v));
}

void showMinutes() {
  static int16_t bbx = 400;
  static int16_t bby = 300;
  static uint16_t bbw = 0;
  static uint16_t bbh = 0;
  //Serial.println("showMinutes");
  String str = str02(minutes);
  display.setRotation(display.epd2.WIDTH > display.epd2.HEIGHT ? 0 : 1);
  display.setFont(&Open_Sans_ExtraBold_120);
  display.setTextColor(GxEPD_BLACK);
  int16_t tbx, tby;
  uint16_t tbw, tbh;
  display.getTextBounds(str, 0, 0, &tbx, &tby, &tbw, &tbh);
  // place the bounding box, cover any previous ones
  int16_t tx = max(0, ((display.width() - tbw) / 2));
  int16_t ty = max(0, ((display.height() - tbh) / 2));
  bbx = min(bbx, tx);
  bby = min(bby, ty);
  bbw = max(bbw, tbw);
  bbh = max(bbh, tbh);
  // calculate the cursor
  int16_t x = bbx - tbx;
  int16_t y = bby - tby;
  display.setPartialWindow(bbx, bby, bbw, bbh);
  display.firstPage();
  do {
    display.fillScreen(GxEPD_WHITE);
    //display.drawRect(bbx, bby, bbw, bbh, GxEPD_BLACK);
    display.setCursor(x, y);
    display.print(str);
  } while (display.nextPage());
  delay(1000);
  //Serial.println("showMinutes done");
}

void showDays() {
  static int16_t bbx = 400;
  static int16_t bby = 300;
  static uint16_t bbw = 0;
  static uint16_t bbh = 0;
  //Serial.println("showDays");
  String str(days);
  str += " days";
  display.setRotation(0);
  display.setFont(&Open_Sans_ExtraBold_120);
  display.setTextColor(GxEPD_BLACK);
  int16_t tbx, tby;
  uint16_t tbw, tbh;
  display.getTextBounds(str, 0, 0, &tbx, &tby, &tbw, &tbh);
  // place the bounding box
  int16_t tx = max(0, ((display.width() - tbw) / 2));
  int16_t ty = max(0, (display.height() / 4 - tbh / 2));
  bbx = min(bbx, tx);
  bby = min(bby, ty);
  bbw = max(bbw, tbw);
  bbh = max(bbh, tbh);
  // calculate the cursor
  uint16_t x = bbx - tbx;
  uint16_t y = bby - tby;
  display.setPartialWindow(bbx, bby, bbw, bbh);
  display.firstPage();
  do {
    display.fillScreen(GxEPD_WHITE);
    //display.drawRect(bbx, bby, bbw, bbh, GxEPD_BLACK);
    display.setCursor(x, y);
    display.print(str);
  } while (display.nextPage());
  delay(1000);
  //Serial.println("showDays done");
}

void showTime() {
  static int16_t bbx = 400;
  static int16_t bby = 300;
  static uint16_t bbw = 0;
  static uint16_t bbh = 0;
  //Serial.println("showTime");
  //String str = str02(minutes) + ":" + str02(seconds);
  String str = str02(hours) + ":" + str02(minutes);
  display.setRotation(0);
  display.setFont(&Open_Sans_ExtraBold_120);
  display.setTextColor(GxEPD_BLACK);
  int16_t tbx, tby;
  uint16_t tbw, tbh;
  display.getTextBounds(str, 0, 0, &tbx, &tby, &tbw, &tbh);
  // place the bounding box
  int16_t tx = max(0, ((display.width() - tbw) / 2));
  int16_t ty = max(0, (display.height() * 3 / 4 - tbh / 2));
  bbx = min(bbx, tx);
  bby = min(bby, ty);
  bbw = max(bbw, tbw);
  bbh = max(bbh, tbh);
  // calculate the cursor
  uint16_t x = bbx - tbx;
  uint16_t y = bby - tby;
  display.setPartialWindow(bbx, bby, bbw, bbh);
  display.firstPage();
  do {
    display.fillScreen(GxEPD_WHITE);
    //display.drawRect(bbx, bby, bbw, bbh, GxEPD_BLACK);
    display.setCursor(x, y);
    display.print(str);
  } while (display.nextPage());
  //Serial.println("showTime done");
}
