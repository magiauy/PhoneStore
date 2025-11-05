using System;
using System.Threading;

namespace PhoneStoreAdmin.Utils
{
    public static class AsyncResourceCleanupHelper
    {
        public static void DisposeTimer(ref Timer? timer)
        {
            var timerToDispose = timer;
            timer = null;
            timerToDispose?.Dispose();
        }

        public static void CancelAndDispose(ref CancellationTokenSource? cancellationTokenSource)
        {
            var cts = cancellationTokenSource;
            cancellationTokenSource = null;

            if (cts == null)
            {
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignore: already disposed elsewhere.
            }
            finally
            {
                cts.Dispose();
            }
        }

        public static void CleanupDebounceAndCaching(ref Timer? timer, ref CancellationTokenSource? cancellationTokenSource)
        {
            DisposeTimer(ref timer);
            CancelAndDispose(ref cancellationTokenSource);
        }
    }
}
