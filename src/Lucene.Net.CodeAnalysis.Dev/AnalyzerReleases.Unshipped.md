### New Rules

Rule ID       | Category | Severity | Notes
--------------|----------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------
LuceneDev1007 | Design   | Warning  | Generic Dictionary<TKey, TValue> indexer should not be used to retrieve values because it may throw KeyNotFoundException (value type value)
LuceneDev1008 | Design   | Warning  | Generic Dictionary<TKey, TValue> indexer should not be used to retrieve values because it may throw KeyNotFoundException (reference type value)
LuceneDev6000 | Usage    | Info     | IDictionary indexer may be used to retrieve values, but must be checked for null before using the value
LuceneDev6001 | Usage    | Error    | String overloads of StartsWith/EndsWith/IndexOf/LastIndexOf must be called with StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase
LuceneDev6002 | Usage    | Warning  | Span overloads of StartsWith/EndsWith/IndexOf/LastIndexOf should not pass non-Ordinal StringComparison
LuceneDev6003 | Usage    | Info     | Single-character string arguments should use the char overload of StartsWith/EndsWith/IndexOf/LastIndexOf instead of a string
