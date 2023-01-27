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

// WaveShare ESP8266 e-Paper Raw Panel Driver Board
#define CS_PIN SS  /* D8, GPIO15 */
#define DC_PIN 4   /* D2, GPIO4 */
#define RST_PIN 2  /* D4. GPIO 2 */
#define BUSY_PIN 5 /* D1, GPIO 5 */
