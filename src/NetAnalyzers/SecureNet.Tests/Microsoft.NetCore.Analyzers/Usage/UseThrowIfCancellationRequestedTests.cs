// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using SecureNet.Category.Usage;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    SecureNet.Category.Usage.UseThrowIfCancellationRequestedAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SecureNet.Tests
{
    public sealed class UseThrowIfCancellationRequestedTests
    {
        [Fact]
        public async Task EmptyText_NoDiagnostics()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"");
        }
        [Fact]
        public async Task NoThrow_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test(CancellationToken ct)
            {
                if(ct.IsCancellationRequested)
                    Console.ReadKey();
            }
        }
    }");
        }

        [Fact]
        public async Task NoThrowBlock_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test(CancellationToken ct)
            {
                if(ct.IsCancellationRequested)
                {
                    Console.ReadKey();
                }
            }
        }
    }");
        }
        [Fact]
        public async Task SingleDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test(CancellationToken ct)
            {
                {|#0:if(ct.IsCancellationRequested)
                    throw new Exception();|}
            }
        }
    }", VerifyCS.Diagnostic(UseThrowIfCancellationRequestedAnalyzer.DefaultRule).WithLocation(0));
        }

        [Fact]
        public async Task SingleFix()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test(CancellationToken ct)
            {
                {|#0:if(ct.IsCancellationRequested)
                    throw new Exception();|}
            }
        }
    }", VerifyCS.Diagnostic(UseThrowIfCancellationRequestedAnalyzer.DefaultRule),
            @"using System;
using System.Threading.Tasks;
using System.Threading;
namespace ConsoleApplication1
{
    class TypeName
    {
        public async Task Test(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
        }
    }
}");
        }

        [Fact]
        public async Task SingleFix_IfBlock()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test(CancellationToken ct)
            {
                {|#0:if(ct.IsCancellationRequested)
                {
                    throw new Exception();
                }|#0}
            }
        }
    }", VerifyCS.Diagnostic(UseThrowIfCancellationRequestedAnalyzer.DefaultRule).WithLocation(0, Microsoft.CodeAnalysis.Testing.DiagnosticLocationOptions.InterpretAsMarkupKey),
            @"using System;
using System.Threading.Tasks;
using System.Threading;
namespace ConsoleApplication1
{
    class TypeName
    {
        public async Task Test(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
        }
    }
}");
        }
    }
}