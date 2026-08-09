#pragma once
#include "pch.h"

namespace pt {

    struct ScanSession {
        HANDLE hProc = nullptr;
        // Endereços que ainda batem o filtro atual
        std::vector<uintptr_t> addresses;
        // Últimos valores lidos para cada endereço (para "next scan" comparativo)
        std::vector<int32_t> values;
    };

    // Cria uma nova sessão anexada a um processo pelo nome (ex.: "poketibia.exe").
    ScanSession* CreateSession(const std::string& procName);
    void DestroySession(ScanSession* s);

    // Primeira varredura: procura int32 == value em todas as regiões RW privadas.
    // Retorna quantidade de matches. Limita a maxResults para evitar explosão.
    size_t FirstScanInt32(ScanSession* s, int32_t value, size_t maxResults);

    // Varredura seguinte: filtra endereços existentes por int32 == value.
    size_t NextScanInt32(ScanSession* s, int32_t value);

    // Varredura seguinte por comparação com valor anterior:
    // 0=unchanged, 1=changed, 2=increased, 3=decreased
    size_t NextScanCompare(ScanSession* s, int mode);

    // Preenche o buffer com até maxOut resultados: pares (address, currentValue).
    // Retorna quantos foram escritos.
    size_t GetResults(ScanSession* s, size_t offset, size_t maxOut,
                      uint64_t* addrOut, int32_t* valOut);

    size_t ResultCount(ScanSession* s);
}
