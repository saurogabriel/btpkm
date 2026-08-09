#pragma once
#include "pch.h"

namespace pt {
    // Encontra HWND cujo título contém o texto (case-insensitive).
    HWND FindWindowByTitleContains(const std::string& part);

    // Retorna bounds da janela.
    bool GetWindowRectSafe(HWND hwnd, int& x, int& y, int& w, int& h);

    // Abre processo pelo nome (poketibia.exe, etc). Retorna HANDLE ou NULL.
    HANDLE OpenProcessByName(const std::string& procName);

    // PID do processo dono da janela.
    DWORD GetPidFromHwnd(HWND hwnd);
}
