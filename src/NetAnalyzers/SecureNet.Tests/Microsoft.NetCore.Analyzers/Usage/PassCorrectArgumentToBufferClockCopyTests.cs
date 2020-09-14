// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using SecureNet.Category.Usage;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    SecureNet.Category.Usage.PassCorrectArgumentToBufferClockCopyAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SecureNet.Tests
{
    public class PassCorrectArgumentToBufferClockCopyTests
    {
        [Fact]
        public async Task CA2250_NotArrayTypeButConvertedType_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

public class C
{
    public static implicit operator int[](C c)
        {
            return Array.Empty<int>();
        }
    void BlockCopy(object a, object b, object c,object d,object e) {}
    public void Method()
    {
        var intArray = new byte[1];
        var intArrayDest = new byte[1];
        Buffer.BlockCopy(new C(), 0, intArrayDest, 0, intArray.Length);
    }
}");
        }

        [Fact]
        public async Task CA2250_BufferBlockCopyInvocation_NotSameLengthValueUsed_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

public class C
{
    public void Method()
    {
        var intArray = new byte[1];
        var intArrayDest = new byte[1];
        Buffer.BlockCopy(intArray, 0, intArrayDest, 0, intArrayDest.Length); //makes no sense to use intArrayDest here, this is just to test that the identifier matches
    }
}");
        }

        [Fact]
        public async Task CA2250_NotBufferBlockCopyInvocation_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

public class C
{
    void BlockCopy(object a, object b, object c,object d,object e) {}
    public void Method()
    {
        var intArray = new byte[1];
        var intArrayDest = new byte[1];
        BlockCopy(intArray, 0, intArrayDest, 0, intArray.Length);
    }
}");
        }

        [Fact]
        public async Task CA2250_CopyingByteArrayWithLength_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

public class C
{
    public void Method()
    {
        var byteArray = new byte[1];
        var byteArrayDest = new byte[1];
        Buffer.BlockCopy(byteArray, 0, byteArrayDest, 0, byteArray.Length);
    }
}");
        }

        [Fact]
        public async Task CA2250_CopyingIntArrayWithIntLength_Diagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

public class C
{
    public void Method()
    {
        var intArray = new int[1];
        var intArrayDest = new int[1];
        {|#0:Buffer.BlockCopy(intArray, 0, intArrayDest, 0, intArray.Length)|};
    }
}",
                VerifyCS.Diagnostic(PassCorrectArgumentToBufferClockCopyAnalyzer.DefaultRule).WithLocation(0));
        }
    }
}