#include "pch.h"
#include "ProcessFinder.h"
#include "ProcessList.h"
#include "ScreenCapture.h"
#include "ImageProcessor.h"
#include "MemoryReader.h"
#include "MemoryScanner.h"

// ==== Exports (C ABI) chamados via P/Invoke pelo C# ====

PT_API HWND __cdecl FindGameWindow(const char* titleContains) {
    if (!titleContains) return nullptr;
    return pt::FindWindowByTitleContains(titleContains);
}

PT_API bool __cdecl GetWindowBounds(HWND h, int* x, int* y, int* w, int* hOut) {
    if (!x || !y || !w || !hOut) return false;
    return pt::GetWindowRectSafe(h, *x, *y, *w, *hOut);
}

PT_API bool __cdecl BringWindowFront(HWND h) {
    if (!h) return false;
    if (IsIconic(h)) ShowWindow(h, SW_RESTORE);
    return SetForegroundWindow(h) != 0;
}

PT_API uint32_t __cdecl ReadPixel(int x, int y) {
    return pt::ReadScreenPixel(x, y);
}

PT_API int __cdecl ReadBarPercent(int x, int y, int width,
                                   int r, int g, int b, int tolerance) {
    return pt::BarPercent(x, y, width, r, g, b, tolerance);
}

PT_API int __cdecl FindTemplate(const char* path,
                                 int roiX, int roiY, int roiW, int roiH,
                                 double threshold,
                                 int* foundX, int* foundY) {
    if (!path || !foundX || !foundY) return 0;
    int fx = 0, fy = 0;
    bool ok = pt::FindTemplateInScreen(path, roiX, roiY, roiW, roiH, threshold, fx, fy);
    *foundX = fx; *foundY = fy;
    return ok ? 1 : 0;
}

PT_API HANDLE __cdecl OpenProcessByName(const char* procName) {
    if (!procName) return nullptr;
    return pt::OpenProcessByName(procName);
}

PT_API int __cdecl ReadInt32(HANDLE h, uint64_t addr) {
    return pt::ReadRemoteInt32(h, (uintptr_t)addr);
}

PT_API void __cdecl CloseProcessHandle(HANDLE h) {
    if (h) CloseHandle(h);
}

// ==== Memory Scanner ====

PT_API pt::ScanSession* __cdecl Scanner_Create(const char* procName) {
    if (!procName) return nullptr;
    return pt::CreateSession(procName);
}

PT_API void __cdecl Scanner_Destroy(pt::ScanSession* s) {
    pt::DestroySession(s);
}

PT_API uint64_t __cdecl Scanner_FirstScan(pt::ScanSession* s, int32_t value, uint64_t maxResults) {
    return (uint64_t)pt::FirstScanInt32(s, value, (size_t)maxResults);
}

PT_API uint64_t __cdecl Scanner_NextScan(pt::ScanSession* s, int32_t value) {
    return (uint64_t)pt::NextScanInt32(s, value);
}

PT_API uint64_t __cdecl Scanner_NextCompare(pt::ScanSession* s, int mode) {
    return (uint64_t)pt::NextScanCompare(s, mode);
}

PT_API uint64_t __cdecl Scanner_Count(pt::ScanSession* s) {
    return (uint64_t)pt::ResultCount(s);
}

PT_API uint64_t __cdecl Scanner_GetResults(pt::ScanSession* s, uint64_t offset, uint64_t maxOut,
                                            uint64_t* addrs, int32_t* vals) {
    return (uint64_t)pt::GetResults(s, (size_t)offset, (size_t)maxOut, addrs, vals);
}

// ==== Process listing ====
// Preenche names[] com nomes (string, terminados em NUL) e pids[] com PIDs.
// namesBuf deve ter tamanho suficiente; retorna quantos processos foram enumerados.
// Cada nome ocupa exatamente 260 bytes no buffer (MAX_PATH), padded com zeros.
PT_API uint32_t __cdecl ListProcesses(char* namesBuf, uint32_t* pids, uint32_t maxOut) {
    if (!namesBuf || !pids || maxOut == 0) return 0;
    auto list = pt::ListProcesses(maxOut);
    memset(namesBuf, 0, (size_t)maxOut * 260);
    for (size_t i = 0; i < list.size(); i++) {
        pids[i] = list[i].pid;
        strncpy_s(namesBuf + i * 260, 260, list[i].name.c_str(), _TRUNCATE);
    }
    return (uint32_t)list.size();
}
