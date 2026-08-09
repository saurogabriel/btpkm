#pragma once
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <TlHelp32.h>
#include <psapi.h>
#include <string>
#include <vector>
#include <functional>
#include <cstdint>

#ifdef POKETIBIA_EXPORTS
#define PT_API extern "C" __declspec(dllexport)
#else
#define PT_API extern "C" __declspec(dllimport)
#endif
