### New Rules

Rule ID       | Category | Severity | Notes
--------------|----------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------
LuceneDev1009 | Design   | Warning  | Fields should not be public; use a public property instead
LuceneDev1010 | Design   | Warning  | Properties should not have a setter without a getter
LuceneDev1011 | Design   | Warning  | Properties should not return arrays
LuceneDev1012 | Design   | Warning  | Methods should not return writable arrays
LuceneDev1013 | Design   | Warning  | Public members should not be nullable enums
LuceneDev1014 | Design   | Disabled | Members should not accept or return IEnumerable<T> (one-time port aid; disabled by default)
LuceneDev1015 | Design   | Warning  | Members should not accept or return List<T> or Dictionary<K, V>; prefer interface types
LuceneDev7000 | Naming   | Warning  | Private fields should be camelCase, optionally with a leading underscore
LuceneDev7001 | Naming   | Warning  | Protected fields should use the 'm_' prefix followed by camelCase
LuceneDev7002 | Naming   | Warning  | Method parameters should be camelCase
LuceneDev7003 | Naming   | Warning  | Interface names should begin with 'I'
LuceneDev7004 | Naming   | Warning  | Class names should use PascalCase
LuceneDev7005 | Naming   | Warning  | Public members should not contain the word 'Comparator'; use 'Comparer'
LuceneDev7006 | Naming   | Warning  | Public members should not be named 'Size'; use 'Count' or 'Length'
LuceneDev7007 | Naming   | Warning  | Public members should not contain non-.NET numeric type names (Int/Long/Short/Float)
LuceneDev7008 | Naming   | Warning  | Type names should not contain non-.NET numeric type names (Int/Long/Short/Float)
