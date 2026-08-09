#include "MemoryScanner.h"
#include "ProcessFinder.h"

namespace pt {

    ScanSession* CreateSession(const std::string& procName) {
        HANDLE h = OpenProcessByName(procName);
        if (!h) return nullptr;
        auto* s = new ScanSession();
        s->hProc = h;
        return s;
    }

    void DestroySession(ScanSession* s) {
        if (!s) return;
        if (s->hProc) CloseHandle(s->hProc);
        delete s;
    }

    // Enumera regiões de memória committed + RW + não-image para varrer.
    static void ForEachRegion(HANDLE hProc,
        const std::function<void(uintptr_t base, size_t size)>& cb) {
        MEMORY_BASIC_INFORMATION mbi{};
        uintptr_t addr = 0;
        while (VirtualQueryEx(hProc, (LPCVOID)addr, &mbi, sizeof(mbi)) == sizeof(mbi)) {
            bool ok = (mbi.State == MEM_COMMIT)
                && ((mbi.Protect & (PAGE_READWRITE | PAGE_EXECUTE_READWRITE)) != 0)
                && !(mbi.Protect & PAGE_GUARD)
                && (mbi.Type == MEM_PRIVATE || mbi.Type == MEM_MAPPED);
            if (ok) cb((uintptr_t)mbi.BaseAddress, (size_t)mbi.RegionSize);
            addr = (uintptr_t)mbi.BaseAddress + mbi.RegionSize;
            if (addr < (uintptr_t)mbi.BaseAddress) break; // overflow
        }
    }

    size_t FirstScanInt32(ScanSession* s, int32_t value, size_t maxResults) {
        if (!s || !s->hProc) return 0;
        s->addresses.clear();
        s->values.clear();
        std::vector<uint8_t> buf;

        ForEachRegion(s->hProc, [&](uintptr_t base, size_t size) {
            if (s->addresses.size() >= maxResults) return;
            buf.resize(size);
            SIZE_T read = 0;
            if (!ReadProcessMemory(s->hProc, (LPCVOID)base, buf.data(), size, &read)) return;
            size_t limit = read - (read % 4);
            for (size_t i = 0; i + 4 <= limit; i += 4) {
                int32_t v = *reinterpret_cast<int32_t*>(&buf[i]);
                if (v == value) {
                    s->addresses.push_back(base + i);
                    s->values.push_back(v);
                    if (s->addresses.size() >= maxResults) return;
                }
            }
        });
        return s->addresses.size();
    }

    size_t NextScanInt32(ScanSession* s, int32_t value) {
        if (!s || !s->hProc) return 0;
        std::vector<uintptr_t> keepA;
        std::vector<int32_t> keepV;
        keepA.reserve(s->addresses.size());
        keepV.reserve(s->addresses.size());
        for (auto addr : s->addresses) {
            int32_t v = 0; SIZE_T r = 0;
            if (ReadProcessMemory(s->hProc, (LPCVOID)addr, &v, 4, &r) && r == 4 && v == value) {
                keepA.push_back(addr);
                keepV.push_back(v);
            }
        }
        s->addresses = std::move(keepA);
        s->values = std::move(keepV);
        return s->addresses.size();
    }

    size_t NextScanCompare(ScanSession* s, int mode) {
        if (!s || !s->hProc) return 0;
        std::vector<uintptr_t> keepA;
        std::vector<int32_t> keepV;
        for (size_t i = 0; i < s->addresses.size(); i++) {
            int32_t cur = 0; SIZE_T r = 0;
            if (!ReadProcessMemory(s->hProc, (LPCVOID)s->addresses[i], &cur, 4, &r) || r != 4) continue;
            int32_t prev = s->values[i];
            bool keep = false;
            switch (mode) {
                case 0: keep = (cur == prev); break;
                case 1: keep = (cur != prev); break;
                case 2: keep = (cur > prev); break;
                case 3: keep = (cur < prev); break;
            }
            if (keep) { keepA.push_back(s->addresses[i]); keepV.push_back(cur); }
        }
        s->addresses = std::move(keepA);
        s->values = std::move(keepV);
        return s->addresses.size();
    }

    size_t GetResults(ScanSession* s, size_t offset, size_t maxOut,
                      uint64_t* addrOut, int32_t* valOut) {
        if (!s || !addrOut || !valOut) return 0;
        size_t n = 0;
        for (size_t i = offset; i < s->addresses.size() && n < maxOut; i++, n++) {
            int32_t cur = 0; SIZE_T r = 0;
            ReadProcessMemory(s->hProc, (LPCVOID)s->addresses[i], &cur, 4, &r);
            addrOut[n] = (uint64_t)s->addresses[i];
            valOut[n] = cur;
        }
        return n;
    }

    size_t ResultCount(ScanSession* s) { return s ? s->addresses.size() : 0; }
}
