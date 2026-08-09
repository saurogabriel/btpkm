#include "MemoryReader.h"

namespace pt {
    int32_t ReadRemoteInt32(HANDLE hProc, uintptr_t address) {
        if (!hProc) return 0;
        int32_t value = 0; SIZE_T read = 0;
        if (!ReadProcessMemory(hProc, (LPCVOID)address, &value, sizeof(value), &read))
            return 0;
        return (read == sizeof(value)) ? value : 0;
    }
}
