using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MewgenicsSaveGuardian.Services;

public class SpeedHackService : IDisposable
{
    private const string SHARED_MEMORY_NAME = "BBS_SpeedHack";

    private MemoryMappedFile? _mmf;
    private MemoryMappedViewAccessor? _accessor;
    private bool _disposed;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize,
        uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
        byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes,
        uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags,
        out uint lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandleA(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

    private const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_RELEASE = 0x8000;
    private const uint PAGE_READWRITE = 0x04;

    public void EnsureSharedMemory(double initialSpeed = 1.0)
    {
        if (_mmf != null) return;

        _mmf = MemoryMappedFile.CreateOrOpen(SHARED_MEMORY_NAME, sizeof(double));
        _accessor = _mmf.CreateViewAccessor(0, sizeof(double));
        _accessor.Write(0, initialSpeed);
    }

    public void SetSpeed(double multiplier)
    {
        EnsureSharedMemory(multiplier);
        _accessor?.Write(0, multiplier);
    }

    public bool InjectDll(int processId, string dllPath)
    {
        var fullPath = Path.GetFullPath(dllPath);
        if (!File.Exists(fullPath))
            return false;

        var hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
        if (hProcess == IntPtr.Zero)
            return false;

        try
        {
            var hKernel32 = GetModuleHandleA("kernel32.dll");
            var loadLibAddr = GetProcAddress(hKernel32, "LoadLibraryW");
            if (loadLibAddr == IntPtr.Zero)
                return false;

            var pathBytes = System.Text.Encoding.Unicode.GetBytes(fullPath + '\0');
            var pathAddr = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)pathBytes.Length,
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (pathAddr == IntPtr.Zero)
                return false;

            if (!WriteProcessMemory(hProcess, pathAddr, pathBytes, (uint)pathBytes.Length, out _))
            {
                VirtualFreeEx(hProcess, pathAddr, 0, MEM_RELEASE);
                return false;
            }

            var hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0,
                loadLibAddr, pathAddr, 0, out _);
            if (hThread == IntPtr.Zero)
            {
                VirtualFreeEx(hProcess, pathAddr, 0, MEM_RELEASE);
                return false;
            }

            WaitForSingleObject(hThread, 5000);
            CloseHandle(hThread);
            VirtualFreeEx(hProcess, pathAddr, 0, MEM_RELEASE);

            return true;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    public bool IsInjected(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName?.Equals("speedhack.dll", StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }
        }
        catch
        {
            // Process may have exited or access denied
        }
        return false;
    }

    public string GetDllPath()
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var dllPath = Path.Combine(exeDir, "speedhack.dll");
        if (File.Exists(dllPath))
            return dllPath;

        dllPath = Path.Combine(exeDir, "native", "speedhack.dll");
        if (File.Exists(dllPath))
            return dllPath;

        return string.Empty;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _accessor?.Dispose();
        _mmf?.Dispose();
        _accessor = null;
        _mmf = null;
    }
}
