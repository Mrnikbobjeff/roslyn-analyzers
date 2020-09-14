// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using SecureNet.Category.Tasks;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    SecureNet.Category.Tasks.TaskDelayUseCTOnWaitAnyAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SecureNet.Tests
{
    public class TaskDelayUseCTOnWaitAnyUnitTests
    {
        [Fact]
        public async Task EmptyText_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"");
        }

        [Fact]
        public async Task NoDiagnostic_WaitAnyWithCT()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test(CancellationToken ct) => await Task.WhenAny(Task.Delay(1, ct));
        }
    }");
        }

        [Fact]
        public async Task NoDiagnostic_NotInWhenAny()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test() => await Task.Delay(1);
        }
    }");
        }

        [Fact]
        public async Task SingleDiagnostic_WaitAnyWIthoutCtInDelay()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    using System.Threading.Tasks;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test() => await Task.WhenAny({|#0:Task.Delay(1)|});
        }
    }", VerifyCS.Diagnostic(TaskDelayUseCTOnWaitAnyAnalyzer.DefaultRule).WithLocation(0));
        }
    }
}