using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

// Copied from `src/Microsoft.AspNet.Identity.Core/AsyncHelper.cs` at https://github.com/aspnet/AspNetIdentity
// Copyright (c) Microsoft Corporation, Inc. All rights reserved.
// Licensed under the MIT License, Version 2.0

namespace Whitestone.SegnoSharp.Shared.Helpers
{
    public static class AsyncHelper
    {
        private static readonly TaskFactory MyTaskFactory = new(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            CultureInfo cultureUi = CultureInfo.CurrentUICulture;
            CultureInfo culture = CultureInfo.CurrentCulture;

            return MyTaskFactory.StartNew(() =>
                {
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = cultureUi;

                    return func();
                })
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            CultureInfo cultureUi = CultureInfo.CurrentUICulture;
            CultureInfo culture = CultureInfo.CurrentCulture;

            MyTaskFactory.StartNew(() =>
                {
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = cultureUi;

                    return func();
                })
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
    }
}
