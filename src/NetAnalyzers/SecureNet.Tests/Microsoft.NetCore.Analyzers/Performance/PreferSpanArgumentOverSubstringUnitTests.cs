// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using SecureNet.Category.Performance;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    SecureNet.Category.Performance.PreferSpanArgumentOverSubstringAnalyzer,
    SecureNet.Category.Performance.PreferSpanArgumentOverSubstringFixer>;

namespace SecureNet.Tests
{
    public class PreferSpanArgumentOverSubstringTests
    {

        [Fact]
        public async Task NoDiagnostic_EmptyText()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"");
        }

        [Fact]
        public async Task NoDiagnostic_StringPassed()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public int Test() => int.Parse("""");
        }
    }");
        }

        [Fact]
        public async Task NoDiagnostic_NoSpanOrStringOverload()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public void Accepts(object s) {}
            public void Test() => Accepts("""".Substring(1));
        }
    }");
        }

        [Fact]
        public async Task NoDiagnostic_NoParentInvocation()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName(string s) {}
            public void Test() => new TypeName("""".Substring(1));
        }
    }");
        }

        [Fact]
        public async Task Noiagnostic_StringPassedInsteadOfParameter()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    using System.Threading.Tasks;
    
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task<int> Test() => int.Parse("""".Substring(1));
        }
    }");
        }

        [Fact]
        public async Task SingleDiagnostic_StringPassedInsteadOfParameter()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public int Test() => int.Parse({|#0:"""".Substring(1)|});
        }
    }", VerifyCS.Diagnostic(PreferSpanArgumentOverSubstringAnalyzer.Rule).WithLocation(0).WithArguments(@""""".Substring(1)"));
        }

        [Fact]
        public async Task SingleFix_StringPassedInsteadOfParameter()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public int Test() => int.Parse({|#0:"""".Substring(1)|});
        }
    }", VerifyCS.Diagnostic(PreferSpanArgumentOverSubstringAnalyzer.Rule).WithLocation(0).WithArguments(@""""".Substring(1)"),
    @"using System;
namespace ConsoleApplication1
{
    class TypeName
    {
        public int Test() => int.Parse("""".AsSpan().Slice(1));
    }
}");
        }
    }
}