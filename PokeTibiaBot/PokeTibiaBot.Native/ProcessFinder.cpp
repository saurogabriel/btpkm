#include "ProcessFinder.h"
#include <algorithm>
#include <cctype>

namespace pt {

    static std::string ToLower(std::string s) {
        std::transform(s.begin(), s.end(), s.begin(),
            [](unsigned char c) { return (char)std::tolower(c); });
        return s;
    }

    struct FindCtx { std::string needle; HWND result; };

    static BOOL CALLBACK EnumProc(HWND hwnd, LPARAM lp) {
        auto* ctx = reinterpret_cast<FindCtx*>(lp);
        char buf[512] = { 0 };
        GetWindowTextA(hwnd, buf, sizeof(buf) - 1);
        if (buf[0] && IsWindowVisible(hwnd)) {
            std::string title = ToLower(buf);
            if (title.find(ctx->needle) != std::string::npos) {
                ctx->result = hwnd;
                return FALSE;
            }
        }
        return TRUE;
    }

    HWND FindWindowByTitleContains(const std::string& part) {
        FindCtx ctx{ ToLower(part), nullptr };
        EnumWindows(EnumProc, reinterpret_cast<LPARAM>(&ctx));
        return ctx.result;
    }

    bool GetWindowRectSafe(HWND hwnd, int& x, int& y, int& w, int& h) {
        if (!hwnd) return false;
        RECT r; if (!GetWindowRect(hwnd, &r)) return false;
        x = r.left; y = r.top; w = r.right - r.left; h = r.bottom - r.top;
        return true;
    }

    DWORD GetPidFromHwnd(HWND hwnd) {
        if (!hwnd) return 0;
        DWORD pid = 0; GetWindowThreadProcessId(hwnd, &pid); return pid;
    }

    HANDLE OpenProcessByName(const std::string& procName) {
        std::string target = ToLower(procName);
        HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snap == INVALID_HANDLE_VALUE) return nullptr;
        PROCESSENTRY32 pe{ sizeof(pe) };
        DWORD pid = 0;
        if (Process32First(snap, &pe)) {
            do {
                std::string exe = ToLower(pe.szExeFile);
                if (exe == target) { pid = pe.th32ProcessID; break; }
            } while (Process32Next(snap, &pe));
        }
        CloseHandle(snap);
        if (!pid) return nullptr;
        return OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, FALSE, pid);
    }
}
