// Display is 800x480 B/W
// https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/ aka
// WaveShare SKU 13187: https://www.waveshare.com/7.5inch-e-paper.htm aka
// GoodDisplay GDEW075T7 800x480 (EK79655 / GD7965)

// Driver board is
// https://www.laskakit.cz/waveshare-esp8266-e-paper-raw-panel-driver-board/ aka
// WaveShare "ESP8266 e-Paper Raw Panel Driver Board" Driver board settings:
// switch set to "A" position (because the "B" position produced streaking and
// incompletely drawn content)

// GDEW075T7 800x480, EK79655 (GD7965)
#define DISPLAY_WIDTH 480
#define DISPLAY_HEIGHT 800

// Uncomment correct color capability of your ePaper display
#define TYPE_BW // black and white
// #define TYPE_3C // 3 colors - black, white and red/yellow
//#define TYPE_GRAYSCALE // grayscale - 4 colors
//#define TYPE_7C // 7 colors

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

// // ESP32S3 devkitc1 N16R8 board
// #define SPI_BUS HSPI
// #define REMAP_SPI
// #define PIN_SPI_CLK 13  // CLK
// #define PIN_SPI_MISO 14 // unused
// #define PIN_SPI_MOSI 12 // DIN
// #define PIN_SPI_SS 15   // unused

// #define CS_PIN 5    // SS
// #define DC_PIN 17   // D/C
// #define RST_PIN 16  // RES
// #define BUSY_PIN 4  // PIN_BUSY
// // #define VOLTAGE_ADC_PIN 32

