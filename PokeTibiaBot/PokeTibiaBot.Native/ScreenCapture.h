#pragma once
#include "pch.h"

namespace pt {
    // Lê pixel RGB da tela (screen coords). Retorna 0xRRGGBB.
    uint32_t ReadScreenPixel(int x, int y);

    // Copia um retângulo da tela para um vetor RGBA.
    // Retorna true em caso de sucesso; out é largura*altura*4 bytes.
    bool CaptureRect(int x, int y, int w, int h, std::vector<uint8_t>& outRGBA);

    // % de preenchimento (0..100) de barra horizontal, contando pixels dentro da
    // tolerância da cor esperada.
    int BarPercent(int x, int y, int width,
                   int expR, int expG, int expB, int tolerance);
}
