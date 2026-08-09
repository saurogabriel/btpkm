#pragma once
#include "pch.h"

namespace pt {
    // Lê 4 bytes (int32) do processo remoto no endereço dado.
    // Retorna 0 em caso de falha. Para clientes onde você sabe offsets.
    int32_t ReadRemoteInt32(HANDLE hProc, uintptr_t address);
}
