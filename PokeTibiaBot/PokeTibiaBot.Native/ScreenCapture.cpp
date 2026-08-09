#include "ScreenCapture.h"
#include <cmath>

namespace pt {

    uint32_t ReadScreenPixel(int x, int y) {
        HDC hdc = GetDC(nullptr);
        COLORREF c = GetPixel(hdc, x, y);
        ReleaseDC(nullptr, hdc);
        // COLORREF é 0x00BBGGRR -> converter para 0xRRGGBB
        uint8_t r = GetRValue(c), g = GetGValue(c), b = GetBValue(c);
        return (uint32_t)((r << 16) | (g << 8) | b);
    }

    bool CaptureRect(int x, int y, int w, int h, std::vector<uint8_t>& outRGBA) {
        if (w <= 0 || h <= 0) return false;
        HDC screen = GetDC(nullptr);
        HDC mem = CreateCompatibleDC(screen);
        HBITMAP bmp = CreateCompatibleBitmap(screen, w, h);
        HGDIOBJ old = SelectObject(mem, bmp);

        BitBlt(mem, 0, 0, w, h, screen, x, y, SRCCOPY);

        BITMAPINFO bi{};
        bi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        bi.bmiHeader.biWidth = w;
        bi.bmiHeader.biHeight = -h; // top-down
        bi.bmiHeader.biPlanes = 1;
        bi.bmiHeader.biBitCount = 32;
        bi.bmiHeader.biCompression = BI_RGB;

        outRGBA.resize((size_t)w * h * 4);
        int ok = GetDIBits(mem, bmp, 0, h, outRGBA.data(), &bi, DIB_RGB_COLORS);

        SelectObject(mem, old);
        DeleteObject(bmp);
        DeleteDC(mem);
        ReleaseDC(nullptr, screen);
        return ok != 0;
    }

    int BarPercent(int x, int y, int width, int expR, int expG, int expB, int tolerance) {
        std::vector<uint8_t> px;
        if (!CaptureRect(x, y, width, 1, px)) return -1;
        int hits = 0;
        for (int i = 0; i < width; i++) {
            // ordem BGRA (Windows DIB)
            uint8_t b = px[i * 4 + 0];
            uint8_t g = px[i * 4 + 1];
            uint8_t r = px[i * 4 + 2];
            if (std::abs((int)r - expR) <= tolerance &&
                std::abs((int)g - expG) <= tolerance &&
                std::abs((int)b - expB) <= tolerance) {
                hits++;
            }
        }
        return (int)(hits * 100 / std::max(1, width));
    }
}
