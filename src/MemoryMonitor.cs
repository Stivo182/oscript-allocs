using System;
using ScriptEngine.Machine.Contexts;

#if NET8_0_OR_GREATER
using OneScript.Commons;
using OneScript.Contexts;
using OneScript.Exceptions;
#elif NET48
using ScriptEngine;
using ScriptEngine.Machine;
#endif

namespace AllocsOneScript
{
    [ContextClass("МониторПамяти", "MemoryMonitor")]
    public class MemoryMonitor : AutoContext<MemoryMonitor>
    {
        private long _startAllocatedBytes;
        private bool _isStarted;

        [ScriptConstructor]
        public static MemoryMonitor Constructor()
        {
            return new MemoryMonitor();
        }

        [ContextMethod("Начать", "Start")]
        public void Start()
        {
            if (_isStarted)
                throw new RuntimeException(
                    Locale.NStr(
                        "ru = 'Замер памяти уже начат'; en = 'Memory measurement has already been started'"));

            _startAllocatedBytes = GetAllocatedBytes();
            _isStarted = true;
        }

        [ContextMethod("Завершить", "Stop")]
        public decimal Stop()
        {
            if (!_isStarted)
                throw new RuntimeException(
                    Locale.NStr(
                        "ru = 'Замер памяти не был начат'; en = 'Memory measurement has not been started'"));

            long endAllocatedBytes = GetAllocatedBytes();
            _isStarted = false;

            if (endAllocatedBytes < _startAllocatedBytes)
                throw new RuntimeException(
                    Locale.NStr(
                        "ru = 'Счётчик аллокаций уменьшился; Start и Stop могли выполниться на разных потоках'; " +
                        "en = 'The allocation counter decreased; Start and Stop may have run on different threads'"));

            return endAllocatedBytes - _startAllocatedBytes;
        }

        [ContextMethod("РазмерКучи", "HeapSize")]
        public decimal HeapSize() => GC.GetTotalMemory(true);

        [ContextMethod("ВсегоВыделеноБайт", "TotalAllocatedBytes")]
        public decimal GetTotalAllocatedBytes() => GetAllocatedBytes();

        private long GetAllocatedBytes()
        {
#if NET8_0_OR_GREATER
            return GC.GetTotalAllocatedBytes(precise: true);
#elif NET48
            if (CanUseMonitoringTotalAllocatedMemorySize)
                return AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;

            // AppDomain monitoring is unavailable on Mono. The net48 reference
            // assemblies expose this API directly, so reflection is unnecessary.
            return GC.GetAllocatedBytesForCurrentThread();
#else
#error Unsupported target framework
#endif
        }

#if NET48
        private static readonly bool CanUseMonitoringTotalAllocatedMemorySize =
            TryEnableMonitoringTotalAllocatedMemorySize();

        private static bool TryEnableMonitoringTotalAllocatedMemorySize()
        {
            try
            {
                if (!AppDomain.MonitoringIsEnabled)
                    AppDomain.MonitoringIsEnabled = true;

                return AppDomain.MonitoringIsEnabled
                    && AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize >= 0;
            }
            catch
            {
                return false;
            }
        }
#endif
    }
}
