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

using JCG = J2N.Collections.Generic;

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev6xxx;

public class LuceneDev6006_6007_PreferJ2NCollectionSample
{
    // BCL collection types: trigger LuceneDev6006 (Warning).
    private readonly IList<int> list = new List<int>();
    private readonly ISet<string> set = new HashSet<string>();
    private readonly IDictionary<string, int> map = new Dictionary<string, int>();

    // J2N collection types: no diagnostic.
    private readonly IList<int> jcgList = new JCG.List<int>();
    private readonly ISet<string> jcgSet = new JCG.HashSet<string>();
    private readonly IDictionary<string, int> jcgMap = new JCG.Dictionary<string, int>();

    // JCG.LinkedHashSet<T> and JCG.LinkedDictionary<TKey, TValue> trigger LuceneDev6007 (Warning)
    // once JCG.OrderedHashSet<T> and JCG.OrderedDictionary<TKey, TValue> are available (J2N 2.2+).
    private readonly ISet<string> linkedSet = new JCG.LinkedHashSet<string>();
}
