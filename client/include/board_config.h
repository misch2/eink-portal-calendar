#pragma once

#include "board.h"

#define DISPLAY_BUFFER_SIZE (DISPLAY_WIDTH * BITMAP_BPP / 8)

#ifdef DISPLAY_TYPE_BW
#include <GxEPD2_BW.h>
#define DISPLAY_COLOR_TYPE_AS_STRING "BW"
#endif
#ifdef DISPLAY_TYPE_GRAYSCALE
#define DISPLAY_COLOR_TYPE_AS_STRING "4G"
#ifdef USE_GRAYSCALE_BW_DISPLAY
#include <GxEPD2_4G_BW.h>
#else
#include <GxEPD2_4G_4G.h>
#endif
#endif
#ifdef DISPLAY_TYPE_3C
#define DISPLAY_COLOR_TYPE_AS_STRING "3C"
#include <GxEPD2_3C.h>
#endif

