/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.Utility
{
    internal static class CodeActionHelper
    {
        /// <summary>
        /// Create a CodeAction using a resource string and formatting arguments.
        /// </summary>
        public static CodeAction CreateFromResource(
            string resourceValue,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey,
            params object[] args)
        {
            var title = string.Format(resourceValue, args);
            return CodeAction.Create(title, createChangedDocument, equivalenceKey);
        }
    }
}
