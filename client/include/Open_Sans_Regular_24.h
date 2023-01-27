// Created by http://oleddisplay.squix.ch/ Consider a donation
// In case of problems make sure that you are using the font file with the correct version!
const uint8_t Open_Sans_Regular_24Bitmaps[] PROGMEM = {

	// Bitmap Data:
	0x00, // ' '
	0xDB,0x6D,0xB6,0xDB,0x60,0x37,0xC0, // '!'
	0xCF,0x3C,0xF3,0x86,0x10, // '"'
	0x06,0x30,0x18,0xC0,0x43,0x01,0x08,0x0C,0x21,0xFF,0xF7,0xFF,0xC2,0x18,0x08,0x40,0x61,0x0F,0xFF,0xBF,0xFE,0x10,0xC0,0xC2,0x03,0x08,0x0C,0x60,0x31,0x80, // '#'
	0x0C,0x03,0x03,0xF9,0xFF,0xEC,0xB3,0x0C,0xC3,0xB0,0x7C,0x07,0xE0,0xFC,0x33,0x0C,0xE3,0x7F,0xF9,0xF8,0x0C,0x03,0x00,0xC0, // '$'
	0x3C,0x04,0x1B,0x83,0x04,0x60,0x83,0x08,0x60,0xC2,0x30,0x30,0x8C,0x0C,0x26,0x01,0x19,0x3C,0x6E,0xDF,0x8F,0x26,0x20,0x19,0x0C,0x0C,0x43,0x03,0x10,0xC1,0x84,0x30,0x61,0x88,0x30,0x76,0x08,0x0F,0x00, // '%'
	0x0F,0x80,0x0F,0xE0,0x0E,0x30,0x06,0x0C,0x03,0x06,0x01,0x86,0x00,0x66,0x00,0x1E,0x00,0x1E,0x00,0x1D,0xC1,0x98,0x71,0x8C,0x1C,0xCC,0x07,0xC7,0x01,0xE1,0xC1,0xF0,0xFF,0xDC,0x1F,0x83,0x00, // '&'
	0x6D,0x24,0x80, // '''
	0x19,0x8C,0xC6,0x33,0x18,0xC6,0x31,0x8C,0x63,0x0C,0x63,0x0C,0x61,0x80, // '('
	0xC3,0x18,0x63,0x18,0x63,0x18,0xC6,0x31,0x8C,0x66,0x31,0x98,0xCC,0x00, // ')'
	0x0E,0x01,0x80,0x10,0x42,0x1F,0x5F,0xFF,0xC3,0x80,0xD8,0x1B,0x06,0x30,0x44,0x00, // '*'
	0x06,0x00,0x60,0x06,0x00,0x60,0x06,0x0F,0xFF,0xFF,0xF0,0x60,0x06,0x00,0x60,0x06,0x00,0x60, // '+'
	0x6D,0xED,0x00, // ','
	0xFF,0xF0, // '-'
	0xDF,0x00, // '.'
	0x01,0x81,0x80,0xC0,0xC0,0x60,0x30,0x30,0x18,0x1C,0x0C,0x06,0x06,0x03,0x01,0x81,0x80,0xC0,0xC0,0x00, // '/'
	0x0F,0x03,0xFC,0x70,0xC6,0x06,0x60,0x6C,0x03,0xC0,0x3C,0x03,0xC0,0x3C,0x03,0xC0,0x3C,0x03,0x60,0x66,0x06,0x70,0xE3,0xFC,0x0F,0x80, // '0'
	0x0E,0x3C,0xDF,0x34,0x60,0xC1,0x83,0x06,0x0C,0x18,0x30,0x60,0xC1,0x83,0x06, // '1'
	0x1F,0x07,0xFC,0x60,0xE4,0x06,0x00,0x60,0x06,0x00,0x60,0x0C,0x01,0x80,0x30,0x06,0x00,0xC0,0x18,0x03,0x00,0x60,0x0F,0xFF,0xFF,0xF0, // '2'
	0x1F,0x87,0xFC,0x60,0xE0,0x06,0x00,0x60,0x06,0x01,0xC1,0xF0,0x1F,0xC0,0x0E,0x00,0x60,0x03,0x00,0x70,0x06,0xC0,0xEF,0xFC,0x3F,0x00, // '3'
	0x01,0xC0,0x0E,0x00,0xF0,0x0D,0x80,0xCC,0x06,0x60,0x63,0x06,0x18,0x20,0xC3,0x06,0x30,0x31,0xFF,0xFF,0xFF,0x80,0x60,0x03,0x00,0x18,0x00,0xC0, // '4'
	0x7F,0x8F,0xF3,0x00,0x60,0x0C,0x01,0x80,0x30,0x07,0xF8,0xFF,0x80,0x38,0x03,0x00,0x70,0x06,0x01,0xA0,0x77,0xFC,0x7E,0x00, // '5'
	0x07,0xC1,0xFC,0x3C,0x03,0x00,0x60,0x06,0x00,0x6F,0x85,0xFE,0xF0,0xEE,0x07,0xC0,0x34,0x03,0x60,0x36,0x06,0x30,0xE3,0xFC,0x0F,0x80, // '6'
	0xFF,0xFF,0xFF,0x00,0x60,0x06,0x00,0xC0,0x0C,0x01,0x80,0x18,0x01,0x80,0x30,0x03,0x00,0x60,0x06,0x00,0xC0,0x0C,0x01,0x80,0x18,0x00, // '7'
	0x1F,0x83,0xFC,0x70,0xE6,0x06,0x60,0x66,0x06,0x39,0xC1,0xF8,0x1F,0x83,0x1C,0x60,0x6C,0x03,0xC0,0x3C,0x07,0x70,0xE7,0xFE,0x1F,0x80, // '8'
	0x1F,0x03,0xFC,0x70,0xC6,0x06,0xC0,0x6C,0x03,0xC0,0x3E,0x07,0x70,0xF7,0xFB,0x1F,0x20,0x06,0x00,0x60,0x0C,0x01,0xC3,0xF8,0x3E,0x00, // '9'
	0xDF,0x00,0x00,0x03,0x7C, // ':'
	0x6D,0x80,0x00,0x01,0xBF,0xB4, // ';'
	0x00,0x10,0x07,0x01,0xC0,0x70,0x1C,0x07,0x00,0xE0,0x03,0x80,0x0F,0x00,0x3C,0x00,0xF0,0x01, // '<'
	0xFF,0xFF,0xFF,0x00,0x00,0x00,0x00,0x0F,0xFF,0xFF,0xF0, // '='
	0x80,0x0E,0x00,0x38,0x00,0xE0,0x03,0x80,0x0E,0x00,0x70,0x1C,0x0F,0x03,0xC0,0xF0,0x08,0x00, // '>'
	0x3E,0x7F,0xD0,0x60,0x30,0x08,0x0C,0x0E,0x0E,0x0E,0x06,0x06,0x03,0x00,0x00,0x00,0x60,0x38,0x18,0x00, // '?'
	0x01,0xFC,0x00,0x7F,0xF0,0x0C,0x01,0x81,0x80,0x0C,0x30,0x00,0x66,0x0F,0xC6,0x61,0xDC,0x24,0x30,0xC3,0x42,0x0C,0x3C,0x60,0xC2,0xC6,0x0C,0x2C,0x20,0xC6,0x43,0xB6,0xC6,0x1E,0x38,0x60,0x00,0x03,0x00,0x00,0x18,0x00,0x00,0xFF,0xE0,0x03,0xF8,0x00, // '@'
	0x03,0x80,0x07,0x00,0x0A,0x00,0x36,0x00,0x6C,0x00,0x8C,0x03,0x18,0x06,0x30,0x18,0x30,0x30,0x60,0x7F,0xC1,0xFF,0xC3,0x01,0x8C,0x03,0x98,0x03,0x30,0x06,0xC0,0x06, // 'A'
	0xFF,0x87,0xFF,0x30,0x1D,0x80,0x6C,0x03,0x60,0x1B,0x01,0x9F,0xF8,0xFF,0xC6,0x03,0xB0,0x0D,0x80,0x7C,0x03,0xE0,0x1B,0x01,0xDF,0xFC,0xFF,0xC0, // 'B'
	0x03,0xF0,0x7F,0xCF,0x06,0x60,0x07,0x00,0x30,0x01,0x80,0x1C,0x00,0xE0,0x07,0x00,0x18,0x00,0xC0,0x07,0x00,0x18,0x00,0xF0,0x23,0xFF,0x07,0xF0, // 'C'
	0xFF,0x81,0xFF,0xC3,0x01,0xE6,0x01,0xCC,0x01,0xD8,0x01,0xB0,0x03,0x60,0x06,0xC0,0x0F,0x80,0x1B,0x00,0x36,0x00,0x6C,0x01,0xD8,0x07,0x30,0x3C,0x7F,0xF0,0xFF,0x80, // 'D'
	0xFF,0xFF,0xFC,0x03,0x00,0xC0,0x30,0x0C,0x03,0xFE,0xFF,0xB0,0x0C,0x03,0x00,0xC0,0x30,0x0C,0x03,0xFF,0xFF,0xC0, // 'E'
	0xFF,0xFF,0xFC,0x03,0x00,0xC0,0x30,0x0C,0x03,0x00,0xFF,0xBF,0xEC,0x03,0x00,0xC0,0x30,0x0C,0x03,0x00,0xC0,0x00, // 'F'
	0x03,0xF0,0x3F,0xF3,0xC0,0x8C,0x00,0x70,0x01,0x80,0x06,0x00,0x38,0x00,0xE0,0x7F,0x81,0xF6,0x00,0xD8,0x03,0x70,0x0C,0xC0,0x33,0xC0,0xC7,0xFF,0x07,0xF8, // 'G'
	0xC0,0x0F,0x00,0x3C,0x00,0xF0,0x03,0xC0,0x0F,0x00,0x3C,0x00,0xFF,0xFF,0xFF,0xFF,0x00,0x3C,0x00,0xF0,0x03,0xC0,0x0F,0x00,0x3C,0x00,0xF0,0x03,0xC0,0x0C, // 'H'
	0xFF,0xFF,0xFF,0xFF,0xC0, // 'I'
	0x0C,0x30,0xC3,0x0C,0x30,0xC3,0x0C,0x30,0xC3,0x0C,0x30,0xC3,0x0C,0x31,0xFE,0xF0, // 'J'
	0xC0,0x36,0x03,0x30,0x31,0x83,0x0C,0x30,0x63,0x83,0x38,0x1B,0x80,0xFE,0x07,0x30,0x30,0xC1,0x87,0x0C,0x1C,0x60,0x63,0x01,0x98,0x0E,0xC0,0x38, // 'K'
	0xC0,0x60,0x30,0x18,0x0C,0x06,0x03,0x01,0x80,0xC0,0x60,0x30,0x18,0x0C,0x06,0x03,0x01,0xFF,0xFF,0x80, // 'L'
	0xF0,0x03,0xFC,0x00,0xFF,0x00,0x2F,0x60,0x1B,0xD8,0x06,0xF3,0x03,0x3C,0xC0,0xCF,0x30,0x33,0xC6,0x18,0xF1,0x86,0x3C,0x33,0x0F,0x0C,0xC3,0xC3,0x30,0xF0,0x78,0x3C,0x1E,0x0F,0x07,0x03,0xC0,0xC0,0xC0, // 'M'
	0xE0,0x0F,0xC0,0x3F,0x00,0xF6,0x03,0xDC,0x0F,0x30,0x3C,0x60,0xF1,0xC3,0xC3,0x0F,0x06,0x3C,0x18,0xF0,0x33,0xC0,0xEF,0x01,0xBC,0x03,0xF0,0x0F,0xC0,0x1C, // 'N'
	0x07,0xF0,0x0F,0xFE,0x0F,0x07,0x86,0x00,0xC6,0x00,0x33,0x00,0x19,0x80,0x0C,0xC0,0x06,0xE0,0x03,0xB0,0x01,0x98,0x00,0xCC,0x00,0x66,0x00,0x31,0x80,0x30,0xF0,0x78,0x3F,0xF8,0x07,0xF0,0x00, // 'O'
	0xFF,0x1F,0xF3,0x07,0x60,0x7C,0x07,0x80,0xF0,0x1E,0x06,0xC1,0xDF,0xF3,0xF8,0x60,0x0C,0x01,0x80,0x30,0x06,0x00,0xC0,0x00, // 'P'
	0x07,0xF0,0x0F,0xFE,0x0F,0x07,0x86,0x00,0xC6,0x00,0x33,0x00,0x19,0x80,0x0C,0xC0,0x06,0xE0,0x03,0xB0,0x01,0x98,0x00,0xCC,0x00,0x66,0x00,0x31,0x80,0x30,0xF0,0x78,0x3F,0xF8,0x07,0xF0,0x00,0x18,0x00,0x0E,0x00,0x03,0x80,0x00,0xE0, // 'Q'
	0xFF,0x87,0xFE,0x30,0x39,0x80,0xCC,0x06,0x60,0x33,0x01,0x98,0x1C,0xFF,0xC7,0xF8,0x30,0xE1,0x83,0x0C,0x0C,0x60,0x63,0x01,0x98,0x0E,0xC0,0x30, // 'R'
	0x1F,0x8F,0xF9,0xC1,0x70,0x0C,0x00,0xC0,0x1C,0x01,0xE0,0x1F,0x00,0x78,0x03,0x00,0x30,0x06,0x00,0xF0,0x77,0xFE,0x7F,0x00, // 'S'
	0xFF,0xFF,0xFF,0xC0,0xC0,0x06,0x00,0x30,0x01,0x80,0x0C,0x00,0x60,0x03,0x00,0x18,0x00,0xC0,0x06,0x00,0x30,0x01,0x80,0x0C,0x00,0x60,0x03,0x00, // 'T'
	0xC0,0x1E,0x00,0xF0,0x07,0x80,0x3C,0x01,0xE0,0x0F,0x00,0x78,0x03,0xC0,0x1E,0x00,0xF0,0x07,0x80,0x3C,0x01,0xB0,0x19,0xC1,0xC7,0xFC,0x1F,0x80, // 'U'
	0xC0,0x0D,0x80,0x66,0x01,0x98,0x06,0x30,0x30,0xC0,0xC3,0x03,0x06,0x18,0x18,0x60,0x61,0x80,0xCC,0x03,0x30,0x0C,0xC0,0x1E,0x00,0x78,0x01,0xE0,0x03,0x00, // 'V'
	0xE0,0x30,0x1D,0x81,0xC0,0x66,0x07,0x81,0x98,0x1E,0x06,0x70,0x58,0x30,0xC3,0x30,0xC3,0x0C,0xC3,0x0C,0x33,0x0C,0x19,0x86,0x60,0x66,0x19,0x81,0x98,0x66,0x06,0xC0,0x98,0x0F,0x03,0xC0,0x3C,0x0F,0x00,0xF0,0x3C,0x03,0x80,0x70,0x06,0x01,0x80, // 'W'
	0x60,0x19,0xC0,0xE3,0x03,0x06,0x18,0x1C,0xE0,0x33,0x00,0x78,0x01,0xE0,0x03,0x00,0x1E,0x00,0xCC,0x03,0x30,0x18,0x60,0xC1,0xC3,0x03,0x18,0x06,0xE0,0x1C, // 'X'
	0xC0,0x1B,0x01,0x98,0x0C,0x60,0xC3,0x06,0x0C,0x60,0x63,0x01,0xB0,0x0F,0x80,0x38,0x00,0xC0,0x06,0x00,0x30,0x01,0x80,0x0C,0x00,0x60,0x03,0x00, // 'Y'
	0xFF,0xFF,0xFF,0x00,0x60,0x0C,0x00,0xC0,0x18,0x03,0x00,0x70,0x06,0x00,0xC0,0x1C,0x01,0x80,0x30,0x06,0x00,0x60,0x0F,0xFF,0xFF,0xF0, // 'Z'
	0xFF,0xF1,0x8C,0x63,0x18,0xC6,0x31,0x8C,0x63,0x18,0xC6,0x31,0xFF,0x80, // '['
	0xC0,0x30,0x18,0x06,0x03,0x01,0x80,0x60,0x30,0x08,0x06,0x03,0x00,0xC0,0x60,0x30,0x0C,0x06,0x01,0x80, // '\'
	0xFF,0xC6,0x31,0x8C,0x63,0x18,0xC6,0x31,0x8C,0x63,0x18,0xC7,0xFF,0x80, // ']'
	0x0C,0x01,0xC0,0x78,0x0D,0x81,0x30,0x63,0x08,0x63,0x06,0x40,0xD8,0x0F,0x01,0x80, // '^'
	0xFF,0xFF,0xFC, // '_'
	0xC6,0x63, // '`'
	0x1F,0x0F,0xE2,0x1C,0x03,0x00,0xC7,0xF7,0xFF,0xC3,0xC0,0xF0,0x3E,0x1D,0xFF,0x3E,0x40, // 'a'
	0xC0,0x0C,0x00,0xC0,0x0C,0x00,0xC0,0x0C,0xF8,0xFF,0xCF,0x0E,0xE0,0x6C,0x06,0xC0,0x3C,0x03,0xC0,0x7C,0x06,0xE0,0x6F,0x0E,0xFF,0xCC,0xF8, // 'b'
	0x1F,0x9F,0xDC,0x2C,0x0E,0x06,0x03,0x01,0x80,0xE0,0x30,0x1C,0x07,0xF1,0xF0, // 'c'
	0x00,0x30,0x03,0x00,0x30,0x03,0x00,0x31,0xF3,0x3F,0xF7,0x0F,0x60,0x76,0x03,0xC0,0x3C,0x03,0xC0,0x36,0x03,0x60,0x77,0x0F,0x3F,0xF1,0xF3, // 'd'
	0x1F,0x07,0xF1,0xC3,0x30,0x6C,0x07,0xFF,0xFF,0xFE,0x00,0xE0,0x0C,0x01,0xC1,0x1F,0xE0,0xF8, // 'e'
	0x0F,0x8F,0xC6,0x03,0x01,0x83,0xFB,0xFC,0x30,0x18,0x0C,0x06,0x03,0x01,0x80,0xC0,0x60,0x30,0x18,0x0C,0x00, // 'f'
	0x0F,0xF3,0xFF,0x30,0xC6,0x06,0x60,0x63,0x0E,0x3F,0xC1,0xF8,0x10,0x03,0x00,0x30,0x01,0xFE,0x3F,0xF6,0x03,0x40,0x3C,0x03,0x60,0x77,0xFE,0x1F,0x80, // 'g'
	0xC0,0x18,0x03,0x00,0x60,0x0C,0x01,0x9F,0x3F,0xF7,0x87,0xE0,0x78,0x0F,0x01,0xE0,0x3C,0x07,0x80,0xF0,0x1E,0x03,0xC0,0x78,0x0C, // 'h'
	0xFC,0x3F,0xFF,0xFF,0xF0, // 'i'
	0x18,0xC6,0x00,0x0C,0x63,0x18,0xC6,0x31,0x8C,0x63,0x18,0xC6,0x31,0x8F,0xFE, // 'j'
	0xC0,0x18,0x03,0x00,0x60,0x0C,0x01,0x83,0xB0,0x66,0x18,0xC6,0x19,0x83,0x60,0x7E,0x0E,0xE1,0x8C,0x30,0xC6,0x1C,0xC1,0xD8,0x18, // 'k'
	0xFF,0xFF,0xFF,0xFF,0xF0, // 'l'
	0xCF,0x0F,0x3F,0xEF,0xEE,0x1F,0x1F,0x83,0x83,0xC0,0xC0,0xF0,0x30,0x3C,0x0C,0x0F,0x03,0x03,0xC0,0xC0,0xF0,0x30,0x3C,0x0C,0x0F,0x03,0x03,0xC0,0xC0,0xC0, // 'm'
	0xCF,0x9F,0xFB,0xC3,0xF0,0x3C,0x07,0x80,0xF0,0x1E,0x03,0xC0,0x78,0x0F,0x01,0xE0,0x3C,0x06, // 'n'
	0x0F,0x81,0xFF,0x1C,0x1C,0xC0,0x66,0x03,0x60,0x0F,0x00,0x78,0x03,0x60,0x33,0x01,0x9C,0x1C,0x7F,0xC0,0xF8,0x00, // 'o'
	0xCF,0x8F,0xFC,0xF0,0xEE,0x06,0xC0,0x6C,0x03,0xC0,0x3C,0x03,0xC0,0x6E,0x06,0xF0,0xEF,0xFC,0xCF,0x8C,0x00,0xC0,0x0C,0x00,0xC0,0x0C,0x00,0xC0,0x00, // 'p'
	0x1F,0x33,0xFF,0x70,0xF6,0x07,0x60,0x3C,0x03,0xC0,0x3E,0x03,0x60,0x36,0x07,0x70,0xF3,0xFF,0x1F,0x30,0x03,0x00,0x30,0x03,0x00,0x30,0x03,0x00,0x30, // 'q'
	0xCF,0xDE,0xF0,0xE0,0xC0,0xC0,0xC0,0xC0,0xC0,0xC0,0xC0,0xC0,0xC0, // 'r'
	0x3E,0x3F,0xF0,0x58,0x0E,0x03,0xC0,0x78,0x0E,0x01,0x80,0xC0,0xFF,0xEF,0xE0, // 's'
	0x10,0x10,0x30,0x7E,0xFE,0x30,0x30,0x30,0x30,0x30,0x30,0x30,0x30,0x38,0x1F,0x1F, // 't'
	0xC0,0x78,0x0F,0x01,0xE0,0x3C,0x07,0x80,0xF0,0x1E,0x03,0xC0,0x78,0x1F,0x87,0xBF,0xF3,0xE6, // 'u'
	0xC0,0x36,0x06,0x60,0x66,0x06,0x30,0xC3,0x0C,0x30,0xC1,0x98,0x19,0x80,0x90,0x0F,0x00,0xF0,0x06,0x00, // 'v'
	0xC0,0xE0,0x6C,0x1C,0x19,0x82,0x83,0x30,0xD8,0x63,0x1B,0x18,0x63,0x63,0x0C,0xC6,0x61,0x98,0xCC,0x1B,0x1B,0x03,0xC1,0xE0,0x78,0x3C,0x0F,0x07,0x80,0xC0,0x60, // 'w'
	0xC0,0x66,0x0C,0x31,0x83,0x18,0x1B,0x00,0xE0,0x0E,0x00,0xE0,0x1B,0x03,0x18,0x71,0xC6,0x0C,0xC0,0x60, // 'x'
	0xC0,0x36,0x06,0x60,0x66,0x06,0x30,0xC3,0x0C,0x18,0xC1,0x98,0x19,0x80,0xD8,0x0F,0x00,0x70,0x06,0x00,0x60,0x06,0x00,0xC0,0x1C,0x0F,0x80,0xF0,0x00, // 'y'
	0xFF,0xFF,0xC0,0xC0,0x40,0x60,0x60,0x20,0x30,0x30,0x30,0x18,0x1F,0xFF,0xF8, // 'z'
	0x06,0x3C,0x60,0xC1,0x83,0x06,0x0C,0x30,0xE3,0x03,0x83,0x03,0x06,0x0C,0x18,0x30,0x60,0xF0,0x60, // '{'
	0xFF,0xFF,0xFF, // '|'
	0xC1,0xE0,0xC1,0x83,0x06,0x0C,0x18,0x18,0x38,0x18,0xE1,0x86,0x0C,0x18,0x30,0x60,0xC7,0x8C,0x00 // '}'
};
const GFXglyph Open_Sans_Regular_24Glyphs[] PROGMEM = {
// bitmapOffset, width, height, xAdvance, xOffset, yOffset
	  {     0,   1,   1,   7,    0,   -1 }, // ' '
	  {     1,   3,  17,   7,    2,  -17 }, // '!'
	  {     8,   6,   6,  11,    2,  -17 }, // '"'
	  {    13,  14,  17,  17,    1,  -17 }, // '#'
	  {    43,  10,  19,  15,    2,  -18 }, // '$'
	  {    67,  18,  17,  21,    1,  -17 }, // '%'
	  {   106,  17,  17,  19,    1,  -17 }, // '&'
	  {   143,   3,   6,   6,    1,  -17 }, // '''
	  {   146,   5,  21,   8,    1,  -17 }, // '('
	  {   160,   5,  21,   8,    1,  -17 }, // ')'
	  {   174,  11,  11,  14,    1,  -18 }, // '*'
	  {   190,  12,  12,  15,    1,  -14 }, // '+'
	  {   208,   3,   6,   7,    1,   -3 }, // ','
	  {   211,   6,   2,   9,    1,   -7 }, // '-'
	  {   213,   3,   3,   7,    2,   -3 }, // '.'
	  {   215,   9,  17,  10,    0,  -17 }, // '/'
	  {   235,  12,  17,  15,    1,  -17 }, // '0'
	  {   261,   7,  17,  15,    2,  -17 }, // '1'
	  {   276,  12,  17,  15,    1,  -17 }, // '2'
	  {   302,  12,  17,  15,    1,  -17 }, // '3'
	  {   328,  13,  17,  15,    1,  -17 }, // '4'
	  {   356,  11,  17,  15,    2,  -17 }, // '5'
	  {   380,  12,  17,  15,    1,  -17 }, // '6'
	  {   406,  12,  17,  15,    1,  -17 }, // '7'
	  {   432,  12,  17,  15,    1,  -17 }, // '8'
	  {   458,  12,  17,  15,    1,  -17 }, // '9'
	  {   484,   3,  13,   7,    2,  -13 }, // ':'
	  {   489,   3,  16,   7,    1,  -13 }, // ';'
	  {   495,  12,  12,  15,    1,  -15 }, // '<'
	  {   513,  12,   7,  15,    1,  -12 }, // '='
	  {   524,  12,  12,  15,    1,  -15 }, // '>'
	  {   542,   9,  17,  11,    0,  -17 }, // '?'
	  {   562,  20,  19,  23,    1,  -17 }, // '@'
	  {   610,  15,  17,  16,    0,  -17 }, // 'A'
	  {   642,  13,  17,  17,    2,  -17 }, // 'B'
	  {   670,  13,  17,  16,    1,  -17 }, // 'C'
	  {   698,  15,  17,  19,    2,  -17 }, // 'D'
	  {   730,  10,  17,  14,    2,  -17 }, // 'E'
	  {   752,  10,  17,  13,    2,  -17 }, // 'F'
	  {   774,  14,  17,  18,    1,  -17 }, // 'G'
	  {   804,  14,  17,  19,    2,  -17 }, // 'H'
	  {   834,   2,  17,   8,    2,  -17 }, // 'I'
	  {   839,   6,  21,   7,   -2,  -17 }, // 'J'
	  {   855,  13,  17,  16,    2,  -17 }, // 'K'
	  {   883,   9,  17,  13,    2,  -17 }, // 'L'
	  {   903,  18,  17,  23,    2,  -17 }, // 'M'
	  {   942,  14,  17,  19,    2,  -17 }, // 'N'
	  {   972,  17,  17,  20,    1,  -17 }, // 'O'
	  {  1009,  11,  17,  15,    2,  -17 }, // 'P'
	  {  1033,  17,  21,  20,    1,  -17 }, // 'Q'
	  {  1078,  13,  17,  16,    2,  -17 }, // 'R'
	  {  1106,  11,  17,  14,    1,  -17 }, // 'S'
	  {  1130,  13,  17,  14,    0,  -17 }, // 'T'
	  {  1158,  13,  17,  18,    2,  -17 }, // 'U'
	  {  1186,  14,  17,  15,    0,  -17 }, // 'V'
	  {  1216,  22,  17,  23,    0,  -17 }, // 'W'
	  {  1263,  14,  17,  15,    0,  -17 }, // 'X'
	  {  1293,  13,  17,  14,    0,  -17 }, // 'Y'
	  {  1321,  12,  17,  15,    1,  -17 }, // 'Z'
	  {  1347,   5,  21,   9,    2,  -17 }, // '['
	  {  1361,   9,  17,  10,    0,  -17 }, // '\'
	  {  1381,   5,  21,   9,    1,  -17 }, // ']'
	  {  1395,  11,  11,  14,    1,  -17 }, // '^'
	  {  1411,  11,   2,  12,    0,    2 }, // '_'
	  {  1414,   4,   4,  15,    5,  -19 }, // '`'
	  {  1416,  10,  13,  14,    1,  -13 }, // 'a'
	  {  1433,  12,  18,  16,    2,  -18 }, // 'b'
	  {  1460,   9,  13,  12,    1,  -13 }, // 'c'
	  {  1475,  12,  18,  16,    1,  -18 }, // 'd'
	  {  1502,  11,  13,  14,    1,  -13 }, // 'e'
	  {  1520,   9,  18,   9,    0,  -18 }, // 'f'
	  {  1541,  12,  19,  14,    0,  -13 }, // 'g'
	  {  1570,  11,  18,  16,    2,  -18 }, // 'h'
	  {  1595,   2,  18,   7,    2,  -18 }, // 'i'
	  {  1600,   5,  24,   7,   -1,  -18 }, // 'j'
	  {  1615,  11,  18,  14,    2,  -18 }, // 'k'
	  {  1640,   2,  18,   7,    2,  -18 }, // 'l'
	  {  1645,  18,  13,  23,    2,  -13 }, // 'm'
	  {  1675,  11,  13,  16,    2,  -13 }, // 'n'
	  {  1693,  13,  13,  16,    1,  -13 }, // 'o'
	  {  1715,  12,  19,  16,    2,  -13 }, // 'p'
	  {  1744,  12,  19,  16,    1,  -13 }, // 'q'
	  {  1773,   8,  13,  11,    2,  -13 }, // 'r'
	  {  1786,   9,  13,  12,    1,  -13 }, // 's'
	  {  1801,   8,  16,   9,    0,  -16 }, // 't'
	  {  1817,  11,  13,  16,    2,  -13 }, // 'u'
	  {  1835,  12,  13,  13,    0,  -13 }, // 'v'
	  {  1855,  19,  13,  20,    0,  -13 }, // 'w'
	  {  1886,  12,  13,  14,    1,  -13 }, // 'x'
	  {  1906,  12,  19,  13,    0,  -13 }, // 'y'
	  {  1935,   9,  13,  12,    1,  -13 }, // 'z'
	  {  1950,   7,  21,  10,    1,  -17 }, // '{'
	  {  1969,   1,  24,  14,    6,  -18 }, // '|'
	  {  1972,   7,  21,  10,    1,  -17 } // '}'
};
const GFXfont Open_Sans_Regular_24 PROGMEM = {
(uint8_t  *)Open_Sans_Regular_24Bitmaps,(GFXglyph *)Open_Sans_Regular_24Glyphs,0x20, 0x7E, 33};