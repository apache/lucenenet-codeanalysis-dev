### New Rules

 Rule ID       | Category | Severity | Notes
---------------|----------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------
 LuceneDev1007 | Design   | Warning  | Generic Dictionary<TKey, TValue> indexer should not be used to retrieve values because it may throw KeyNotFoundException (value type value)
 LuceneDev1008 | Design   | Warning  | Generic Dictionary<TKey, TValue> indexer should not be used to retrieve values because it may throw KeyNotFoundException (reference type value)
 LuceneDev6000 | Usage    | Info     | IDictionary indexer may be used to retrieve values, but must be checked for null before using the value
