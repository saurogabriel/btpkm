#include "ImageProcessor.h"
#include "ScreenCapture.h"
#include <fstream>
#include <cmath>

namespace pt {

#pragma pack(push, 1)
    struct BmpHeader {
        uint16_t bfType;
        uint32_t bfSize;
        uint16_t bfReserved1, bfReserved2;
        uint32_t bfOffBits;
        uint32_t biSize;
        int32_t  biWidth;
        int32_t  biHeight;
        uint16_t biPlanes;
        uint16_t biBitCount;
        uint32_t biCompression;
        uint32_t biSizeImage;
        int32_t  biXPelsPerMeter, biYPelsPerMeter;
        uint32_t biClrUsed, biClrImportant;
    };
#pragma pack(pop)

    static bool LoadBmp24(const std::string& path, std::vector<uint8_t>& outRGB, int& w, int& h) {
        std::ifstream f(path, std::ios::binary);
        if (!f) return false;
        BmpHeader hdr{}; f.read(reinterpret_cast<char*>(&hdr), sizeof(hdr));
        if (hdr.bfType != 0x4D42 || hdr.biBitCount != 24) return false;
        w = hdr.biWidth; h = std::abs(hdr.biHeight);
        f.seekg(hdr.bfOffBits, std::ios::beg);
        int rowSize = ((24 * w + 31) / 32) * 4;
        std::vector<uint8_t> row(rowSize);
        outRGB.resize((size_t)w * h * 3);
        for (int y = 0; y < h; y++) {
            f.read(reinterpret_cast<char*>(row.data()), rowSize);
            int destY = (hdr.biHeight > 0) ? (h - 1 - y) : y;
            for (int x = 0; x < w; x++) {
                outRGB[(destY * w + x) * 3 + 0] = row[x * 3 + 2]; // R
                outRGB[(destY * w + x) * 3 + 1] = row[x * 3 + 1]; // G
                outRGB[(destY * w + x) * 3 + 2] = row[x * 3 + 0]; // B
            }
        }
        return true;
    }

    bool FindTemplateInScreen(const std::string& bmpPath,
        int roiX, int roiY, int roiW, int roiH,
        double threshold,
        int& foundX, int& foundY)
    {
        std::vector<uint8_t> tpl; int tw = 0, th = 0;
        if (!LoadBmp24(bmpPath, tpl, tw, th)) return false;
        if (tw <= 0 || th <= 0 || tw > roiW || th > roiH) return false;

        std::vector<uint8_t> shot; // BGRA
        if (!CaptureRect(roiX, roiY, roiW, roiH, shot)) return false;

        double bestScore = 1e18; int bestX = -1, bestY = -1;
        double norm = (double)(tw * th * 3) * 255.0 * 255.0;

        for (int y = 0; y <= roiH - th; y += 2) {
            for (int x = 0; x <= roiW - tw; x += 2) {
                double sumSq = 0.0;
                for (int ty = 0; ty < th; ty += 2) {
                    for (int tx = 0; tx < tw; tx += 2) {
                        int si = ((y + ty) * roiW + (x + tx)) * 4;
                        int ti = (ty * tw + tx) * 3;
                        int dr = (int)shot[si + 2] - (int)tpl[ti + 0];
                        int dg = (int)shot[si + 1] - (int)tpl[ti + 1];
                        int db = (int)shot[si + 0] - (int)tpl[ti + 2];
                        sumSq += dr * dr + dg * dg + db * db;
                    }
                }
                if (sumSq < bestScore) { bestScore = sumSq; bestX = x; bestY = y; }
            }
        }
        double score = 1.0 - (bestScore / norm);
        if (score >= threshold && bestX >= 0) {
            foundX = roiX + bestX + tw / 2;
            foundY = roiY + bestY + th / 2;
            return true;
        }
        return false;
    }
}
