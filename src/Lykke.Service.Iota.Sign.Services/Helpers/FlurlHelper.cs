﻿using Flurl.Http;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.Dash.Sign.Services.Helpers
{
    public class FlurlHelper
    {
        public static async Task<T> GetJsonAsync<T>(string url, int tryCount = 3, int tryDelayMs = 100)
        {
            return await Retry.Try(() => url.GetJsonAsync<T>(), NeedToRetryException, tryCount, tryDelayMs);
        }

        public static async Task PostJsonAsync(string url, object data, int tryCount = 3, int tryDelayMs = 100)
        {
            await Retry.Try(() => url.PostJsonAsync(data), NeedToRetryException, tryCount, tryDelayMs);
        }

        private static bool NeedToRetryException(Exception ex)
        {
            if (!(ex is FlurlHttpException flurlException))
            {
                return false;
            }

            var isTimeout = flurlException is FlurlHttpTimeoutException;
            if (isTimeout)
            {
                return true;
            }

            if (flurlException.Call.HttpStatus == HttpStatusCode.ServiceUnavailable ||
                flurlException.Call.HttpStatus == HttpStatusCode.InternalServerError ||
                flurlException.Call.HttpStatus == HttpStatusCode.BadGateway)
            {
                return true;
            }

            return false;
        }
    }
}
