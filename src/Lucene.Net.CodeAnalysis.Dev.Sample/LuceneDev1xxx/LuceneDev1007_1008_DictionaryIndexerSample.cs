/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using System.Collections.Generic;

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev1xxx;

public class LuceneDev1007_1008_DictionaryIndexerSample
{
    public int GetIntValue(IDictionary<string, int> dict, string key)
    {
        // LuceneDev1007 (value-type value): indexer may throw KeyNotFoundException.
        return dict[key];
    }

    public string GetStringValue(IDictionary<string, string> dict, string key)
    {
        // LuceneDev1008 (reference-type value): indexer may throw KeyNotFoundException.
        return dict[key];
    }

    public void ReadOnlyUsage(IReadOnlyDictionary<string, string> dict, string key)
    {
        // LuceneDev1008: also applies to IReadOnlyDictionary<TKey, TValue>.
        var value = dict[key];
    }

    public void ConcreteDictionaryUsage(Dictionary<string, string> dict, string key)
    {
        // LuceneDev1008: Dictionary<TKey, TValue> implements IDictionary<TKey, TValue>.
        var value = dict[key];
    }

    public void AssignmentIsFine(Dictionary<string, int> dict, string key)
    {
        // No diagnostic: indexer setter does not throw.
        dict[key] = 42;
    }
}
