// 7.5" 3C 800x480 B/W/R
// https://www.aliexpress.com/item/1005005121813674.html

#define DISPLAY_CLASS_TYPE GxEPD2_3C<GxEPD2_750c_Z08, GxEPD2_750c_Z08::HEIGHT>  // or HEIGHT/2 etc. for partial page buffer and memory savings
#define DISPLAY_CLASS_ARGUMENTS (GxEPD2_750c_Z08(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN))

#define DISPLAY_WIDTH 800
#define DISPLAY_HEIGHT 480
#define DISPLAY_TYPE_3C
