using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = LoggerAnalyzer.Test.CSharpCodeFixVerifier<
    LoggerAnalyzer.LoggerGenericTypeAnalyzer,
    LoggerAnalyzer.LoggerGenericTypeAnalyzerCodeFixProvider>;
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzer.Test;

[TestClass]
public class LoggerAnalyzerUnitTest
{
    [TestMethod]
    public async Task EmptyCode_NoDiagnostic()
    {
        var code = @"";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [TestMethod]
    public async Task EmptyLoggerField_NoDiagnostic()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class Worker
    {
        private readonly ILogger _logger;

        public Worker(ILogger logger)
        {
            _logger = logger;
        }
    }
}";
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location }
            },
        }.RunAsync();
    }

    [TestMethod]
    public async Task EmptyLoggerProperty_NoDiagnostic()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class Worker
    {
        private ILogger _logger { get; set; }

        public Worker(ILogger logger)
        {
            _logger = logger;
        }
    }
}";
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location }
            },
        }.RunAsync();
    }

    [TestMethod]
    public async Task CorrectLoggerField_NoDiagnostic()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class Worker
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
    }
}";

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location }
            }
        }.RunAsync();
    }

    [TestMethod]
    public async Task CorrectLoggerProperty_NoDiagnostic()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class Worker
    {
        private ILogger<Worker> _logger { get; set; }

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
    }
}";

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location }
            }
        }.RunAsync();
    }

    [TestMethod]
    public async Task EmptyLogger_NoDiagnostic()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class Worker
    {
        public Worker(ILogger logger)
        {
        }
    }
}";
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location }
            },
        }.RunAsync();
    }

    [TestMethod]
    public async Task CorrectLogger_NoDiagnostic()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class Worker
    {
        public Worker(ILogger<Worker> logger)
        {
        }
    }
}";

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location }
            }
        }.RunAsync();
    }

    [TestMethod]
    public async Task NotCorrectLogger_Diagnostic()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker {}

    public class Worker
    {
        public Worker(ILogger<AnotherWorker> logger)
        {
        }
    }
}";

        var fixedCode = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker {}

    public class Worker
    {
        public Worker(ILogger<Worker> logger)
        {
        }
    }
}";
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location },
                ExpectedDiagnostics = { VerifyCS.Diagnostic().WithLocation(10, 31).WithSeverity(DiagnosticSeverity.Error).WithArguments("Worker") }
            },
            FixedCode = fixedCode
        }.RunAsync();
    }


    [TestMethod]
    public async Task NotCorrectLoggerField_NoAssignment_Diagnostic_CheckFix()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker
    {
        private readonly ILogger<AnotherWorker> _logger;

        public Worker(ILogger<AnotherWorker> logger)
        {
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        var fixedCode = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker
    {
        private readonly ILogger<AnotherWorker> _logger;

        public Worker(ILogger<Worker> logger)
        {
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location },
                ExpectedDiagnostics = { VerifyCS.Diagnostic().WithLocation(17, 31).WithSeverity(DiagnosticSeverity.Error).WithArguments("Worker") },
            },
            FixedCode = fixedCode
        }.RunAsync();
    }


    [TestMethod]
    public async Task NotCorrectLoggerProperty_NoAssignment_Diagnostic_CheckFix()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker
    {
        private ILogger<AnotherWorker> _logger { get; set; }

        public Worker(ILogger<AnotherWorker> logger)
        {
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        var fixedCode = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker
    {
        private ILogger<AnotherWorker> _logger { get; set; }

        public Worker(ILogger<Worker> logger)
        {
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location },
                ExpectedDiagnostics = { VerifyCS.Diagnostic().WithLocation(17, 31).WithSeverity(DiagnosticSeverity.Error).WithArguments("Worker") },
            },
            FixedCode = fixedCode
        }.RunAsync();
    }

    [TestMethod]
    public async Task NotCorrectLoggerField_Diagnostic_CheckFix()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker
    {
        private readonly ILogger<AnotherWorker> _logger;

        public Worker(ILogger<AnotherWorker> logger)
        {
            _logger = logger;
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        var fixedCode = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location },
                ExpectedDiagnostics = { VerifyCS.Diagnostic().WithLocation(17, 31).WithSeverity(DiagnosticSeverity.Error).WithArguments("Worker") },
            },
            FixedCode = fixedCode
        }.RunAsync();
    }

    [TestMethod]
    public async Task NotCorrectLoggerProperty_Diagnostic_CheckFix()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker
    {
        private ILogger<AnotherWorker> _logger { get; set; }

        public Worker(ILogger<AnotherWorker> logger)
        {
            _logger = logger;
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        var fixedCode = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker
    {
        private ILogger<Worker> _logger { get; set; }

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location },
                ExpectedDiagnostics = { VerifyCS.Diagnostic().WithLocation(17, 31).WithSeverity(DiagnosticSeverity.Error).WithArguments("Worker") },
            },
            FixedCode = fixedCode
        }.RunAsync();
    }

    [TestMethod]
    public async Task NotCorrectLoggerProperty_Diagnostic_CheckFix2()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class WorkerBase 
    {
         protected ILogger _logger { get; set; }
    }

    public class Worker: WorkerBase
    {
        public Worker(ILogger<AnotherWorker> logger)
        {
            _logger = logger;
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        var fixedCode = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker
    {
        public AnotherWorker()
        {
        }
    }

    public class WorkerBase 
    {
         protected ILogger _logger { get; set; }
    }

    public class Worker: WorkerBase
    {
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
    }

    public class AnotherWorker2
    {
        public AnotherWorker2()
        {
        }
    }
}";

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location },
                ExpectedDiagnostics = { VerifyCS.Diagnostic().WithLocation(20, 31).WithSeverity(DiagnosticSeverity.Error).WithArguments("Worker") },
            },
            FixedCode = fixedCode
        }.RunAsync();
    }

    [TestMethod]
    public async Task Generic_NotCorrectLoggerField_Diagnostic_CheckFix()
    {
        var code = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker<T>
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker<T>
    {
        private readonly ILogger<AnotherWorker<T>> _logger;

        public Worker(ILogger<AnotherWorker<T>> logger)
        {
            _logger = logger;
        }
    }
}";

        var fixedCode = @"
using Microsoft.Extensions.Logging;

namespace LoggerAnalyzerTest
{
    public class AnotherWorker<T>
    {
        public AnotherWorker()
        {
        }
    }

    public class Worker<T>
    {
        private readonly ILogger<Worker<T>> _logger;

        public Worker(ILogger<Worker<T>> logger)
        {
            _logger = logger;
        }
    }
}";

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                AdditionalReferences = { typeof(ILogger<LoggerAnalyzerUnitTest>).Assembly.Location },
                ExpectedDiagnostics = { VerifyCS.Diagnostic().WithLocation(17, 31).WithSeverity(DiagnosticSeverity.Error).WithArguments("Worker") },
            },
            FixedCode = fixedCode
        }.RunAsync();
    }
}
