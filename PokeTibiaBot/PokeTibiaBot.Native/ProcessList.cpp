#include "ProcessList.h"

namespace pt {
    std::vector<ProcInfo> ListProcesses(size_t maxOut) {
        std::vector<ProcInfo> out;
        HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snap == INVALID_HANDLE_VALUE) return out;
        PROCESSENTRY32 pe{ sizeof(pe) };
        if (Process32First(snap, &pe)) {
            do {
                if (out.size() >= maxOut) break;
                out.push_back({ pe.th32ProcessID, std::string(pe.szExeFile) });
            } while (Process32Next(snap, &pe));
        }
        CloseHandle(snap);
        return out;
    }
}
