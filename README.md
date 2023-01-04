# MicrosoftLogger.Analyzer
[![NuGet Version and Downloads count](https://buildstats.info/nuget/MicrosoftLogger.Analyzer)](https://www.nuget.org/packages/MicrosoftLogger.Analyzer)

Analyzer helps you to find ILogger\<TCategoryName\> with wrong category name and fix it

## Installation

To install MicrosoftLogger.Analyzer, run the following command:

```
dotnet add package MicrosoftLogger.Analyzer
```

## Description

When you use Microsoft.Extensions.Logging.ILogger\<TCategoryName\>, you usually resolve it from dependency injection and set current class as the generic parameter.

The class is used as a category name in logs. You can filter logs by category, you can set a specific log level for category. Because of that, you must set the right class for the generic parameter of ILogger.

Often there are mistakes in ILogger\<TCategoryName\>. **MicrosoftLogger.Analyzer** analyzes class constructors and helps you to find and fix mistakes.
![Diagnostic example](https://raw.githubusercontent.com/PavelStefanov/MicrosoftLogger.Analyzer/master/img/example1.png)
