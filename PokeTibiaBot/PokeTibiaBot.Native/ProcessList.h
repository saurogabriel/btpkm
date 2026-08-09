#pragma once
#include "pch.h"

namespace pt {
    struct ProcInfo { DWORD pid; std::string name; };
    // Lista processos visíveis. Retorna até maxOut entradas.
    std::vector<ProcInfo> ListProcesses(size_t maxOut);
}
