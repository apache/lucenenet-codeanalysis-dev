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

using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    public static partial class Descriptors
    {
        static readonly ConcurrentDictionary<Category, string> categoryMapping = new();

        static DiagnosticDescriptor Diagnostic(
            string id,
            Category category,
            DiagnosticSeverity defaultSeverity)
            => Diagnostic(id, category, defaultSeverity, isEnabledByDefault: true);

        static DiagnosticDescriptor Diagnostic(
            string id,
            Category category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault)
        {
            //string? helpLink = null;
            var categoryString = categoryMapping.GetOrAdd(category, c => c.ToString());

            var title = new LocalizableResourceString($"{id}_AnalyzerTitle", Resources.ResourceManager, typeof(Resources));
            var messageFormat = new LocalizableResourceString($"{id}_AnalyzerMessageFormat", Resources.ResourceManager, typeof(Resources));
            var description = new LocalizableResourceString($"{id}_AnalyzerDescription", Resources.ResourceManager, typeof(Resources));

            //return new DiagnosticDescriptor(id, title, messageFormat, categoryString, defaultSeverity, isEnabledByDefault: true, helpLinkUri: helpLink);
            return new DiagnosticDescriptor(id, title, messageFormat, categoryString, defaultSeverity, isEnabledByDefault);
        }
    }
}
