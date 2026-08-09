#pragma once
#include "pch.h"

namespace pt {
    // Faz template matching (SSD normalizado) usando um BMP 24bpp.
    // ROI = região de busca em coordenadas de tela.
    // Retorna true e escreve foundX/Y (centro do match) se score >= threshold (0..1).
    bool FindTemplateInScreen(const std::string& bmpPath,
                              int roiX, int roiY, int roiW, int roiH,
                              double threshold,
                              int& foundX, int& foundY);
}
