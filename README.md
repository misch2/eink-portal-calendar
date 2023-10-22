# "Portal" calendar for e-ink display

Inspired by https://github.com/wuspy/portal_calendar. Hardware is exactly the same here, only the software (mainly the server part, but client too) is different here.

The software is divided into two parts:
 1. Simple ESP32 web client which handles the e-Paper display
 2. PC/Raspberry webserver which produces the images and takes care of everything else, e.g.:
    - integration with web calendars
    - integration with weather provider
    - integration with HomeAssistant (battery & status monitor), 
    - UI for configuration
 
I choose this approach because it's easier (and more fun) for me to implement the server part in my favourite environments (Perl, NodeJS, HTML+CSS) than to try to do this directly on ESP32.

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
1. Maybe add support for a weather forecast (but I'll probably create a different project just for this purpose).
1. ~~Add a config page to the server, to allow changing calendar properties (e.g. weather on/off, icon sets, etc.) easily without having to redeploy updated server.~~
1. ~~Add battery voltage measurement~~
1. ~~Add MQTT support (to see status in HomeAssistant)~~
1. ~~Add battery level indicator~~
1. ~~Add integration with Google Fit and display a weight data+chart~~
1. ~~Add support for multiple calendars (inspired by https://zivyobraz.eu/)~~
1. ~~Better battery voltage monitoring + move more things from client to the server~~

[^1]: I didn't consider the need for ESP board with very low power consumption. I therefore bought one that was available immediately (ESP8266 with integrated e-Paper driver). But while it's perfectly usable when powered through USB, it wouldn't keep working sufficiently long with AAA batteries. I therefore switched to low power ESP32 board.

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

Edit the `server/app.conf` and update settings there. See the `server/examples/app*.conf` for an example.

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
![image](https://user-images.githubusercontent.com/16558674/219482420-b5643deb-5625-4562-82ca-60fa25804da6.png)

"Broken display" variant:
 
![image](https://user-images.githubusercontent.com/16558674/218329554-1cf13b36-d0ab-4a0a-9ead-7b298c4bb202.png)

## Timing

### Timing on ESP8266:

```
with DEBUG_VISIBLE on:
 0:00 boot
 1:77 try to display 'starting...'
 5:15  - fully displayed
 6:92 fully displayed 'connecting to wifi...'
11:50 fully displayed 'connecting to webserver...'
13:27 fully displayed 'downloading...'
19:82 refreshing display
23:64  - finished

with DEBUG_VISIBLE off:
 0:00 boot
10:40 refreshing display
13:60  - finished

dtto but optimized backend
 0:00 boot
 9:42  - finished
```

### Timing on ESP32
```
 0:00 boot
 2:30 finished reading image checksum data
 9:10 finished all
```
 
### Example of the client->server communication

Approximately one second to wake up, download config and image info, and sleep again if nothing has changed:
```
Feb 12 15:16:39 esp32 portal-calendar ---
Feb 12 15:16:39 esp32 portal-calendar Connected to WiFi in 655ms
Feb 12 15:16:39 esp32 portal-calendar IP address: w.x.y.z
Feb 12 15:16:39 esp32 portal-calendar Wakeup cause: 4, reset cause: 8
Feb 12 15:16:39 esp32 portal-calendar ESP_RST_DEEPSLEEP
Feb 12 15:16:39 esp32 portal-calendar ESP_SLEEP_WAKEUP_TIMER
Feb 12 15:16:39 esp32 portal-calendar Boot count: 20, last image checksum: 22ed4f4309df31d396cd83214a40e77816367ab6
Feb 12 15:16:39 esp32 portal-calendar Loading config from web
Feb 12 15:16:39 esp32 portal-calendar connecting to http://u.v.w.x/config
Feb 12 15:16:39 esp32 portal-calendar calling GET
Feb 12 15:16:39 esp32 portal-calendar end, response=200
Feb 12 15:16:39 esp32 portal-calendar sleepTime set to 3600
Feb 12 15:16:39 esp32 portal-calendar Downloading http://u.v.w.x/calendar/bitmap/epapermono
Feb 12 15:16:40 esp32 portal-calendar  read line: [HTTP/1.1 200 OK#015#012]
Feb 12 15:16:40 esp32 portal-calendar Waiting for OK response from server. Current line: HTTP/1.1 200 OK#015
Feb 12 15:16:40 esp32 portal-calendar  read line: [Server: nginx/1.18.0#015#012]
Feb 12 15:16:40 esp32 portal-calendar  read line: [Date: Sun, 12 Feb 2023 14:16:40 GMT#015#012]
Feb 12 15:16:40 esp32 portal-calendar  read line: [Content-Type: text/html;charset=UTF-8#015#012]
Feb 12 15:16:40 esp32 portal-calendar  read line: [Content-Length: 48044#015#012]
Feb 12 15:16:40 esp32 portal-calendar  read line: [Connection: close#015#012]
Feb 12 15:16:40 esp32 portal-calendar  read line: [Vary: Accept-Encoding#015#012]
Feb 12 15:16:40 esp32 portal-calendar  read line: [#015#012]
Feb 12 15:16:40 esp32 portal-calendar All headers received
Feb 12 15:16:40 esp32 portal-calendar Reading bitmap header
Feb 12 15:16:40 esp32 portal-calendar Reading checksum
Feb 12 15:16:40 esp32 portal-calendar Last checksum was: 22ed4f4309df31d396cd83214a40e77816367ab6
Feb 12 15:16:40 esp32 portal-calendar New checksum is: 22ed4f4309df31d396cd83214a40e77816367ab6
Feb 12 15:16:40 esp32 portal-calendar Not refreshing, image is unchanged
Feb 12 15:16:40 esp32 portal-calendar Total execution time: 1253ms
Feb 12 15:16:40 esp32 portal-calendar Going to hibernate for 3600 seconds
```
