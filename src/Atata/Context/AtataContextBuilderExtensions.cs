﻿using System;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Opera;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Safari;

namespace Atata
{
    public static class AtataContextBuilderExtensions
    {
        /// <summary>
        /// Use the <see cref="ChromeDriver"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <see cref="ChromeAtataContextBuilder"/> instance.</returns>
        public static ChromeAtataContextBuilder UseChrome(this AtataContextBuilder builder)
        {
            return new ChromeAtataContextBuilder(builder.BuildingContext);
        }

        /// <summary>
        /// Use the <see cref="FirefoxDriver"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <see cref="FirefoxAtataContextBuilder"/> instance.</returns>
        public static FirefoxAtataContextBuilder UseFirefox(this AtataContextBuilder builder)
        {
            return new FirefoxAtataContextBuilder(builder.BuildingContext);
        }

        /// <summary>
        /// Use the <see cref="InternetExplorerDriver"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <see cref="InternetExplorerAtataContextBuilder"/> instance.</returns>
        public static InternetExplorerAtataContextBuilder UseInternetExplorer(this AtataContextBuilder builder)
        {
            return new InternetExplorerAtataContextBuilder(builder.BuildingContext);
        }

        /// <summary>
        /// Use the <see cref="EdgeDriver"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <see cref="EdgeAtataContextBuilder"/> instance.</returns>
        public static EdgeAtataContextBuilder UseEdge(this AtataContextBuilder builder)
        {
            return new EdgeAtataContextBuilder(builder.BuildingContext);
        }

        /// <summary>
        /// Use the <see cref="OperaDriver"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <see cref="OperaAtataContextBuilder"/> instance.</returns>
        public static OperaAtataContextBuilder UseOpera(this AtataContextBuilder builder)
        {
            return new OperaAtataContextBuilder(builder.BuildingContext);
        }

        /// <summary>
        /// Use the <see cref="PhantomJSDriver"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <see cref="PhantomJSAtataContextBuilder"/> instance.</returns>
        public static PhantomJSAtataContextBuilder UsePhantomJS(this AtataContextBuilder builder)
        {
            return new PhantomJSAtataContextBuilder(builder.BuildingContext);
        }

        /// <summary>
        /// Use the <see cref="SafariDriver"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <see cref="SafariAtataContextBuilder"/> instance.</returns>
        public static SafariAtataContextBuilder UseSafari(this AtataContextBuilder builder)
        {
            return new SafariAtataContextBuilder(builder.BuildingContext);
        }

        /// <summary>
        /// Adds arguments to be appended to the Chrome.exe command line.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The <see cref="ChromeAtataContextBuilder"/> instance.</returns>
        public static ChromeAtataContextBuilder WithArguments(this ChromeAtataContextBuilder builder, params string[] arguments)
        {
            return builder.WithOptions(options => options.AddArguments(arguments));
        }

        /// <summary>
        /// Adds arguments to be appended to the Opera.exe command line.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The <see cref="OperaAtataContextBuilder"/> instance.</returns>
        public static OperaAtataContextBuilder WithArguments(this OperaAtataContextBuilder builder, params string[] arguments)
        {
            return builder.WithOptions(options => options.AddArguments(arguments));
        }

        /// <summary>
        /// Defines that the name of the test should be taken from the NUnit test.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public static AtataContextBuilder UseNUnitTestName(this AtataContextBuilder builder)
        {
            dynamic testContext = GetNUnitTestContext();
            string testName = testContext.Test.Name;

            return builder.UseTestName(testName);
        }

        /// <summary>
        /// Defines that an error occured during the NUnit test execution should be added to the log upon the clean up.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public static AtataContextBuilder LogNUnitError(this AtataContextBuilder builder)
        {
            return builder.OnCleanUp(() =>
            {
                dynamic testContext = GetNUnitTestContext();
                var testResult = testContext.Result;

                if ((int)testResult.Outcome.Status == 3)
                    AtataContext.Current.Log.Error((string)testResult.Message, (string)testResult.StackTrace);
            });
        }

        private static object GetNUnitTestContext()
        {
            Type testContextType = Type.GetType("NUnit.Framework.TestContext,nunit.framework", true);
            PropertyInfo currentContextProperty = testContextType.GetPropertyWithThrowOnError("CurrentContext");

            return currentContextProperty.GetStaticValue();
        }

        public static AtataContextBuilder<ILogConsumer> UseTraceLogging(this AtataContextBuilder builder)
        {
            return builder.UseLogConsumer<ILogConsumer>(new TraceLogConsumer());
        }

        public static AtataContextBuilder<ILogConsumer> UseDebugLogging(this AtataContextBuilder builder)
        {
            return builder.UseLogConsumer<ILogConsumer>(new DebugLogConsumer());
        }

        public static AtataContextBuilder<ILogConsumer> UseNUnitTestContextLogging(this AtataContextBuilder builder)
        {
            return builder.UseLogConsumer<ILogConsumer>(new NUnitTestContextLogConsumer());
        }

        public static AtataContextBuilder<ILogConsumer> UseNLogLogging(this AtataContextBuilder builder, string loggerName = null)
        {
            return builder.UseLogConsumer<ILogConsumer>(new NLogConsumer(loggerName));
        }

        public static AtataContextBuilder<FileScreenshotConsumer> UseScreenshotFileSaving(this AtataContextBuilder builder, string folderPath)
        {
            return builder.UseScreenshotConsumer(new FileScreenshotConsumer(folderPath));
        }

        public static AtataContextBuilder<FileScreenshotConsumer> UseScreenshotFileSaving(this AtataContextBuilder builder, Func<string> folderPathCreator)
        {
            return builder.UseScreenshotConsumer(new FileScreenshotConsumer(folderPathCreator));
        }

        public static AtataContextBuilder<FileScreenshotConsumer> With(this AtataContextBuilder<FileScreenshotConsumer> builder, ImageFormat imageFormat)
        {
            builder.Context.ImageFormat = imageFormat;
            return builder;
        }

        public static AtataContextBuilder<T> WithoutSectionFinish<T>(this AtataContextBuilder<T> builder)
            where T : ILogConsumer
        {
            LogConsumerInfo consumerInfo = builder.BuildingContext.LogConsumers.Single(x => Equals(x.Consumer, builder.Context));
            consumerInfo.LogSectionFinish = false;
            return builder;
        }

        public static AtataContextBuilder<T> WithMinLevel<T>(this AtataContextBuilder<T> builder, LogLevel level)
            where T : ILogConsumer
        {
            LogConsumerInfo consumerInfo = builder.BuildingContext.LogConsumers.Single(x => Equals(x.Consumer, builder.Context));
            consumerInfo.MinLevel = level;
            return builder;
        }
    }
}
