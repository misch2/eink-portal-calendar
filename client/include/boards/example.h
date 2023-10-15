// #define DEBUG
// #define USE_GRAYSCALE_BW_DISPLAY

#define CALENDAR_URL_HOST "192.168.0.100"
#define CALENDAR_URL_PORT 8000

#define SYSLOG_SERVER "192.168.0.101"
#define SYSLOG_PORT 514
#define SYSLOG_MYHOSTNAME "esp32"
#define SYSLOG_MYAPPNAME "portal-calendar-client"

// Display is 800x480 B/W
// https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/ aka
// WaveShare SKU 13187: https://www.waveshare.com/7.5inch-e-paper.htm aka
// GoodDisplay GDEW075T7 800x480 (EK79655 / GD7965)

// Driver board is
// https://www.laskakit.cz/waveshare-esp8266-e-paper-raw-panel-driver-board/ aka
// WaveShare "ESP8266 e-Paper Raw Panel Driver Board" Driver board settings:
// switch set to "A" position (because the "B" position produced streaking and
// incompletely drawn content)

// physical resolution, independent on later rotation
// GDEW075T7 800x480, EK79655 (GD7965)
#define DISPLAY_WIDTH 800
#define DISPLAY_HEIGHT 480

// Uncomment correct color capability of your ePaper display
#define TYPE_BW // black and white
// #define TYPE_3C // 3 colors - black, white and red/yellow
//#define TYPE_GRAYSCALE // grayscale - 4 colors
//#define TYPE_7C // 7 colors

#define BYTES_PER_ROW (DISPLAY_WIDTH / 8)
#define ROWS_AT_ONCE 8


/*
WaveShare ePaper Driver HAT connector (from top to bottom):

BUSY - violet #808 - to any input GPIO
RST  - white  #fff - to any output GPIO
DC   - green  #070 - to any output GPIO
CS   - orange #f80 - to SPI CS
CLK  - yellow #ff0 - to SPI SCLK
DIN  - blue   #008 - to SPI MOSI
GND  - brown  #844 - to GND
VCC  - gray   #888 - to +3.3V
*/

// EzSBC ESP32 breakout board
#define SPI_BUS HSPI
#define CS_PIN 15
#define DC_PIN 23
#define RST_PIN 33
#define BUSY_PIN 27
#define VOLTAGE_ADC_PIN 32

#define DISPLAY_INSTANCE  GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
