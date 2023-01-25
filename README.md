# "Portal" calendar for e-ink display

Heavily inspired by https://github.com/wuspy/portal_calendar. Only the software part is different here.

The main difference is that this calendar is split into two parts:
 1. Simple ESP32 or ESP8266 web client which handles the e-Paper display
 2. PC/Raspberry webserver which produces the images and (in the future) handles integration with web calendars etc.
 
I choose this approach because it's easier (and more fun) for me to implement the server part in my favourite environments (Perl, NodeJS, HTML+CSS) than to try to do this directly on ESP32.

## TODO
1. ~~Support for Czech localization and characters~~
1. ~~Greyed out icons~~
1. Refresh the display only if image has changed (=check image checksum against the previous value). This should allow the portal calendar to ask server periodically more often but still sleep a lot and preserve energy.
1. Fix the icons at the bottom, make available more of them, make them more random.
1. Add support for a weather forecast.
1. Add support for iCal.
1. Indicate possible WiFi outage or server unavailability on the display.


## Principles

- Everything is designed for a specific e-Paper size of 480x800 pixels. 
- The display content is served as raw bitmap. ESP is expected to just fetch this image from a specific location and display it.
- ESP8266 (which my driver board uses) has very limited amount of RAM. It definitely can't read PNG into memory and then transform it and sent to display. The application therefore reads uncompressed bitmap from server and sends each line to the display as it reads the data stream from http. This means that only a small buffer for 1 image line is needed in RAM.


## Bill of materials
- [Waveshare 7.5" 800x480 ePaper B/W display](https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/)
- [ESP8266 ePaper driver board](https://www.laskakit.cz/waveshare-esp8266-e-paper-raw-panel-driver-board/)
- [FFC FPC cable](https://www.laskakit.cz/ffc-fpc-nestineny-flexibilni-kabel-awm-20624-80c-60v-0-5mm-24pin--20cm/)
- [FFC FPC connector](https://www.laskakit.cz/laskakit-e-paper-ffc-fpc-24pin-atapter/)


## Installation 
Edit the `server/.env` file and set `PORT` for HTTP server and possibly the `DATETIME_LOCALE` for localization of month and day names.
```
$ sudo apt install perl libimlib2-dev libimlib2
$ make modules
$ make run
```

Once the server is running you can test it by pointing your browser at it's address (http://...:<PORT>), it should produce HTML page with source calendar on the left side and (empty yet) image on the right side.

Now you can test if the image rendering works:

```
$ server/scripts/generate_img_from_web
```
it should respond with:
```
✔ Generated 1 screenshot from 1 url and 1 size
```
Now there should be a file named `current_calendar.png` in the `server/generated_images/` folder. You don't have to do anything with it, it will get processed/converted by the server automatically on demand.

If you refresh the page now, a grayscale PNG version of the calendar screen should be visible on the right side.

Done ✅

## Disclaimer

I don't expect anyone to use this project directly, mainly because it's written in Perl. But the HTML, CSS and ESP code might serve as an inspiration maybe.

The `custom-portal-sign-icons.png` and `custom-portal-sign-full.png` were downloaded from https://decalrobot.com/. 

Sources for fonts are listed in the `server/public/fonts/README.txt`.

---

## Examples:

Half-finished:
![image](https://user-images.githubusercontent.com/16558674/214158618-31573f8c-0cd9-4471-a230-aabc3bd393cd.png)

How the grayscale rendered image (`/calendar/bitmap?rotate=0&flip=`) looks like, here in Czech localization:
![image](https://user-images.githubusercontent.com/16558674/214332528-8c96e01c-c7d5-4c95-8720-1074089cf5d4.png)

And how the B&W bitmap looks like with custom threshold (`/calendar/bitmap?rotate=0&flip=&threshold=165`):
![image](https://user-images.githubusercontent.com/16558674/214332690-1f8cfc23-6e10-4f63-b5b9-e4cc32968010.png)


