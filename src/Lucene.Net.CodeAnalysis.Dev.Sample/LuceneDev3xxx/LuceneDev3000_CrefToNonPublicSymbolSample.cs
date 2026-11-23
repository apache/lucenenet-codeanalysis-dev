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

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev3xxx;

public class LuceneDev3000_CrefToNonPublicSymbolSample
{
    private bool useWhiteList;

    /// <summary>
    /// VIOLATION: <see cref="useWhiteList"/> is a private field, so this produces a broken link
    /// in the API documentation.
    /// </summary>
    public bool Accept() => useWhiteList;

    /// <summary>
    /// OK: references to non-public symbols are fine from a non-public symbol's doc comment,
    /// such as <see cref="useWhiteList"/>.
    /// </summary>
    internal bool AcceptInternal() => useWhiteList;
}
