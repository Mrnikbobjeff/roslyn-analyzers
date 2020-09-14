// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using SecureNet.Category.Performance;
using System.Threading.Tasks;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    SecureNet.Category.Performance.ExtractConstArrayAnalyzerAnalyzer,
    SecureNet.Category.Performance.ExtractConstArrayFixer>;

namespace SecureNet.Tests
{
    public sealed class ExtractConstArrayAnalyzerUnitTests
    {
        [Fact]
        public async Task EmptyText_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"");
        }

        [Fact]
        public async Task TemporaryCreation_SingleDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            void Test() => string.Format("""", {|#0:new object[]{1}|});
        }
    }", VerifyCS.Diagnostic(ExtractConstArrayAnalyzerAnalyzer.DefaultRule).WithLocation(0).WithArguments("new object[]{1}"));
        }

        [Fact]
        public async Task TemporaryCreation_StaticArray_noDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            static readonly object[] hoisted = new object[]{1};
            void Test() => string.Format("""", hoisted);
        }
    }");
        }
        [Fact]
        public async Task TemporaryCreation_NonConstantParameters_noDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            void Test() => string.Format("""", new object[]{new object()});
        }
    }");
        }

        [Fact]
        public async Task TemporaryCreation_ConstantParameters_noDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            void Test() => string.Format("""", {|#0:new object[]{int.MaxValue}|});
        }
    }", VerifyCS.Diagnostic(ExtractConstArrayAnalyzerAnalyzer.DefaultRule).WithLocation(0).WithArguments("new object[]{int.MaxValue}"));
        }

        [Fact]
        public async Task TemporaryCreation_ConstantStringParameters_noDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            void Test() => string.Format("""", {|#0:new object[]{"""", null}|});
        }
    }", VerifyCS.Diagnostic(ExtractConstArrayAnalyzerAnalyzer.DefaultRule).WithLocation(0).WithArguments("new object[]{\"\", null}"));
        }

        [Fact]
        public async Task TemporaryCreation_SingleFix()
        {
            await VerifyCS.VerifyCodeFixAsync(@"using System;
    
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test() => string.Format("""", {|#0:new object[]{1}|});
        }
    }", VerifyCS.Diagnostic(ExtractConstArrayAnalyzerAnalyzer.DefaultRule).WithLocation(0).WithArguments("new object[]{1}"), @"using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        static readonly object? [] hoisted = new object[]{1};
        void Test() => string.Format("""", hoisted);
    }
}");
        }
    }
}
