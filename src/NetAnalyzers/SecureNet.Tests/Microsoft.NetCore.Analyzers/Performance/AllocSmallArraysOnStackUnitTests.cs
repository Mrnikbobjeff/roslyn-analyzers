// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using SecureNet.Category.Performance;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    SecureNet.Category.Performance.AllocSmallArraysOnStackAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SecureNet.Tests
{
    public sealed class AllocSmallArraysOnStackUnitTests
    {
        [Fact]
        public async Task EmptyText_NoDiagnostics()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"");
        }

        [Fact]
        public async Task NoDiagnostic_LargeArray()
        {
            var types = new string[] { "byte", "short", "int", "long", "object" };
            foreach (var type in types)
            {
                await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public unsafe void Test()
            {
                var b = new " + type + @"[1111];
            }
        }
    }");
            }
        }

        [Fact]
        public async Task NoDiagnostic_NoConstantSize()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public unsafe void Test()
            {
                var b = new byte[new Random().Next(9,11)];
            }
        }
    }");
        }

        [Fact]
        public async Task NoDiagnostic_EscapedByMethod()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public void TestEscape(byte[] b) {}
            public unsafe void Test()
            {
                    var b = new byte[1];
                    TestEscape(b);
            }
        }
    }");
        }

        [Fact]
        public async Task NoDiagnostic_EscapedByCtor()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            class TestEscape{ public TestEscape(byte[] b) {}}
            public unsafe void Test()
            {
                    var b = new byte[1];
                    new TestEscape(b);
            }
        }
    }");
        }

        [Fact]
        public async Task NoDiagnostic_EscapedByAssign()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            class TestEscape{ public static byte[] b;}
            public unsafe void Test()
            {
                    var b = new byte[1];
                    TestEscape.b = b;
            }
        }
    }");
        }

        [Fact]
        public async Task NoDiagnostic_ForLoop()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public unsafe void Test()
            {
                for(int i = 0; i < 100; i++)
                {
                    var b = new byte[1];
                }
            }
        }
    }");

        }
        [Fact]
        public async Task NoDiagnostic_Escapes()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public unsafe byte[] Test()
            {
                var b = new byte[1];
                return b;
            }
        }
    }");

        }

        [Fact]
        public async Task NoDiagnostic_EscapesDirectly()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public unsafe byte[] Test()
            {
                return new byte[1];
            }
        }
    }");

        }

        [Fact]
        public async Task NoDiagnostic_AsyncContext()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public unsafe async Task Test()
            {
                var b = new byte[1111];
            }
        }
    }");

        }

        [Fact]
        public async Task NoDiagnostic_MemberArray()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            byte[] b = new byte[1111];
        }
    }");

        }

        [Fact]
        public async Task SingleDiagnostic_SmallByte()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public unsafe void Test()
            {
                var b {|#0:= new byte[1]|};
            }
        }
    }", VerifyCS.Diagnostic(AllocSmallArraysOnStackAnalyzer.DefaultRule).WithLocation(0).WithArguments("new byte[1]"));
        }

        [Fact]
        public async Task SingleDiagnostic_SmallByteAsConstMember()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            const int i = 1;
            public unsafe void Test()
            {
                var b {|#0:= new byte[i]|};
            }
        }
    }", VerifyCS.Diagnostic(AllocSmallArraysOnStackAnalyzer.DefaultRule).WithLocation(0).WithArguments("new byte[i]"));
        }

        [Fact]
        public async Task SingleDiagnostic_InitializerExp()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public unsafe void Test()
            {
                var b {|#0:= new byte[]{1}|};
            }
        }
    }", VerifyCS.Diagnostic(AllocSmallArraysOnStackAnalyzer.DefaultRule).WithLocation(0).WithArguments("new byte[]{1}"));
        }
    }
}
