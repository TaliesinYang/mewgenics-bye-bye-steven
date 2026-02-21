#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdint.h>
#include <psapi.h>

/* Shared memory layout: a single double (8 bytes) for speed multiplier */
static const char* SHMEM_NAME = "BBS_SpeedHack";
static HANDLE g_hMapFile = NULL;
static volatile double* g_pSpeedMultiplier = NULL;

/* Original function pointers */
static BOOL (WINAPI *Real_QueryPerformanceCounter)(LARGE_INTEGER* lpCounter) = NULL;
static ULONGLONG (WINAPI *Real_GetTickCount64)(void) = NULL;

/* State for QPC hooking */
static LARGE_INTEGER g_lastRealQPC;
static LARGE_INTEGER g_fakeQPC;
static int g_qpcInitialized = 0;

/* State for GetTickCount64 hooking */
static ULONGLONG g_lastRealTick = 0;
static ULONGLONG g_fakeTick = 0;
static int g_tickInitialized = 0;

static double GetSpeedMultiplier(void)
{
    if (g_pSpeedMultiplier)
        return *g_pSpeedMultiplier;
    return 1.0;
}

/* ================================================================ */
/* IAT Patching                                                     */
/* ================================================================ */

static void PatchIAT(HMODULE hModule, const char* dllName,
                     void* originalFunc, void* hookFunc)
{
    ULONG_PTR baseAddr = (ULONG_PTR)hModule;
    PIMAGE_DOS_HEADER dosHeader = (PIMAGE_DOS_HEADER)baseAddr;
    if (dosHeader->e_magic != IMAGE_DOS_SIGNATURE)
        return;

    PIMAGE_NT_HEADERS ntHeaders = (PIMAGE_NT_HEADERS)(baseAddr + dosHeader->e_lfanew);
    if (ntHeaders->Signature != IMAGE_NT_SIGNATURE)
        return;

    DWORD importRVA = ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress;
    if (importRVA == 0)
        return;

    PIMAGE_IMPORT_DESCRIPTOR importDesc = (PIMAGE_IMPORT_DESCRIPTOR)(baseAddr + importRVA);

    for (; importDesc->Name != 0; importDesc++)
    {
        const char* modName = (const char*)(baseAddr + importDesc->Name);
        if (_stricmp(modName, dllName) != 0)
            continue;

        PIMAGE_THUNK_DATA thunk = (PIMAGE_THUNK_DATA)(baseAddr + importDesc->FirstThunk);
        for (; thunk->u1.Function != 0; thunk++)
        {
            if ((void*)(uintptr_t)thunk->u1.Function == originalFunc)
            {
                DWORD oldProtect;
                VirtualProtect(&thunk->u1.Function, sizeof(uintptr_t),
                              PAGE_READWRITE, &oldProtect);
                thunk->u1.Function = (ULONG_PTR)hookFunc;
                VirtualProtect(&thunk->u1.Function, sizeof(uintptr_t),
                              oldProtect, &oldProtect);
                return;
            }
        }
    }
}

static void PatchAllModules(const char* dllName, void* originalFunc, void* hookFunc)
{
    HMODULE hMods[256];
    DWORD cbNeeded;
    HANDLE hProcess = GetCurrentProcess();

    if (EnumProcessModules(hProcess, hMods, sizeof(hMods), &cbNeeded))
    {
        DWORD count = cbNeeded / sizeof(HMODULE);
        for (DWORD i = 0; i < count; i++)
        {
            PatchIAT(hMods[i], dllName, originalFunc, hookFunc);
        }
    }
}

/* ================================================================ */
/* Hooked Functions                                                 */
/* ================================================================ */

static BOOL WINAPI Hooked_QueryPerformanceCounter(LARGE_INTEGER* lpCounter)
{
    LARGE_INTEGER realNow;
    Real_QueryPerformanceCounter(&realNow);

    if (!g_qpcInitialized)
    {
        g_lastRealQPC = realNow;
        g_fakeQPC = realNow;
        g_qpcInitialized = 1;
    }

    int64_t realElapsed = realNow.QuadPart - g_lastRealQPC.QuadPart;
    double speed = GetSpeedMultiplier();
    int64_t fakeElapsed = (int64_t)(realElapsed * speed);

    g_fakeQPC.QuadPart += fakeElapsed;
    g_lastRealQPC = realNow;

    if (lpCounter)
        *lpCounter = g_fakeQPC;
    return TRUE;
}

static ULONGLONG WINAPI Hooked_GetTickCount64(void)
{
    ULONGLONG realNow = Real_GetTickCount64();

    if (!g_tickInitialized)
    {
        g_lastRealTick = realNow;
        g_fakeTick = realNow;
        g_tickInitialized = 1;
    }

    ULONGLONG realElapsed = realNow - g_lastRealTick;
    double speed = GetSpeedMultiplier();
    ULONGLONG fakeElapsed = (ULONGLONG)(realElapsed * speed);

    g_fakeTick += fakeElapsed;
    g_lastRealTick = realNow;

    return g_fakeTick;
}

/* ================================================================ */
/* Shared Memory Setup                                              */
/* ================================================================ */

static BOOL SetupSharedMemory(void)
{
    g_hMapFile = OpenFileMappingA(FILE_MAP_READ, FALSE, SHMEM_NAME);
    if (g_hMapFile == NULL)
    {
        g_hMapFile = CreateFileMappingA(
            INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE,
            0, sizeof(double), SHMEM_NAME);
        if (g_hMapFile == NULL)
            return FALSE;
    }

    g_pSpeedMultiplier = (volatile double*)MapViewOfFile(
        g_hMapFile, FILE_MAP_READ, 0, 0, sizeof(double));

    if (g_pSpeedMultiplier == NULL)
    {
        CloseHandle(g_hMapFile);
        g_hMapFile = NULL;
        return FALSE;
    }

    return TRUE;
}

static void CleanupSharedMemory(void)
{
    if (g_pSpeedMultiplier)
    {
        UnmapViewOfFile((void*)g_pSpeedMultiplier);
        g_pSpeedMultiplier = NULL;
    }
    if (g_hMapFile)
    {
        CloseHandle(g_hMapFile);
        g_hMapFile = NULL;
    }
}

/* ================================================================ */
/* DLL Entry Point                                                  */
/* ================================================================ */

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    switch (fdwReason)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hinstDLL);

        if (!SetupSharedMemory())
            return FALSE;

        {
            HMODULE hKernel32 = GetModuleHandleA("kernel32.dll");
            if (!hKernel32)
                return FALSE;

            Real_QueryPerformanceCounter = (BOOL (WINAPI*)(LARGE_INTEGER*))
                GetProcAddress(hKernel32, "QueryPerformanceCounter");
            Real_GetTickCount64 = (ULONGLONG (WINAPI*)(void))
                GetProcAddress(hKernel32, "GetTickCount64");

            if (!Real_QueryPerformanceCounter || !Real_GetTickCount64)
                return FALSE;

            Real_QueryPerformanceCounter(&g_lastRealQPC);
            g_fakeQPC = g_lastRealQPC;
            g_qpcInitialized = 1;

            g_lastRealTick = Real_GetTickCount64();
            g_fakeTick = g_lastRealTick;
            g_tickInitialized = 1;

            PatchAllModules("kernel32.dll", Real_QueryPerformanceCounter,
                           Hooked_QueryPerformanceCounter);
            PatchAllModules("kernel32.dll", Real_GetTickCount64,
                           Hooked_GetTickCount64);
        }
        break;

    case DLL_PROCESS_DETACH:
        CleanupSharedMemory();
        break;
    }
    return TRUE;
}
