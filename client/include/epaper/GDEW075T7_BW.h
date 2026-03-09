// Display is 800x480 B/W
// https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/ aka
// WaveShare SKU 13187: https://www.waveshare.com/7.5inch-e-paper.htm aka
// GoodDisplay GDEW075T7 800x480 (EK79655 / GD7965)

#define DISPLAY_WIDTH 800
#define DISPLAY_HEIGHT 480
#define DISPLAY_TYPE_BW

#define DISPLAY_CLASS_TYPE GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT>  // or HEIGHT/2 etc. for partial page buffer and memory savings
#define DISPLAY_CLASS_ARGUMENTS (GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN))
