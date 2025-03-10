﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Opera;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Safari;

namespace Atata
{
    /// <summary>
    /// Represents the builder of <see cref="AtataContext"/>.
    /// </summary>
    public class AtataContextBuilder
    {
        public AtataContextBuilder(AtataBuildingContext buildingContext)
        {
            BuildingContext = buildingContext.CheckNotNull(nameof(buildingContext));
        }

        /// <summary>
        /// Gets the building context.
        /// </summary>
        public AtataBuildingContext BuildingContext { get; internal set; }

        /// <summary>
        /// Gets the builder of context attributes,
        /// which provides the functionality to add extra attributes to different metadata levels:
        /// global, assembly, component and property.
        /// </summary>
        public AttributesAtataContextBuilder Attributes => new AttributesAtataContextBuilder(BuildingContext);

        /// <summary>
        /// Returns an existing or creates a new builder for <typeparamref name="TDriverBuilder"/> by the specified alias.
        /// </summary>
        /// <typeparam name="TDriverBuilder">The type of the driver builder.</typeparam>
        /// <param name="alias">The driver alias.</param>
        /// <param name="driverBuilderCreator">The function that creates a driver builder.</param>
        /// <returns>The <typeparamref name="TDriverBuilder"/> instance.</returns>
        public TDriverBuilder ConfigureDriver<TDriverBuilder>(string alias, Func<TDriverBuilder> driverBuilderCreator)
            where TDriverBuilder : AtataContextBuilder, IDriverFactory
        {
            alias.CheckNotNullOrWhitespace(nameof(alias));
            driverBuilderCreator.CheckNotNull(nameof(driverBuilderCreator));

            var driverFactory = BuildingContext.GetDriverFactory(alias);

            if (driverFactory is null)
            {
                driverFactory = driverBuilderCreator.Invoke();
                BuildingContext.DriverFactories.Add(driverFactory);
            }
            else if (!(driverFactory is TDriverBuilder))
            {
                throw new ArgumentException(
                    $@"Existing driver with ""{alias}"" alias has other factory type in {nameof(AtataContextBuilder)}.
Expected: {typeof(TDriverBuilder).FullName}
Actual: {driverFactory.GetType().FullName}", nameof(alias));
            }

            return (TDriverBuilder)driverFactory;
        }

        /// <summary>
        /// Use the driver factory.
        /// </summary>
        /// <typeparam name="TDriverFactory">The type of the driver factory.</typeparam>
        /// <param name="driverFactory">The driver factory.</param>
        /// <returns>The <typeparamref name="TDriverFactory"/> instance.</returns>
        // TODO: Rename TDriverFactory to TDriverBuilder. Parameter too.
        public TDriverFactory UseDriver<TDriverFactory>(TDriverFactory driverFactory)
            where TDriverFactory : AtataContextBuilder, IDriverFactory
        {
            driverFactory.CheckNotNull(nameof(driverFactory));

            BuildingContext.DriverFactories.Add(driverFactory);
            BuildingContext.DriverFactoryToUse = driverFactory;

            return driverFactory;
        }

        /// <summary>
        /// Sets the alias of the driver to use.
        /// </summary>
        /// <param name="alias">The alias of the driver.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseDriver(string alias)
        {
            alias.CheckNotNullOrWhitespace(nameof(alias));

            IDriverFactory driverFactory = BuildingContext.GetDriverFactory(alias);

            if (driverFactory != null)
                BuildingContext.DriverFactoryToUse = driverFactory;
            else if (UsePredefinedDriver(alias) == null)
                throw new ArgumentException($"No driver with \"{alias}\" alias defined.", nameof(alias));

            return this;
        }

        /// <summary>
        /// Use specified driver instance.
        /// </summary>
        /// <param name="driver">The driver to use.</param>
        /// <returns>The <see cref="CustomDriverAtataContextBuilder"/> instance.</returns>
        public CustomDriverAtataContextBuilder UseDriver(RemoteWebDriver driver)
        {
            driver.CheckNotNull(nameof(driver));

            return UseDriver(() => driver);
        }

        /// <summary>
        /// Use custom driver factory method.
        /// </summary>
        /// <param name="driverFactory">The driver factory method.</param>
        /// <returns>The <see cref="CustomDriverAtataContextBuilder"/> instance.</returns>
        public CustomDriverAtataContextBuilder UseDriver(Func<RemoteWebDriver> driverFactory)
        {
            driverFactory.CheckNotNull(nameof(driverFactory));

            return UseDriver(new CustomDriverAtataContextBuilder(BuildingContext, driverFactory));
        }

        private IDriverFactory UsePredefinedDriver(string alias)
        {
            switch (alias.ToLowerInvariant())
            {
                case DriverAliases.Chrome:
                    return UseChrome();
                case DriverAliases.Firefox:
                    return UseFirefox();
                case DriverAliases.InternetExplorer:
                    return UseInternetExplorer();
                case DriverAliases.Safari:
                    return UseSafari();
                case DriverAliases.Opera:
                    return UseOpera();
                case DriverAliases.Edge:
                    return UseEdge();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates and returns a new builder for <see cref="ChromeDriver"/>
        /// with default <see cref="DriverAliases.Chrome"/> alias.
        /// Sets this builder as a one to use for a driver creation.
        /// </summary>
        /// <returns>The <see cref="ChromeAtataContextBuilder"/> instance.</returns>
        public ChromeAtataContextBuilder UseChrome() =>
            UseDriver(new ChromeAtataContextBuilder(BuildingContext));

        /// <summary>
        /// Creates and returns a new builder for <see cref="FirefoxDriver"/>
        /// with default <see cref="DriverAliases.Firefox"/> alias.
        /// Sets this builder as a one to use for a driver creation.
        /// </summary>
        /// <returns>The <see cref="FirefoxAtataContextBuilder"/> instance.</returns>
        public FirefoxAtataContextBuilder UseFirefox() =>
            UseDriver(new FirefoxAtataContextBuilder(BuildingContext));

        /// <summary>
        /// Creates and returns a new builder for <see cref="InternetExplorerDriver"/>
        /// with default <see cref="DriverAliases.InternetExplorer"/> alias.
        /// Sets this builder as a one to use for a driver creation.
        /// </summary>
        /// <returns>The <see cref="InternetExplorerAtataContextBuilder"/> instance.</returns>
        public InternetExplorerAtataContextBuilder UseInternetExplorer() =>
            UseDriver(new InternetExplorerAtataContextBuilder(BuildingContext));

        /// <summary>
        /// Creates and returns a new builder for <see cref="EdgeDriver"/>
        /// with default <see cref="DriverAliases.Edge"/> alias.
        /// Sets this builder as a one to use for a driver creation.
        /// </summary>
        /// <returns>The <see cref="EdgeAtataContextBuilder"/> instance.</returns>
        public EdgeAtataContextBuilder UseEdge() =>
            UseDriver(new EdgeAtataContextBuilder(BuildingContext));

        /// <summary>
        /// Creates and returns a new builder for <see cref="OperaDriver"/>
        /// with default <see cref="DriverAliases.Opera"/> alias.
        /// Sets this builder as a one to use for a driver creation.
        /// </summary>
        /// <returns>The <see cref="OperaAtataContextBuilder"/> instance.</returns>
        public OperaAtataContextBuilder UseOpera() =>
            UseDriver(new OperaAtataContextBuilder(BuildingContext));

        /// <summary>
        /// Creates and returns a new builder for <see cref="SafariDriver"/>
        /// with default <see cref="DriverAliases.Safari"/> alias.
        /// Sets this builder as a one to use for a driver creation.
        /// </summary>
        /// <returns>The <see cref="SafariAtataContextBuilder"/> instance.</returns>
        public SafariAtataContextBuilder UseSafari() =>
            UseDriver(new SafariAtataContextBuilder(BuildingContext));

        /// <summary>
        /// Creates and returns a new builder for <see cref="RemoteWebDriver"/>
        /// with default <see cref="DriverAliases.Remote"/> alias.
        /// Sets this builder as a one to use for a driver creation.
        /// </summary>
        /// <returns>The <see cref="RemoteDriverAtataContextBuilder"/> instance.</returns>
        public RemoteDriverAtataContextBuilder UseRemoteDriver() =>
            UseDriver(new RemoteDriverAtataContextBuilder(BuildingContext));

        /// <summary>
        /// Returns an existing or creates a new builder for <see cref="ChromeDriver"/> by the specified alias.
        /// </summary>
        /// <param name="alias">
        /// The driver alias.
        /// The default value is <see cref="DriverAliases.Chrome"/>.
        /// </param>
        /// <returns>The <see cref="ChromeAtataContextBuilder"/> instance.</returns>
        public ChromeAtataContextBuilder ConfigureChrome(string alias = DriverAliases.Chrome) =>
            ConfigureDriver(
                alias,
                () => new ChromeAtataContextBuilder(BuildingContext).WithAlias(alias));

        /// <summary>
        /// Returns an existing or creates a new builder for <see cref="FirefoxDriver"/> by the specified alias.
        /// </summary>
        /// <param name="alias">
        /// The driver alias.
        /// The default value is <see cref="DriverAliases.Firefox"/>.
        /// </param>
        /// <returns>The <see cref="FirefoxAtataContextBuilder"/> instance.</returns>
        public FirefoxAtataContextBuilder ConfigureFirefox(string alias = DriverAliases.Firefox) =>
            ConfigureDriver(
                alias,
                () => new FirefoxAtataContextBuilder(BuildingContext).WithAlias(alias));

        /// <summary>
        /// Returns an existing or creates a new builder for <see cref="InternetExplorerDriver"/> by the specified alias.
        /// </summary>
        /// <param name="alias">
        /// The driver alias.
        /// The default value is <see cref="DriverAliases.InternetExplorer"/>.
        /// </param>
        /// <returns>The <see cref="InternetExplorerAtataContextBuilder"/> instance.</returns>
        public InternetExplorerAtataContextBuilder ConfigureInternetExplorer(string alias = DriverAliases.InternetExplorer) =>
            ConfigureDriver(
                alias,
                () => new InternetExplorerAtataContextBuilder(BuildingContext).WithAlias(alias));

        /// <summary>
        /// Returns an existing or creates a new builder for <see cref="EdgeDriver"/> by the specified alias.
        /// </summary>
        /// <param name="alias">
        /// The driver alias.
        /// The default value is <see cref="DriverAliases.Edge"/>.
        /// </param>
        /// <returns>The <see cref="EdgeAtataContextBuilder"/> instance.</returns>
        public EdgeAtataContextBuilder ConfigureEdge(string alias = DriverAliases.Edge) =>
            ConfigureDriver(
                alias,
                () => new EdgeAtataContextBuilder(BuildingContext).WithAlias(alias));

        /// <summary>
        /// Returns an existing or creates a new builder for <see cref="OperaDriver"/> by the specified alias.
        /// </summary>
        /// <param name="alias">
        /// The driver alias.
        /// The default value is <see cref="DriverAliases.Opera"/>.
        /// </param>
        /// <returns>The <see cref="OperaAtataContextBuilder"/> instance.</returns>
        public OperaAtataContextBuilder ConfigureOpera(string alias = DriverAliases.Opera) =>
            ConfigureDriver(
                alias,
                () => new OperaAtataContextBuilder(BuildingContext).WithAlias(alias));

        /// <summary>
        /// Returns an existing or creates a new builder for <see cref="SafariDriver"/> by the specified alias.
        /// </summary>
        /// <param name="alias">
        /// The driver alias.
        /// The default value is <see cref="DriverAliases.Safari"/>.
        /// </param>
        /// <returns>The <see cref="SafariAtataContextBuilder"/> instance.</returns>
        public SafariAtataContextBuilder ConfigureSafari(string alias = DriverAliases.Safari) =>
            ConfigureDriver(
                alias,
                () => new SafariAtataContextBuilder(BuildingContext).WithAlias(alias));

        /// <summary>
        /// Returns an existing or creates a new builder for <see cref="RemoteWebDriver"/> by the specified alias.
        /// </summary>
        /// <param name="alias">
        /// The driver alias.
        /// The default value is <see cref="DriverAliases.Remote"/>.
        /// </param>
        /// <returns>The <see cref="RemoteDriverAtataContextBuilder"/> instance.</returns>
        public RemoteDriverAtataContextBuilder ConfigureRemoteDriver(string alias = DriverAliases.Remote) =>
            ConfigureDriver(
                alias,
                () => new RemoteDriverAtataContextBuilder(BuildingContext).WithAlias(alias));

        /// <summary>
        /// Adds the log consumer.
        /// </summary>
        /// <typeparam name="TLogConsumer">
        /// The type of the log consumer.
        /// Should have default constructor.
        /// </typeparam>
        /// <returns>The <see cref="AtataContextBuilder{TLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<TLogConsumer> AddLogConsumer<TLogConsumer>()
            where TLogConsumer : ILogConsumer, new() =>
            AddLogConsumer(new TLogConsumer());

        /// <summary>
        /// Adds the log consumer.
        /// </summary>
        /// <typeparam name="TLogConsumer">The type of the log consumer.</typeparam>
        /// <param name="consumer">The log consumer.</param>
        /// <returns>The <see cref="AtataContextBuilder{TLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<TLogConsumer> AddLogConsumer<TLogConsumer>(TLogConsumer consumer)
            where TLogConsumer : ILogConsumer
        {
            consumer.CheckNotNull(nameof(consumer));

            BuildingContext.LogConsumers.Add(new LogConsumerInfo(consumer));
            return new AtataContextBuilder<TLogConsumer>(consumer, BuildingContext);
        }

        /// <summary>
        /// Adds the log consumer by its type name or alias.
        /// Predefined aliases are defined in <see cref="LogConsumerAliases"/> static class.
        /// </summary>
        /// <param name="typeNameOrAlias">The type name or alias of the log consumer.</param>
        /// <returns>The <see cref="AtataContextBuilder{TLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<ILogConsumer> AddLogConsumer(string typeNameOrAlias)
        {
            ILogConsumer consumer = LogConsumerAliases.Resolve(typeNameOrAlias);
            return AddLogConsumer(consumer);
        }

        /// <summary>
        /// Adds the <see cref="TraceLogConsumer"/> instance that uses <see cref="Trace"/> class for logging.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder{TraceLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<TraceLogConsumer> AddTraceLogging()
        {
            return AddLogConsumer(new TraceLogConsumer());
        }

        /// <summary>
        /// Adds the <see cref="DebugLogConsumer"/> instance that uses <see cref="Debug"/> class for logging.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder{DebugLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<DebugLogConsumer> AddDebugLogging()
        {
            return AddLogConsumer(new DebugLogConsumer());
        }

        /// <summary>
        /// Adds the <see cref="ConsoleLogConsumer"/> instance that uses <see cref="Console"/> class for logging.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder{ConsoleLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<ConsoleLogConsumer> AddConsoleLogging()
        {
            return AddLogConsumer(new ConsoleLogConsumer());
        }

        /// <summary>
        /// Adds the <see cref="NUnitTestContextLogConsumer"/> instance that uses <c>NUnit.Framework.TestContext</c> class for logging.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder{NUnitTestContextLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<NUnitTestContextLogConsumer> AddNUnitTestContextLogging()
        {
            return AddLogConsumer(new NUnitTestContextLogConsumer());
        }

        /// <summary>
        /// Adds the <see cref="NLogConsumer"/> instance that uses <c>NLog.Logger</c> class for logging.
        /// </summary>
        /// <param name="loggerName">The name of the logger.</param>
        /// <returns>The <see cref="AtataContextBuilder{NLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<NLogConsumer> AddNLogLogging(string loggerName = null)
        {
            return AddLogConsumer(new NLogConsumer(loggerName));
        }

        /// <summary>
        /// Adds the <see cref="NLogFileConsumer"/> instance that uses <c>NLog.Logger</c> class for logging into file.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder{NLogFileConsumer}"/> instance.</returns>
        public AtataContextBuilder<NLogFileConsumer> AddNLogFileLogging()
        {
            return AddLogConsumer(new NLogFileConsumer());
        }

        /// <summary>
        /// Adds the <see cref="Log4NetConsumer"/> instance that uses <c>log4net.ILog</c> interface for logging.
        /// </summary>
        /// <param name="loggerName">The name of the logger.</param>
        /// <returns>The <see cref="AtataContextBuilder{Log4NetConsumer}"/> instance.</returns>
        public AtataContextBuilder<Log4NetConsumer> AddLog4NetLogging(string loggerName = null)
        {
            return AddLogConsumer(new Log4NetConsumer { LoggerName = loggerName });
        }

        /// <summary>
        /// Adds the <see cref="Log4NetConsumer"/> instance that uses <c>log4net.ILog</c> interface for logging.
        /// </summary>
        /// <param name="repositoryName">The name of the logger repository.</param>
        /// <param name="loggerName">The name of the logger.</param>
        /// <returns>The <see cref="AtataContextBuilder{Log4NetConsumer}"/> instance.</returns>
        public AtataContextBuilder<Log4NetConsumer> AddLog4NetLogging(string repositoryName, string loggerName = null)
        {
            return AddLogConsumer(new Log4NetConsumer { RepositoryName = repositoryName, LoggerName = loggerName });
        }

        /// <summary>
        /// Adds the <see cref="Log4NetConsumer"/> instance that uses <c>log4net.ILog</c> interface for logging.
        /// </summary>
        /// <param name="repositoryAssembly">The assembly to use to lookup the repository.</param>
        /// <param name="loggerName">The name of the logger.</param>
        /// <returns>The <see cref="AtataContextBuilder{Log4NetConsumer}"/> instance.</returns>
        public AtataContextBuilder<Log4NetConsumer> AddLog4NetLogging(Assembly repositoryAssembly, string loggerName = null)
        {
            return AddLogConsumer(new Log4NetConsumer { RepositoryAssembly = repositoryAssembly, LoggerName = loggerName });
        }

        /// <summary>
        /// Adds the secret string to mask in log.
        /// </summary>
        /// <param name="value">The secret string value.</param>
        /// <param name="mask">The mask, which should replace the secret string.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder AddSecretStringToMaskInLog(string value, string mask = "{*****}")
        {
            value.CheckNotNullOrWhitespace(nameof(value));
            mask.CheckNotNullOrWhitespace(nameof(mask));

            BuildingContext.SecretStringsToMaskInLog.Add(
                new SecretStringToMask(value, mask));

            return this;
        }

        /// <summary>
        /// Adds the screenshot consumer.
        /// </summary>
        /// <typeparam name="TScreenshotConsumer">The type of the screenshot consumer.</typeparam>
        /// <returns>The <see cref="AtataContextBuilder{TLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<TScreenshotConsumer> AddScreenshotConsumer<TScreenshotConsumer>()
            where TScreenshotConsumer : IScreenshotConsumer, new() =>
            AddScreenshotConsumer(new TScreenshotConsumer());

        /// <summary>
        /// Adds the screenshot consumer.
        /// </summary>
        /// <typeparam name="TScreenshotConsumer">The type of the screenshot consumer.</typeparam>
        /// <param name="consumer">The screenshot consumer.</param>
        /// <returns>The <see cref="AtataContextBuilder{TLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<TScreenshotConsumer> AddScreenshotConsumer<TScreenshotConsumer>(TScreenshotConsumer consumer)
            where TScreenshotConsumer : IScreenshotConsumer
        {
            consumer.CheckNotNull(nameof(consumer));

            BuildingContext.ScreenshotConsumers.Add(consumer);
            return new AtataContextBuilder<TScreenshotConsumer>(consumer, BuildingContext);
        }

        /// <summary>
        /// Adds the screenshot consumer.
        /// </summary>
        /// <param name="typeNameOrAlias">The type name or alias of the log consumer.</param>
        /// <returns>The <see cref="AtataContextBuilder{TLogConsumer}"/> instance.</returns>
        public AtataContextBuilder<IScreenshotConsumer> AddScreenshotConsumer(string typeNameOrAlias)
        {
            IScreenshotConsumer consumer = ScreenshotConsumerAliases.Resolve(typeNameOrAlias);

            return AddScreenshotConsumer(consumer);
        }

        /// <summary>
        /// Adds the <see cref="FileScreenshotConsumer"/> instance for the screenshot saving to file.
        /// By default uses <c>"Logs\{build-start}\{test-name-sanitized}"</c> as folder path format,
        /// <c>"{screenshot-number:D2} - {screenshot-pageobjectfullname}{screenshot-title: - *}"</c> as file name format
        /// and <see cref="OpenQA.Selenium.ScreenshotImageFormat.Png"/> as image format.
        /// Example of screenshot file path using default settings: <c>"Logs\2018-03-03 14_34_04\SampleTest\01 - Home page - Screenshot title.png"</c>.
        /// Available path variables are:
        /// <c>{build-start}</c>,
        /// <c>{test-name}</c>, <c>{test-name-sanitized}</c>,
        /// <c>{test-fixture-name}</c>, <c>{test-fixture-name-sanitized}</c>,
        /// <c>{test-start}</c>, <c>{driver-alias}</c>, <c>{screenshot-number}</c>,
        /// <c>{screenshot-title}</c>, <c>{screenshot-pageobjectname}</c>,
        /// <c>{screenshot-pageobjecttypename}</c>, <c>{screenshot-pageobjectfullname}</c>.
        /// Path variables support the formatting.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder{FileScreenshotConsumer}"/> instance.</returns>
        public AtataContextBuilder<FileScreenshotConsumer> AddScreenshotFileSaving()
        {
            return AddScreenshotConsumer(new FileScreenshotConsumer());
        }

        /// <summary>
        /// Sets the factory method of the test name.
        /// </summary>
        /// <param name="testNameFactory">The factory method of the test name.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseTestName(Func<string> testNameFactory)
        {
            testNameFactory.CheckNotNull(nameof(testNameFactory));

            BuildingContext.TestNameFactory = testNameFactory;
            return this;
        }

        /// <summary>
        /// Sets the name of the test.
        /// </summary>
        /// <param name="testName">The name of the test.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseTestName(string testName)
        {
            testName.CheckNotNull(nameof(testName));

            BuildingContext.TestNameFactory = () => testName;
            return this;
        }

        /// <summary>
        /// Sets the factory method of the test fixture name.
        /// </summary>
        /// <param name="testFixtureNameFactory">The factory method of the test fixture name.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseTestFixtureName(Func<string> testFixtureNameFactory)
        {
            testFixtureNameFactory.CheckNotNull(nameof(testFixtureNameFactory));

            BuildingContext.TestFixtureNameFactory = testFixtureNameFactory;
            return this;
        }

        /// <summary>
        /// Sets the name of the test fixture.
        /// </summary>
        /// <param name="testFixtureName">The name of the test fixture.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseTestFixtureName(string testFixtureName)
        {
            testFixtureName.CheckNotNull(nameof(testFixtureName));

            BuildingContext.TestFixtureNameFactory = () => testFixtureName;
            return this;
        }

        /// <summary>
        /// Sets the factory method of the test fixture type.
        /// </summary>
        /// <param name="testFixtureTypeFactory">The factory method of the test fixture type.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseTestFixtureType(Func<Type> testFixtureTypeFactory)
        {
            testFixtureTypeFactory.CheckNotNull(nameof(testFixtureTypeFactory));

            BuildingContext.TestFixtureTypeFactory = testFixtureTypeFactory;
            return this;
        }

        /// <summary>
        /// Sets the type of the test fixture.
        /// </summary>
        /// <param name="testFixtureType">The type of the test fixture.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseTestFixtureType(Type testFixtureType)
        {
            testFixtureType.CheckNotNull(nameof(testFixtureType));

            BuildingContext.TestFixtureTypeFactory = () => testFixtureType;
            return this;
        }

        /// <summary>
        /// Sets the base URL.
        /// </summary>
        /// <param name="baseUrl">The base URL.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseBaseUrl(string baseUrl)
        {
            if (baseUrl != null && !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
                throw new ArgumentException($"Invalid URL format \"{baseUrl}\".", nameof(baseUrl));

            BuildingContext.BaseUrl = baseUrl;
            return this;
        }

        /// <summary>
        /// Sets the base retry timeout.
        /// The default value is <c>5</c> seconds.
        /// </summary>
        /// <param name="timeout">The retry timeout.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        [Obsolete("Use UseBaseRetryTimeout instead.")] // Obsolete since v0.17.0.
        public AtataContextBuilder UseRetryTimeout(TimeSpan timeout)
        {
            return UseBaseRetryTimeout(timeout);
        }

        /// <summary>
        /// Sets the base retry interval.
        /// The default value is <c>500</c> milliseconds.
        /// </summary>
        /// <param name="interval">The retry interval.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        [Obsolete("Use UseBaseRetryInterval instead.")] // Obsolete since v0.17.0.
        public AtataContextBuilder UseRetryInterval(TimeSpan interval)
        {
            return UseBaseRetryInterval(interval);
        }

        /// <summary>
        /// Sets the base retry timeout.
        /// The default value is <c>5</c> seconds.
        /// </summary>
        /// <param name="timeout">The retry timeout.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseBaseRetryTimeout(TimeSpan timeout)
        {
            BuildingContext.BaseRetryTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the base retry interval.
        /// The default value is <c>500</c> milliseconds.
        /// </summary>
        /// <param name="interval">The retry interval.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseBaseRetryInterval(TimeSpan interval)
        {
            BuildingContext.BaseRetryInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the element find timeout.
        /// The default value is taken from <see cref="AtataBuildingContext.BaseRetryTimeout"/>, which is equal to <c>5</c> seconds by default.
        /// </summary>
        /// <param name="timeout">The retry timeout.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseElementFindTimeout(TimeSpan timeout)
        {
            BuildingContext.ElementFindTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the element find retry interval.
        /// The default value is taken from <see cref="AtataBuildingContext.BaseRetryInterval"/>, which is equal to <c>500</c> milliseconds by default.
        /// </summary>
        /// <param name="interval">The retry interval.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseElementFindRetryInterval(TimeSpan interval)
        {
            BuildingContext.ElementFindRetryInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the waiting timeout.
        /// The default value is taken from <see cref="AtataBuildingContext.BaseRetryTimeout"/>, which is equal to <c>5</c> seconds by default.
        /// </summary>
        /// <param name="timeout">The retry timeout.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseWaitingTimeout(TimeSpan timeout)
        {
            BuildingContext.WaitingTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the waiting retry interval.
        /// The default value is taken from <see cref="AtataBuildingContext.BaseRetryInterval"/>, which is equal to <c>500</c> milliseconds by default.
        /// </summary>
        /// <param name="interval">The retry interval.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseWaitingRetryInterval(TimeSpan interval)
        {
            BuildingContext.WaitingRetryInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the verification timeout.
        /// The default value is taken from <see cref="AtataBuildingContext.BaseRetryTimeout"/>, which is equal to <c>5</c> seconds by default.
        /// </summary>
        /// <param name="timeout">The retry timeout.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseVerificationTimeout(TimeSpan timeout)
        {
            BuildingContext.VerificationTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the verification retry interval.
        /// The default value is taken from <see cref="AtataBuildingContext.BaseRetryInterval"/>, which is equal to <c>500</c> milliseconds by default.
        /// </summary>
        /// <param name="interval">The retry interval.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseVerificationRetryInterval(TimeSpan interval)
        {
            BuildingContext.VerificationRetryInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the culture.
        /// The default value is <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        /// <param name="culture">The culture.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseCulture(CultureInfo culture)
        {
            BuildingContext.Culture = culture;
            return this;
        }

        /// <summary>
        /// Sets the culture by the name.
        /// The default value is <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        /// <param name="cultureName">The name of the culture.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseCulture(string cultureName)
        {
            return UseCulture(CultureInfo.GetCultureInfo(cultureName));
        }

        /// <summary>
        /// Sets the type of the assertion exception.
        /// The default value is a type of <see cref="AssertionException"/>.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseAssertionExceptionType<TException>()
            where TException : Exception
        {
            return UseAssertionExceptionType(typeof(TException));
        }

        /// <summary>
        /// Sets the type of the assertion exception.
        /// The default value is a type of <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="exceptionType">The type of the exception.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseAssertionExceptionType(Type exceptionType)
        {
            exceptionType.CheckIs<Exception>(nameof(exceptionType));

            BuildingContext.AssertionExceptionType = exceptionType;
            return this;
        }

        /// <summary>
        /// Sets the type of aggregate assertion exception.
        /// The default value is a type of <see cref="AggregateAssertionException"/>.
        /// The exception type should have public constructor with <c>IEnumerable&lt;AssertionResult&gt;</c> argument.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseAggregateAssertionExceptionType<TException>()
            where TException : Exception
        {
            return UseAggregateAssertionExceptionType(typeof(TException));
        }

        /// <summary>
        /// Sets the type of aggregate assertion exception.
        /// The default value is a type of <see cref="AggregateAssertionException"/>.
        /// The exception type should have public constructor with <c>IEnumerable&lt;AssertionResult&gt;</c> argument.
        /// </summary>
        /// <param name="exceptionType">The type of the exception.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseAggregateAssertionExceptionType(Type exceptionType)
        {
            exceptionType.CheckIs<Exception>(nameof(exceptionType));

            BuildingContext.AggregateAssertionExceptionType = exceptionType;
            return this;
        }

        /// <summary>
        /// Sets the default assembly name pattern that is used to filter assemblies to find types in them.
        /// Modifies the <see cref="AtataBuildingContext.DefaultAssemblyNamePatternToFindTypes"/> property value of <see cref="BuildingContext"/>.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseDefaultAssemblyNamePatternToFindTypes(string pattern)
        {
            pattern.CheckNotNullOrWhitespace(nameof(pattern));

            BuildingContext.DefaultAssemblyNamePatternToFindTypes = pattern;
            return this;
        }

        /// <summary>
        /// Sets the assembly name pattern that is used to filter assemblies to find component types in them.
        /// Modifies the <see cref="AtataBuildingContext.AssemblyNamePatternToFindComponentTypes"/> property value of <see cref="BuildingContext"/>.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseAssemblyNamePatternToFindComponentTypes(string pattern)
        {
            pattern.CheckNotNullOrWhitespace(nameof(pattern));

            BuildingContext.AssemblyNamePatternToFindComponentTypes = pattern;
            return this;
        }

        /// <summary>
        /// Sets the assembly name pattern that is used to filter assemblies to find attribute types in them.
        /// Modifies the <see cref="AtataBuildingContext.AssemblyNamePatternToFindAttributeTypes"/> property value of <see cref="BuildingContext"/>.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseAssemblyNamePatternToFindAttributeTypes(string pattern)
        {
            pattern.CheckNotNullOrWhitespace(nameof(pattern));

            BuildingContext.AssemblyNamePatternToFindAttributeTypes = pattern;
            return this;
        }

        /// <summary>
        /// Sets the path to the Artifacts directory.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseArtifactsPath(string directoryPath)
        {
            directoryPath.CheckNotNullOrWhitespace(nameof(directoryPath));

            return UseArtifactsPath(_ => directoryPath);
        }

        /// <summary>
        /// Sets the builder of the path to the Artifacts directory.
        /// </summary>
        /// <param name="directoryPathBuilder">The directory path builder.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseArtifactsPath(Func<AtataContext, string> directoryPathBuilder)
        {
            directoryPathBuilder.CheckNotNull(nameof(directoryPathBuilder));

            BuildingContext.ArtifactsPathBuilder = directoryPathBuilder;
            return this;
        }

        /// <summary>
        /// Sets the default Artifacts path with optionally including <c>"{build-start}"</c> folder in the path.
        /// </summary>
        /// <param name="include">Whether to include the <c>"{build-start}"</c> folder in the path.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseDefaultArtifactsPathIncludingBuildStart(bool include) =>
            UseArtifactsPath(include
                ? AtataBuildingContext.DefaultArtifactsPath
                : AtataBuildingContext.DefaultArtifactsPathWithoutBuildStartFolder);

        /// <summary>
        /// Adds the action to perform during <see cref="AtataContext"/> building.
        /// It will be executed at the beginning of the build after the log is set up.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder OnBuilding(Action action)
        {
            if (action != null)
                BuildingContext.OnBuildingActions.Add(action);
            return this;
        }

        /// <summary>
        /// Adds the action to perform after <see cref="AtataContext"/> building.
        /// It will be executed at the end of the build after the driver is created.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder OnBuilt(Action action)
        {
            if (action != null)
                BuildingContext.OnBuiltActions.Add(action);
            return this;
        }

        /// <summary>
        /// Adds the action to perform after the driver is created.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder OnDriverCreated(Action<RemoteWebDriver> action)
        {
            if (action != null)
                BuildingContext.OnDriverCreatedActions.Add(action);
            return this;
        }

        /// <summary>
        /// Adds the action to perform after the driver is created.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder OnDriverCreated(Action action)
        {
            return action != null ? OnDriverCreated(_ => action()) : this;
        }

        /// <summary>
        /// Adds the action to perform during <see cref="AtataContext"/> cleanup.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder OnCleanUp(Action action)
        {
            if (action != null)
                BuildingContext.CleanUpActions.Add(action);
            return this;
        }

        /// <summary>
        /// Defines that the name of the test should be taken from the NUnit test.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseNUnitTestName()
        {
            return UseTestName(NUnitAdapter.GetCurrentTestName);
        }

        /// <summary>
        /// Defines that the name of the test fixture should be taken from the NUnit test fixture.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseNUnitTestFixtureName()
        {
            return UseTestFixtureName(NUnitAdapter.GetCurrentTestFixtureName);
        }

        /// <summary>
        /// Defines that the type of the test fixture should be taken from the NUnit test fixture.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseNUnitTestFixtureType()
        {
            return UseTestFixtureType(NUnitAdapter.GetCurrentTestFixtureType);
        }

        /// <summary>
        /// Sets <see cref="NUnitAggregateAssertionStrategy"/> as the aggregate assertion strategy.
        /// The <see cref="NUnitAggregateAssertionStrategy"/> uses NUnit's <c>Assert.Multiple</c> method for aggregate assertion.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseNUnitAggregateAssertionStrategy()
        {
            return UseAggregateAssertionStrategy(new NUnitAggregateAssertionStrategy());
        }

        /// <summary>
        /// Sets the aggregate assertion strategy.
        /// </summary>
        /// <typeparam name="TAggregateAssertionStrategy">The type of the aggregate assertion strategy.</typeparam>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseAggregateAssertionStrategy<TAggregateAssertionStrategy>()
            where TAggregateAssertionStrategy : IAggregateAssertionStrategy, new()
        {
            IAggregateAssertionStrategy strategy = Activator.CreateInstance<TAggregateAssertionStrategy>();

            return UseAggregateAssertionStrategy(strategy);
        }

        /// <summary>
        /// Sets the aggregate assertion strategy.
        /// </summary>
        /// <param name="strategy">The aggregate assertion strategy.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseAggregateAssertionStrategy(IAggregateAssertionStrategy strategy)
        {
            BuildingContext.AggregateAssertionStrategy = strategy;

            return this;
        }

        /// <summary>
        /// Sets <see cref="NUnitWarningReportStrategy"/> as the strategy for warning assertion reporting.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseNUnitWarningReportStrategy()
        {
            return UseWarningReportStrategy(new NUnitWarningReportStrategy());
        }

        /// <summary>
        /// Sets the strategy for warning assertion reporting.
        /// </summary>
        /// <param name="strategy">The warning report strategy.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseWarningReportStrategy(IWarningReportStrategy strategy)
        {
            BuildingContext.WarningReportStrategy = strategy;

            return this;
        }

        /// <summary>
        /// Defines that an error occurred during the NUnit test execution should be added to the log during the cleanup.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder LogNUnitError()
        {
            return OnCleanUp(() =>
            {
                dynamic testResult = GetNUnitTestResult();

                if (IsNUnitTestResultFailed(testResult))
                    AtataContext.Current.Log.Error((string)testResult.Message, (string)testResult.StackTrace);
            });
        }

        /// <summary>
        /// Defines that an error occurred during the NUnit test execution should be captured by a screenshot during the cleanup.
        /// </summary>
        /// <param name="title">The screenshot title.</param>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder TakeScreenshotOnNUnitError(string title = "Failed")
        {
            return OnCleanUp(() =>
            {
                dynamic testResult = GetNUnitTestResult();

                if (IsNUnitTestResultFailed(testResult))
                    AtataContext.Current.Log.Screenshot(title);
            });
        }

        /// <summary>
        /// Sets the type of <c>NUnit.Framework.AssertionException</c> as the assertion exception type.
        /// The default value is a type of <see cref="AssertionException"/>.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseNUnitAssertionExceptionType()
        {
            return UseAssertionExceptionType(NUnitAdapter.AssertionExceptionType);
        }

        /// <summary>
        /// Enables all NUnit features for Atata.
        /// Executes the following methods:
        /// <list type="bullet">
        /// <item><see cref="UseNUnitTestName"/>,</item>
        /// <item><see cref="UseNUnitTestFixtureName"/>,</item>
        /// <item><see cref="UseNUnitTestFixtureType"/>,</item>
        /// <item><see cref="UseNUnitAssertionExceptionType"/>,</item>
        /// <item><see cref="UseNUnitAggregateAssertionStrategy"/>,</item>
        /// <item><see cref="UseNUnitWarningReportStrategy"/>,</item>
        /// <item><see cref="AddNUnitTestContextLogging"/>,</item>
        /// <item><see cref="LogNUnitError"/>,</item>
        /// <item><see cref="TakeScreenshotOnNUnitError(string)"/>.</item>
        /// </list>
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder UseAllNUnitFeatures()
        {
            return UseNUnitTestName()
                .UseNUnitTestFixtureName()
                .UseNUnitTestFixtureType()
                .UseNUnitAssertionExceptionType()
                .UseNUnitAggregateAssertionStrategy()
                .UseNUnitWarningReportStrategy()
                .AddNUnitTestContextLogging()
                .LogNUnitError()
                .TakeScreenshotOnNUnitError();
        }

        private static dynamic GetNUnitTestContext()
        {
            Type testContextType = Type.GetType("NUnit.Framework.TestContext,nunit.framework", true);
            PropertyInfo currentContextProperty = testContextType.GetPropertyWithThrowOnError("CurrentContext");

            return currentContextProperty.GetStaticValue();
        }

        private static dynamic GetNUnitTestResult()
        {
            dynamic testContext = GetNUnitTestContext();
            return testContext.Result;
        }

        private static bool IsNUnitTestResultFailed(dynamic testResult)
        {
            return testResult.Outcome.Status.ToString().Contains("Fail");
        }

        private DirectorySubject CreateArtifactsDirectorySubject(AtataContext context)
        {
            string pathTemplate = BuildingContext.ArtifactsPathBuilder.Invoke(context);

            string path = context.FillTemplateString(pathTemplate);

            return new DirectorySubject(path, "artifacts");
        }

        /// <summary>
        /// Clears the <see cref="BuildingContext"/>.
        /// </summary>
        /// <returns>The <see cref="AtataContextBuilder"/> instance.</returns>
        public AtataContextBuilder Clear()
        {
            BuildingContext = new AtataBuildingContext();
            return this;
        }

        /// <summary>
        /// Builds the <see cref="AtataContext" /> instance and sets it to <see cref="AtataContext.Current" /> property.
        /// </summary>
        /// <returns>The created <see cref="AtataContext"/> instance.</returns>
        public AtataContext Build()
        {
            AtataContext.InitGlobalVariables();

            ValidateBuildingContextBeforeBuild();

            LogManager logManager = CreateLogManager();

            IObjectConverter objectConverter = new ObjectConverter
            {
                AssemblyNamePatternToFindTypes = BuildingContext.DefaultAssemblyNamePatternToFindTypes
            };

            IObjectMapper objectMapper = new ObjectMapper(objectConverter);
            IObjectCreator objectCreator = new ObjectCreator(objectConverter, objectMapper);

            AtataContext context = new AtataContext
            {
                TestName = BuildingContext.TestNameFactory?.Invoke(),
                TestFixtureName = BuildingContext.TestFixtureNameFactory?.Invoke(),
                TestFixtureType = BuildingContext.TestFixtureTypeFactory?.Invoke(),
                BaseUrl = BuildingContext.BaseUrl,
                Log = logManager,
                OnDriverCreatedActions = BuildingContext.OnDriverCreatedActions?.ToList() ?? new List<Action<RemoteWebDriver>>(),
                CleanUpActions = BuildingContext.CleanUpActions?.ToList() ?? new List<Action>(),
                Attributes = BuildingContext.Attributes.Clone(),
                BaseRetryTimeout = BuildingContext.BaseRetryTimeout,
                BaseRetryInterval = BuildingContext.BaseRetryInterval,
                ElementFindTimeout = BuildingContext.ElementFindTimeout,
                ElementFindRetryInterval = BuildingContext.ElementFindRetryInterval,
                WaitingTimeout = BuildingContext.WaitingTimeout,
                WaitingRetryInterval = BuildingContext.WaitingRetryInterval,
                VerificationTimeout = BuildingContext.VerificationTimeout,
                VerificationRetryInterval = BuildingContext.VerificationRetryInterval,
                Culture = BuildingContext.Culture ?? CultureInfo.CurrentCulture,
                AssertionExceptionType = BuildingContext.AssertionExceptionType,
                AggregateAssertionExceptionType = BuildingContext.AggregateAssertionExceptionType,
                AggregateAssertionStrategy = BuildingContext.AggregateAssertionStrategy ?? new AtataAggregateAssertionStrategy(),
                WarningReportStrategy = BuildingContext.WarningReportStrategy ?? new AtataWarningReportStrategy(),
                ObjectConverter = objectConverter,
                ObjectMapper = objectMapper,
                ObjectCreator = objectCreator
            };

            if (context.TestFixtureName is null && context.TestFixtureType != null)
                context.TestFixtureName = context.TestFixtureType.Name;

            context.DriverFactory = BuildingContext.DriverFactoryToUse
                ?? BuildingContext.DriverFactories.Last();
            context.DriverAlias = context.DriverFactory.Alias;

            context.Artifacts = CreateArtifactsDirectorySubject(context);

            AtataContext.Current = context;

            context.LogTestStart();

            context.Log.ExecuteSection(
                new LogSection("Set up AtataContext", LogLevel.Trace),
                () => SetUp(context));

            context.PureExecutionStopwatch.Start();

            return context;
        }

        private LogManager CreateLogManager()
        {
            LogManager logManager = new LogManager();

            logManager.AddSecretStringsToMask(BuildingContext.SecretStringsToMaskInLog);

            foreach (var logConsumerItem in BuildingContext.LogConsumers)
                logManager.Use(logConsumerItem);

            foreach (var screenshotConsumer in BuildingContext.ScreenshotConsumers)
                logManager.Use(screenshotConsumer);

            return logManager;
        }

        private void SetUp(AtataContext context)
        {
            OnBuilding();

            if (context.BaseUrl != null)
                context.Log.Trace($"Set: BaseUrl={context.BaseUrl}");

            LogRetrySettings(context);

            if (BuildingContext.Culture != null)
                ApplyCulture(context, BuildingContext.Culture);

            context.Log.Trace($"Set: Artifacts={context.Artifacts.FullName.Value}");

            context.InitDriver();

            context.Log.Trace($"Set: Driver={context.Driver.GetType().Name}{context.DriverFactory.Alias?.ToFormattedString(" (alias={0})")}");

            OnBuilt();
        }

        private void OnBuilding()
        {
            if (BuildingContext.OnBuildingActions != null)
                foreach (Action action in BuildingContext.OnBuildingActions)
                    action();
        }

        private void OnBuilt()
        {
            if (BuildingContext.OnBuiltActions != null)
                foreach (Action action in BuildingContext.OnBuiltActions)
                    action();
        }

        private static void LogRetrySettings(AtataContext context)
        {
            string messageFormat = "Set: {0}Timeout={1}; {0}RetryInterval={2}";

            context.Log.Trace(messageFormat, "ElementFind", context.ElementFindTimeout.ToShortIntervalString(), context.ElementFindRetryInterval.ToShortIntervalString());
            context.Log.Trace(messageFormat, "Waiting", context.WaitingTimeout.ToShortIntervalString(), context.WaitingRetryInterval.ToShortIntervalString());
            context.Log.Trace(messageFormat, "Verification", context.VerificationTimeout.ToShortIntervalString(), context.VerificationRetryInterval.ToShortIntervalString());
        }

        private static void ApplyCulture(AtataContext context, CultureInfo culture)
        {
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = culture;

#if !NET40
            if (AtataContext.ModeOfCurrent == AtataContextModeOfCurrent.Static)
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = culture;
#endif

            context.Log.Trace($"Set: Culture={culture.Name}");
        }

        private void ValidateBuildingContextBeforeBuild()
        {
            if (BuildingContext.DriverFactoryToUse == null && BuildingContext.DriverFactories.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot build {nameof(AtataContext)} as no driver is specified. " +
                    $"Use one of \"Use*\" methods to specify the driver to use, e.g.:AtataContext.Configure().UseChrome().Build();");
            }
        }

        protected IObjectMapper CreateObjectMapper()
        {
            IObjectConverter objectConverter = new ObjectConverter
            {
                AssemblyNamePatternToFindTypes = BuildingContext.DefaultAssemblyNamePatternToFindTypes
            };

            return new ObjectMapper(objectConverter);
        }

        /// <summary>
        /// <para>
        /// Sets up driver with auto version detection for the local browser to use.
        /// Gets the name of the local browser to use from <see cref="AtataBuildingContext.LocalBrowserToUseName"/> property.
        /// Then invokes <c>Atata.WebDriverSetup.DriverSetup.AutoSetUpSafely(...)</c> static method
        /// from <c>Atata.WebDriverSetup</c> package.
        /// </para>
        /// <para>
        /// In order to use this method,
        /// ensure that <c>Atata.WebDriverSetup</c> package is installed.
        /// </para>
        /// </summary>
        public void AutoSetUpDriverToUse()
        {
            if (BuildingContext.UsesLocalBrowser)
                InvokeAutoSetUpSafelyMethodOfDriverSetup(new[] { BuildingContext.LocalBrowserToUseName });
        }
#if !NET40

        /// <inheritdoc cref="AutoSetUpDriverToUse"/>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task AutoSetUpDriverToUseAsync()
        {
            await Task.Run(AutoSetUpDriverToUse);
        }
#endif

        /// <summary>
        /// <para>
        /// Sets up drivers with auto version detection for the local configured browsers.
        /// Gets the names of configured local browsers from <see cref="AtataBuildingContext.ConfiguredLocalBrowserNames"/> property.
        /// Then invokes <c>Atata.WebDriverSetup.DriverSetup.AutoSetUpSafely(...)</c> static method
        /// from <c>Atata.WebDriverSetup</c> package.
        /// </para>
        /// <para>
        /// In order to use this method,
        /// ensure that <c>Atata.WebDriverSetup</c> package is installed.
        /// </para>
        /// </summary>
        public void AutoSetUpConfiguredDrivers()
        {
            InvokeAutoSetUpSafelyMethodOfDriverSetup(BuildingContext.ConfiguredLocalBrowserNames);
        }
#if !NET40

        /// <inheritdoc cref="AutoSetUpConfiguredDrivers"/>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task AutoSetUpConfiguredDriversAsync()
        {
            await Task.Run(AutoSetUpConfiguredDrivers);
        }
#endif

        private static void InvokeAutoSetUpSafelyMethodOfDriverSetup(IEnumerable<string> browserNames)
        {
            Type driverSetupType = Type.GetType("Atata.WebDriverSetup.DriverSetup,Atata.WebDriverSetup", true);

            var setUpMethod = driverSetupType.GetMethodWithThrowOnError(
                "AutoSetUpSafely",
                BindingFlags.Public | BindingFlags.Static);

            setUpMethod.InvokeStaticAsLambda(browserNames);
        }
    }
}
