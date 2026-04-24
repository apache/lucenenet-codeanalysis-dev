### New Rules

Rule ID       | Category | Severity | Notes
--------------|----------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------
LuceneDev1007 | Design   | Warning  | Generic Dictionary<TKey, TValue> indexer should not be used to retrieve values because it may throw KeyNotFoundException (value type value)
LuceneDev1008 | Design   | Warning  | Generic Dictionary<TKey, TValue> indexer should not be used to retrieve values because it may throw KeyNotFoundException (reference type value)
LuceneDev6000 | Usage    | Info     | IDictionary indexer may be used to retrieve values, but must be checked for null before using the value
LuceneDev6001 | Usage    | Error    | Missing StringComparison argument in String overloads of StartsWith/EndsWith/IndexOf/LastIndexOf; must use Ordinal/OrdinalIgnoreCase
LuceneDev6002 | Usage    | Error    | Invalid StringComparison value in String overloads of StartsWith/EndsWith/IndexOf/LastIndexOf; only Ordinal/OrdinalIgnoreCase allowed
LuceneDev6003 | Usage    | Warning  | Redundant StringComparison.Ordinal argument in Span overloads of StartsWith/EndsWith/IndexOf/LastIndexOf; should be removed
LuceneDev6004 | Usage    | Error    | Invalid StringComparison value in Span overloads of StartsWith/EndsWith/IndexOf/LastIndexOf; only Ordinal or OrdinalIgnoreCase allowed
LuceneDev6005 | Usage    | Info     | Single-character string arguments should use the char overload of StartsWith/EndsWith/IndexOf/LastIndexOf instead of a string
