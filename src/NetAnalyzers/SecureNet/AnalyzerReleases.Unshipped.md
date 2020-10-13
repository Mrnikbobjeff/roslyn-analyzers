; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
HA1000 | Performance | Info | OverrideReadAndWriteAsyncOnApmStreamAnalyzer
HA1001 | Performance | Info | OverrideReadAndWriteAsyncOnApmStreamAnalyzer
HA1839 | Performance | Info | PreferContainsKeyOrValueOverPropertyAccessAnalyzer
HA1840 | Performance | Info | PreferContainsKeyOrValueOverPropertyAccessAnalyzer
HA1841 | Performance | Info | PreferSpanArgumentOverSubstringAnalyzer
HA1843 | Performance | Info | ExtractConstArrayAnalyzerAnalyzer
HA1846 | Performance | Info | PreferReadOnlySpanOverArrayOnPrivateFieldsAnalyzer
UA2052 | Usage | Warning | TaskDelayUseCTOnWaitAnyAnalyzer
UA2053 | Usage | Warning | TaskWaitAllAnalyzer
UA2054 | Usage | Warning | UseThrowIfCancellationRequestedAnalyzer
UA2250 | Usage | Warning | DetectPLINQNopsAnalyzer
UA2251 | Usage | Warning | PassCorrectArgumentToBufferClockCopyAnalyzer