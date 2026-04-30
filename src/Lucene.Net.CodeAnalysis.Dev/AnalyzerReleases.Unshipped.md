### New Rules

Rule ID       | Category | Severity | Notes
--------------|----------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------
LuceneDev1007 | Design   | Warning  | Generic Dictionary<TKey, TValue> indexer should not be used to retrieve values because it may throw KeyNotFoundException (value type value)
LuceneDev1008 | Design   | Warning  | Generic Dictionary<TKey, TValue> indexer should not be used to retrieve values because it may throw KeyNotFoundException (reference type value)
LuceneDev2000 | Globalization | Warning | Numeric Parse/TryParse without IFormatProvider; specify CultureInfo.InvariantCulture (or CurrentCulture) explicitly
LuceneDev2001 | Globalization | Warning | Numeric ToString/TryFormat without IFormatProvider; specify CultureInfo.InvariantCulture (or CurrentCulture) explicitly
LuceneDev2002 | Globalization | Warning | System.Convert numeric to/from string without IFormatProvider; specify CultureInfo.InvariantCulture (or CurrentCulture) explicitly
LuceneDev2003 | Globalization | Warning | string.Format with numeric argument and no IFormatProvider; pass CultureInfo.InvariantCulture (or CurrentCulture) as the first argument
LuceneDev2004 | Globalization | Warning | J2N.Numerics.* method without IFormatProvider; specify CultureInfo.InvariantCulture (or CurrentCulture) explicitly
LuceneDev2005 | Globalization | Warning | Numeric value concatenated with string formats using current culture; wrap with .ToString(CultureInfo.InvariantCulture) explicitly
LuceneDev2006 | Globalization | Warning | Numeric value interpolated into string formats using current culture; use FormattableString.Invariant or wrap with .ToString(CultureInfo.InvariantCulture) explicitly
LuceneDev2007 | Globalization | Warning | Numeric format/parse passes a non-invariant IFormatProvider; suppress when intentional
LuceneDev2008 | Globalization | Disabled | Numeric format/parse passes CultureInfo.InvariantCulture (review-sweep aid; default Info severity, disabled by default)
LuceneDev4000 | Performance | Warning | [MethodImpl(MethodImplOptions.NoInlining)] has no effect on interface or abstract methods (the attribute is not inherited)
LuceneDev4001 | Performance | Warning | [MethodImpl(MethodImplOptions.NoInlining)] should not be used on empty-bodied methods (no benefit, harms performance)
LuceneDev4002 | Performance | Warning | Methods referenced by the 2-argument StackTraceHelper.DoesStackTraceContainMethod overload should be marked [MethodImpl(MethodImplOptions.NoInlining)] when the method body is non-empty
LuceneDev6000 | Usage    | Info     | IDictionary indexer may be used to retrieve values, but must be checked for null before using the value
LuceneDev6001 | Usage    | Error    | Missing StringComparison argument in String overloads of StartsWith/EndsWith/IndexOf/LastIndexOf; must use Ordinal/OrdinalIgnoreCase
LuceneDev6002 | Usage    | Error    | Invalid StringComparison value in String overloads of StartsWith/EndsWith/IndexOf/LastIndexOf; only Ordinal/OrdinalIgnoreCase allowed
LuceneDev6003 | Usage    | Warning  | Redundant StringComparison.Ordinal argument in Span overloads of StartsWith/EndsWith/IndexOf/LastIndexOf; should be removed
LuceneDev6004 | Usage    | Error    | Invalid StringComparison value in Span overloads of StartsWith/EndsWith/IndexOf/LastIndexOf; only Ordinal or OrdinalIgnoreCase allowed
LuceneDev6005 | Usage    | Info     | Single-character string arguments should use the char overload of StartsWith/EndsWith/IndexOf/LastIndexOf instead of a string
