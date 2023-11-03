# "Portal" calendar for e-ink display

<img src="https://github.com/misch2/eink-portal-calendar/assets/16558674/b2b185de-a960-480c-99a1-aa7d521ed9d6" width="250">
<img src="https://github.com/misch2/eink-portal-calendar/assets/16558674/66098158-f8c2-456c-95e3-673dab4ea655" width="250">

Inspired by https://github.com/wuspy/portal_calendar. Hardware is exactly the same here, only the software (mainly the server part, but client too) is different here.

The software is divided into two parts:
 1. Simple ESP32 web client which handles the e-Paper display
 2. PC/Raspberry webserver which produces the images and takes care of everything else, e.g.:
    - integration with web calendars
    - integration with weather provider
    - integration with HomeAssistant (battery & status monitor), 
    - UI for configuration
 
I choose this approach because it's easier (and more fun) for me to implement the server part in my favourite environments (Perl, NodeJS, HTML+CSS) than to try to do this directly on ESP32.

## Principles

* Everything is designed for a specific e-Paper size of 480x800 pixels. 
* The display content is served as raw bitmap. The only task for ESP is to fetch this image from a specific location and display it.
* All the rendering is performed on the server, using standard HTML + CSS. This allows me to use provide content without constantly re-flashing the ESP32. It's also much easier for me to debug CSS and try to pixel-perfect position everything or to integrace for example ICS calendar etc.

I also added a voltage monitorig because with ePaper it's not easily detactable when the battery goes low -- the old image just keeps being on the display.

## Bill of materials

* Display:
  * [Waveshare 7.5" 800x480 ePaper B/W display](https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/)
  * or [WFT0583CZ61 7.5" 800x480 ePaper B/W/R display](https://www.aliexpress.com/item/1005005121813674.html?spm=a2g0o.order_list.order_list_main.5.1a521802F7URVo)
* ~~[ESP8266 ePaper driver board](https://www.laskakit.cz/waveshare-esp8266-e-paper-raw-panel-driver-board/)~~ I used the exact ESP32 board + extra hat as in the original project because it nicely matched the existing frame.
* [FFC FPC cable](https://www.laskakit.cz/ffc-fpc-nestineny-flexibilni-kabel-awm-20624-80c-60v-0-5mm-24pin--20cm/)
* [FFC FPC connector](https://www.laskakit.cz/laskakit-e-paper-ffc-fpc-24pin-atapter/)

## Installation 

Edit the `server/portal_calendar.conf` and update settings there. See the `server/examples/portal_calendar8.conf` for an example.

```
$ sudo apt install perl libimlib2-dev libimlib2
$ make modules
$ server/scripts/run_minion &
$ server/scripts/run_webserver &
```

Once the server is running you can test it by pointing your browser at it's address (http://...:<PORT>), it should produce HTML page with source calendar on the left side and (empty yet) image on the right side.

Now you can test if the image rendering works:

```
$ server/scripts/generate_img_from_web
```

it should respond with something like:

```
1
Job enqueued
```

After a short while there should be a file named `current_calendar.png` in the `server/generated_images/` folder. If there isn't any, check the minion output (either in console, or via http://...:<PORT>/admin URL).

If you refresh the page now, a grayscale PNG version of the calendar screen should be visible on the right side.

Done ✅

## Sources:
 - The `custom-portal-sign-icons.png` and `custom-portal-sign-full.png` were downloaded from https://decalrobot.com/. 
   - Icons in the `server/public/images/portal_icons` were extracted manually from the image above
 - Fonts in `server/public/fonts` were downloaded from:
   - D-DIN-BOLD.otf, D-DIN.otf, D-DINCondensed.otf
     https://www.fontsquirrel.com/fonts/d-din (ASCII and basic accents only)
   - 651-font.otf
     https://cs.fontsisland.com/font/din-pro (full Czech set of characters)
 - Files in `client/wuspy_portal_calendar` are git-cloned from https://github.com/wuspy/portal_calendar.git (see `.gitmodules` file in the root folder)
 - "Broken display" overlay was downloaded from https://www.wallpaperflare.com/technology-cracked-screen-broken-screen-no-people-animal-wildlife-wallpaper-jpnv

 ## Disclaimer

I don't expect anyone to use this project directly, mainly because it's written in Perl. But on the other hand the HTML, CSS or ESP32 code might serve as an inspiration for someone.

---

## Examples:

Half-finished:

![image](https://user-images.githubusercontent.com/16558674/214158618-31573f8c-0cd9-4471-a230-aabc3bd393cd.png)

Grayscale rendered image ( `/calendar/bitmap?rotate=0&flip=` ), in Czech localization:

![image](https://user-images.githubusercontent.com/16558674/214332528-8c96e01c-c7d5-4c95-8720-1074089cf5d4.png)

B&W bitmap with modified gamma for more blacks ( `/calendar/bitmap?colors=2&gamma=1.8` ):

![image](https://user-images.githubusercontent.com/16558674/214617604-5f2b534c-2f68-4d9c-8866-10e8eeeff591.png)

Configuration UI:
![image](https://github.com/misch2/eink-portal-calendar/assets/16558674/c6f1d8bc-9d4d-44d4-83df-628a64559bb5)

## TODO

1. ~~Support for Czech localization and characters~~
1. ~~Grayed out icons~~
1. ~~Add support for iCal.~~
1. ~~Fix the icons at the bottom, make more of them available, make them more random.~~
1. ~~Indicate possible WiFi outage or server unavailability on the display.~~
1. ~~Refresh the display only if image has changed (=check image checksum against the previous value). This should allow the portal calendar to ask server periodically more often but still sleep a lot and preserve energy.~~
1. ~~Replace the ESP8266 ePaper module with what [original project](https://github.com/wuspy/portal_calendar) uses, i.e. specific low power ESP32 board + separate e-Paper hat [^1].~~
1. ~~Configurable through the UI.~~
1. ~~Offload calendar parsing to minion worker.~~
1. ~~Maybe add support for a weather forecast (but I'll probably create a different project just for this purpose).~~
1. ~~Add a config page to the server, to allow changing calendar properties (e.g. weather on/off, icon sets, etc.) easily without having to redeploy updated server.~~
1. ~~Add battery voltage measurement~~
1. ~~Add MQTT support (to see status in HomeAssistant)~~
1. ~~Add battery level indicator~~
1. ~~Add integration with Google Fit and display a weight data+chart~~
1. ~~Add support for multiple calendars (inspired by https://zivyobraz.eu/)~~
1. ~~Better battery voltage monitoring + move more things from client to the server~~

[^1]: I didn't consider the need for ESP board with very low power consumption. I therefore bought one that was available immediately (ESP8266 with integrated e-Paper driver). But while it's perfectly usable when powered through USB, it wouldn't keep working sufficiently long with AAA batteries. I therefore switched to low power ESP32 board.
