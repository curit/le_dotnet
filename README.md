[![Build status](https://ci.appveyor.com/api/projects/status/a2ox11hp7wqr0knq)](https://ci.appveyor.com/project/albertjan/le-dotnet)

# Logging To Logentries.

Logentries currently has plugins for [NLog](#nlog) and [Log4net](#log4net).

To configure your application to log to Logentries, you will need to perform the following tasks:

1. Create a Logentries account.
2. Create a host and log to receive your log data.
3. Adding the Plugin library and the appropriate Logentries Appender libraries to your application.
4. Configure the Plugin and the Logentries appender in your application.
5. Send log messages from your application.

These steps are outlined in further detail below.

## Creating Your Logentries Account

You can register your account on Logentries by browning to [https://logentries.com](https://logentries.com) and simply clicking `Sign Up` at the top of the page.

## Creating the Host and Log

Once logged in to your Logentries account, create a new host with a name that best represents your application. Select this host and create a new log with a source type of `Token TCP` (see below for more information) and a name that represents what you will be logging.

Please note that Logentries reads no particular meaning into the names of your hosts and logs; they are primarily for your own benefit in keeping your logs organized.

## Adding the Logentries Plugin Libraries to Your Application

log4net
-------
The easiest way to add the Log4net and the Logentries Plugin library to your application is to install the `curit.logentries.log4net` [Nuget package](http://www.nuget.org/packages/curit.logentries.log4net "Nuget package"). This package will install the Logentries Plugin library and will also automatically install the `log4net` and Azure cloud configuration packages as a dependencies.

## Configuring Log4net and the Logentries Appender

General Log4net configuration is beyond the scope of this readme. Please refer to the [Configuration section of the Log4net manual](http://logging.apache.org/log4net/release/manual/configuration.html) for details on how to configure Log4net.

Log4net allows log messages to be sent to multiple destinations. In Log4net terminology, such an output destination is called an *appender*. Appenders must implement the `log4net.Appenders.IAppender` interface. The Logentries Appender library provides such an appender component that is specifically designed to send log messages to Logentries in an efficient manner.

The Logentries appender is configured and added to your Log4net configuration in the normal way using an `<appender>` element:

	<appender name="LeAppender" type="log4net.Appender.LogentriesAppender, LogentriesLog4net">
		...
	</appender>

The Logentries appender has two categories of settings that are configured somewhat differently:

- Logging settings
- Logentries credentials

### Logging Settings

Logging settings determine how the appender operates, and are specified as child elements of the `<appender>` element. The Logentries appender supports the following configuration settings:

- **Level**: The lowest Log4net logging level that should be included. All log messages with a logging level below this level will be filtered out and not sent to Logentries.
- **ImmediateFlush**: Set to `true` to always flush the TCP stream after every written entry.
- **Layout**: The layout used to format log messages before they are sent to Logentries. See the [Configuration section of the Log4net manual](http://logging.apache.org/log4net/release/manual/configuration.html) for more information on configuring layouts.

Here is an example of an appender configuration that works well for Logentries:

	<appender name="LeAppender" type="log4net.Appender.LogentriesAppender, LogentriesLog4net">
		<layout type="log4net.Layout.PatternLayout">
			<!-- The below pattern has been carefully formatted and optimized to work well with the Logentries.com entry parser. For reference see https://logentries.com/doc/search/. -->
			<param name="ConversionPattern" value="%d %logger %level% %m%n" />
		</layout>
	</appender>

### Logentries Credentials

Logentries credentials determine to which host and log your log messages are sent. The following settings constitute the Logentries credentials:

- **Token**: The unique token GUID of the log to send messages to. This applies when using the newer token-based logging.

Unlike the logging settings (which are typically configured once for a given application) the Logentries credentials typically vary based on the environment or instance of your application. For example, your application might run in both a testing and a production environment, and you will most likely wish to have separate logging destinations for those two environments.

Therefore, the Logentries credentials can be specified more flexibly than the configuration settings. You have three options:

- Specify the credentials as child elements of the `<appender>` element (if you don't need the added flexibility).
- Specify the credentials as settings in the `<appSettings>` element in your App.config och Web.config file.
- Specify the credentials as Windows Azure role configuration settings in your cloud service project (only applicable when running your application as a cloud service in Windows Azure).

The Logentries appender uses the [CloudConfigurationManager class](http://msdn.microsoft.com/en-us/library/microsoft.windowsazure.cloudconfigurationmanager.aspx) internally to read the credential values. This class looks for each credential value in the following order:

1. If the value exists as a Windows Azure role configuration setting, that value is used.
2. Otherwise if the value exists as a setting in the `<appSettings>` element in your App.config och Web.config file, that value is used.
3. Otherwise if the value exists as a configured child element of the `<appender>` element, that value is used.
4. If the value was not found in any of these locations, errors are logged to the Log4net internal debug log and logging to Logentries will fail.

Here is an example of how to specify the credentials in the `<appender>` element:

	<appender name="LeAppender" type="log4net.Appender.LogentriesAppender, LogentriesLog4net">
		<token value="bb61600f-f766-451e-b55f-9204f536a79f" />
		...
	</appender>

Here is an example of how to specify the credentials in the `<appSettings>` element in your App.config or Web.config file:

	<appSettings>
		<add key="Logentries.Token" value="bb61600f-f766-451e-b55f-9204f536a79f" />
	</appSettings>

Here is an example of how to specify the credentials as Windows Azure role configuration settings:

	<ServiceConfiguration serviceName="MyApp" osFamily="3" osVersion="\*" xmlns="http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration" schemaVersion="2013-03.2.0">
		<Role name="MyRole">
			<Instances count="2" />
			<ConfigurationSettings>
				<Setting name="Logentries.Token" value="bb61600f-f766-451e-b55f-9204f536a79f" />
			</ConfigurationSettings>
		</Role>
	</ServiceConfiguration>

### Logging Context Information in Web Applications

In web application it is often helpful to use Log4net's built-in ability to log additional contextual information with each log message. This works particularly well in combination with Logentries' log message indexer, which can identify any key-value-pairs in the incoming log message and index those for fast search and retrieval.

Here is an example of how additional web-specific contextual log information can be added to the layout of the Logentries appender in a format that the Logentries parser will recognize and index:

	<appender name="LeAppender" type="log4net.Appender.LogentriesAppender, LogentriesLog4net">
		...
		<layout type="log4net.Layout.PatternLayout">
			<!-- The below pattern has been carefully formatted and optimized to work well with the Logentries.com entry parser. For reference see https://logentries.com/doc/search/. -->
			<param name="ConversionPattern" value="%d %logger %level% %m%nSessionId='%aspnet-request{ASP.NET_SessionId}'; Username='%aspnet-request{AUTH_USER}'; ClientIpAddress='%aspnet-request{REMOTE_ADDR}'; ClientUserAgent='%aspnet-request{HTTP_USER_AGENT}'; ServerName='%aspnet-request{SERVER_NAME}'; RequestMethod='%aspnet-request{REQUEST_METHOD}'; RequestUrl='%aspnet-request{URL}'; RequestQueryString='%aspnet-request{QUERY_STRING}'; RequestCookies='%aspnet-request{HTTP_COOKIE}';%n" />
		</layout>
	</appender>

### Sending Log Data over SSL/TLS

The Logentries appender supports sending log data over SSL/TLS with both of the above logging methods by setting the `useSsl` logging setting to `true` in the appender definition. This is more secure but may have a performance impact.

## Sending Log Messages from Your Application

With installation and configuration out of the way, you are ready to send log data to Logentries.

In each class you wish to log from, add the following using directive at the top if it's not already there:

    using log4net;

Then create a logger object at the class level:

    private static readonly ILog Logger = LogManager.GetLogger(typeof({YOURCLASSNAMEHERE}).FullName);

Be sure to enter the name of current class instead of `{YOURCLASSNAMEHERE}` above. This creates a logger with the same name as the current class, which organizes the Log4net configuration hierarchy according to your code namespace hierarchy. This provides both clarity when reading the logs, and convenience when configuring different log levels for different areas of your code.

Now within your code in that class, you can log using Log4net as normal and it will log to Logentries.

Examples:

    Logger.Debug("Debugging message");
    Logger.Info("Informational message");
    Logger.Warn("Warning message");
    Logger.Error("Error message", ex);

Complete code example:

    using log4net;

    public class HomeController : Controller
    {
        private static readonly ILog Logger = log4net.LogManager.GetLogger(typeof(HomeController).FullName);

        public ActionResult Index()
        {
            Logger.Debug("Home page viewed.");
            ViewBag.Message = "Welcome to ASP.NET MVC!";
            Logger.Warn("This is a warning message!");
            return View();
        }
    }


NLog
-----
The easiest way to add the NLog and the Logentries Target libraries to your application is to install the `logentries.nlog` [Nuget package](http://www.nuget.org/packages/logentries.nlog "Nuget package"). This package will install the Logentries Target library and will also automatically install the `NLog` package as a dependency.

If you would rather install the Logentries appender manually, you can download the complete code in this GitHub repository, compile the LogentriesLog4net Visual Studio project within it into a DLL file, and then reference this file in your application. If you choose this option you must install Log4net yourself.

## Configuring NLog and the Logentries Appender

General NLog configuration is beyond the scope of this readme. Please refer to the [Configuration section of the NLog manual](https://github.com/nlog/NLog/wiki/Configuration-file) for details on how to configure NLog.

NLog allows log messages to be sent to multiple destinations. In NLog terminology, such an output destination is called a *target*. Targets must subclass the `NLog.Targets.Target` class. The Logentries Plugin library provides such a target component that is specifically designed to send log messages to Logentries in an efficient manner.

The Logentries target is configured and added to your NLog configuration in the normal way using a `<target>` element:

	<target name="logentries" type="Logentries" ... />

The Logentries target has two categories of settings that are configured somewhat differently:

- Logging settings
- Logentries credentials

### Logging Settings

Logging settings determine how the target operates, and are specified as child elements of the `<target>` element. The Logentries target supports the following configuration settings:

- **Level**: The lowest NLog logging level that should be included. All log messages with a logging level below this level will be filtered out and not sent to Logentries.
- **ImmediateFlush**: Set to `true` to always flush the TCP stream after every written entry.
- **UseSsl**: Set to `true` to use SSL to send data to Logentries (see below for more information).
- **Layout**: The layout used to format log messages before they are sent to Logentries. See the [Configuration section of the Log4net manual](http://logging.apache.org/log4net/release/manual/configuration.html) for more information on configuring layouts.

Here is an example of an appender configuration that works well for Logentries:

	<extensions>
    		<add assembly="LogentriesNLog"/>
  	</extensions>
  	<targets>
    		<target name="logentries" type="Logentries" debug="true" httpPut="false" ssl="false"
    		layout="${date:format=ddd MMM dd} ${time:format=HH:mm:ss} ${date:format=zzz yyyy} ${logger} : ${LEVEL}, ${message}"/>
  	</targets>


### Logentries Credentials

Logentries credentials determine to which host and log your log messages are sent. The following settings constitute the Logentries credentials:

- **Token**: The unique token GUID of the log to send messages to. This applies when using the newer token-based logging.

Unlike the logging settings (which are typically configured once for a given application) the Logentries credentials typically vary based on the environment or instance of your application. For example, your application might run in both a testing and a production environment, and you will most likely wish to have separate logging destinations for those two environments.

Therefore, the Logentries credentials can be specified more flexibly than the configuration settings. You have three options:

- Specify the credentials as child elements of the `<target>` element (if you don't need the added flexibility).
- Specify the credentials as settings in the `<appSettings>` element in your App.config och Web.config file.
- Specify the credentials as Windows Azure role configuration settings in your cloud service project (only applicable when running your application as a cloud service in Windows Azure).

The Logentries target uses the [CloudConfigurationManager class](http://msdn.microsoft.com/en-us/library/microsoft.windowsazure.cloudconfigurationmanager.aspx) internally to read the credential values. This class looks for each credential value in the following order:

1. If the value exists as a Windows Azure role configuration setting, that value is used.
2. Otherwise if the value exists as a setting in the `<appSettings>` element in your App.config or Web.config file, that value is used.
3. Otherwise if the value exists as a configured child element of the `<target>` element, that value is used.

Here is an example of how to specify the credentials in the `<target>` element:

	<target name="logentries" type="Logentries" token="bb61600f-f766-451e-b55f-9204f536a79f" ... />

Here is an example of how to specify the credentials in the `<appSettings>` element in your App.config or Web.config file:

	<appSettings>
		<add key="Logentries.Token" value="bb61600f-f766-451e-b55f-9204f536a79f" />
	</appSettings>

Here is an example of how to specify the credentials as Windows Azure role configuration settings:

	<ServiceConfiguration serviceName="MyApp" osFamily="3" osVersion="*" xmlns="http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration" schemaVersion="2013-03.2.0">
		<Role name="MyRole">
			<Instances count="2" />
			<ConfigurationSettings>
				<Setting name="Logentries.Token" value="bb61600f-f766-451e-b55f-9204f536a79f" />
			</ConfigurationSettings>
		</Role>
	</ServiceConfiguration>

### Logging Context Information in Web Applications

In web application it is often helpful to use NLog's built-in ability to log additional contextual information with each log message. This works particularly well in combination with Logentries' log message indexer, which can identify any key-value-pairs in the incoming log message and index those for fast search and retrieval.

Here is an example of how additional web-specific contextual log information can be added to the layout of the Logentries appender in a format that the Logentries parser will recognize and index:

	<target name="logentries" type="Logentries"
		layout="${date:format=ddd MMM dd} ${time:format=HH:mm:ss} ${date:format=zzz yyyy} ${logger} ${LEVEL} ${message}${newline}SessionId='${aspnet-sessionid}'; Username='${aspnet-user-identity}'; ${newline}" />


### Sending Log Data over SSL/TLS

The Logentries appender supports sending log data over SSL/TLS with both of the above logging methods by setting the `useSsl` logging setting to `true` in the appender definition. This is more secure but may have a performance impact.

## Sending Log Messages from Your Application

With installation and configuration out of the way, you are ready to send log data to Logentries.

In each class you wish to log from, add the following using directive at the top if it's not already there:

    using NLog;

Then create a logger object at the class level:

    private static readonly Logger Logger = LogManager.getCurrentClassLogger();

This creates a logger with the same name as the current class, which organizes the NLog configuration hierarchy according to your code namespace hierarchy. This provides both clarity when reading the logs, and convenience when configuring different log levels for different areas of your code.

Now within your code in that class, you can log using NLog as normal and it will log to Logentries.

Examples:

    Logger.Debug("Debugging message");
    Logger.Info("Informational message");
    Logger.Warn("Warning message");
    Logger.Error("Error message", ex);

Complete code example:

    using NLog

    public class HomeController : Controller
    {
        private static readonly Logger Logger = LogManager.getCurrentClassLogger();

        public ActionResult Index()
        {
            Logger.Debug("Home page viewed.");
            ViewBag.Message = "Welcome to ASP.NET MVC!";
            Logger.Warn("This is a warning message!");
            return View();
        }
    }
