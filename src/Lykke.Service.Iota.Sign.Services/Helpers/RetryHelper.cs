using System;
using System.Threading.Tasks;

namespace Lykke.Service.Dash.Sign.Services.Helpers
{
    public class Retry
    {
        public static async Task<T> Try<T>(Func<Task<T>> action,
            Func<Exception, bool> exceptionFilter,
            int tryCount,
            int delayAfterException = 0)
        {
            var @try = 0;

            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    @try++;

                    if (!exceptionFilter(ex) || @try >= tryCount)
                    {
                        throw;
                    }

                    if (delayAfterException > 0)
                    {
                        await Task.Delay(delayAfterException);
                    }
                }
            }
        }
    }
}
