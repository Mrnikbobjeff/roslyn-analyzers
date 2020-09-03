// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.CSharp.Analyzers.InteropServices.CSharpMarkBooleanPInvokeArgumentsWithMarshalAsAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
namespace Microsoft.NetCore.Analyzers.InteropServices.UnitTests
{
    public class MarkBooleanPInvokeArgumentsWithMarshalAsTests
    {
        private DiagnosticResult CSharpResult1414(int line, int column, params string[] arguments)
           => VerifyCS.Diagnostic(MarkBooleanPInvokeArgumentsWithMarshalAsAnalyzer.DefaultRule)
               .WithLocation(line, column)
               .WithArguments(arguments);
        private DiagnosticResult CSharpResult1414ReturnRule(int line, int column, params string[] arguments)
           => VerifyCS.Diagnostic(MarkBooleanPInvokeArgumentsWithMarshalAsAnalyzer.ReturnRule)
               .WithLocation(line, column)
               .WithArguments(arguments);
        [Fact]
        public async Task CA1414CSharpTest()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Runtime.InteropServices;

public class C
{
    [DllImport(""user32.dll"")]
    static extern void Method3(bool parameter); // no Attribute is there

    [DllImport(""user32.dll"")]
    static extern bool Method3(); // no Attribute is there

    [DllImport(""user32.dll"")]
    static extern void Method4([MarshalAs(UnmanagedType.Bool)] bool flag); // Attribute OK
}
",
                CSharpResult1414(8, 24),
            CSharpResult1414ReturnRule(11, 24, "C.Method3()"));
        }
    }
}