// NOT VERIFIED!!!
// Doesn't seem to work
//
// 7.5" 3C 800x480 B/W/R (allegedly)
// "SUNMAXIC 7.5 inch Electronic Paper Ink Screen 800x480 Resolution Black And White EPD E-Paper UC8179 Driver SPI Interface 24Pin"
// https://www.aliexpress.com/item/1005007175851007.html

// GxEPD2_750c_ZJY = my experimental panel
// #define DISPLAY_CLASS_TYPE GxEPD2_3C<GxEPD2_750c_ZJY, GxEPD2_750c_ZJY::HEIGHT / 2>
// #define DISPLAY_CLASS_ARGUMENTS (GxEPD2_750c_ZJY(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN))

// test: GxEPD2_750c_GDEY075Z08
#define DISPLAY_CLASS_TYPE GxEPD2_3C<GxEPD2_750c_GDEY075Z08, GxEPD2_750c_GDEY075Z08::HEIGHT>  //  / 2>
#define DISPLAY_CLASS_ARGUMENTS (GxEPD2_750c_GDEY075Z08(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN))

#define DISPLAY_WIDTH 800
#define DISPLAY_HEIGHT 480
#define DISPLAY_TYPE_3C  // 3 colors - black, white, red
