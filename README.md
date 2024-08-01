### README for Logger Implementation

### Overview
This logger is designed to handle logging information and exceptions in a C# application. It reads configuration settings from the application’s config file and writes logs to a specified file path. If no configuration is provided, it uses default settings.

### Features
- Configurable log file path.
- Toggleable information logging.
- Automatically creates necessary directories.
- Logs include method details (namespace, class, method name).

### Configuration
The logger reads from the application's configuration file (`App.config` or `Web.config`). It looks for the following settings:
- `LogPath`: The directory path where log files will be stored.
- `EnableInfoLogFlag`: A flag to enable or disable information logging (`"T"` for true, `"F"` for false).

If these settings are not found, the logger defaults to logging in `C:\temp\<username>`.

### Installation and Setup
1. **Add Configuration Settings**: Ensure your application's config file includes the necessary settings.
    ```xml
    <configuration>
        <appSettings>
            <add key="LogPath" value="C:\your\log\path" />
            <add key="EnableInfoLogFlag" value="T" />
        </appSettings>
    </configuration>
    ```

2. **Integrate Logger in Your Application**: Include the provided logging code in your application.

### Usage
#### Initializing the Logger
The logger is automatically initialized when you first call `WriteInfoToLog` or `WriteExceptionToLog`. Ensure these methods are called early in your application.

#### Writing Logs
- **Information Log**: Use `WriteInfoToLog(string message)` to log informational messages.
    ```csharp
    Logger.WriteInfoToLog("This is an informational message.");
    ```
    - Information logs are only written if the `EnableInfoLogFlag` is set to `"T"` in the configuration. If this flag is `"F"`, information logs are ignored.
  
- **Exception Log**: Use `WriteExceptionToLog(string message)` to log exceptions.
    ```csharp
    Logger.WriteExceptionToLog("This is an exception message.");
    ```
    - Exception logs are always written, regardless of the `EnableInfoLogFlag` setting.

### Notes
- **Thread Safety**: The logger uses `ThreadLocal` to ensure thread safety for logging operations.
- **Directory Creation**: The logger will automatically create the log directory if it does not exist.
- **Exception Handling**: The logger includes basic exception handling to ensure logging does not disrupt the main application flow.

By following these instructions, you can effectively use and customize the logging functionality in your C# application.