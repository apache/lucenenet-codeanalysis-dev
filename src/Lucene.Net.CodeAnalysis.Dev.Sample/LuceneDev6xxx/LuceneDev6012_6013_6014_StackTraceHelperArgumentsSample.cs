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

using System.Runtime.CompilerServices;
using Lucene.Net.Util;

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev6xxx
{
    public class LuceneDev6012_6013_6014_Base
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Flush()
        {
            System.Console.WriteLine(1 + 2);
        }
    }

    public class LuceneDev6012_6013_6014_Target : LuceneDev6012_6013_6014_Base
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Merge()
        {
            System.Console.WriteLine(1 + 2);
        }
    }

    public class LuceneDev6012_6013_6014_Caller
    {
        public void Check()
        {
            // Triggers LuceneDev6012 (Warning): the className argument should use nameof.
            StackTraceHelper.DoesStackTraceContainMethod(
                "LuceneDev6012_6013_6014_Target",
                nameof(LuceneDev6012_6013_6014_Target.Merge));

            // Triggers LuceneDev6013 (Warning): the methodName argument should use nameof.
            StackTraceHelper.DoesStackTraceContainMethod(
                nameof(LuceneDev6012_6013_6014_Target),
                "Merge");

            // Also triggers LuceneDev6013 (Warning), on the single-argument overload.
            StackTraceHelper.DoesStackTraceContainMethod("Merge");

            // Triggers LuceneDev6014 (Error): the type does not declare this method, so the
            // call can never match. Also triggers LuceneDev6013, since this is a literal.
            StackTraceHelper.DoesStackTraceContainMethod(
                nameof(LuceneDev6012_6013_6014_Target),
                "Missing");

            // Triggers LuceneDev6014 (Error): Flush is inherited, so the stack frame's
            // declaring type is the base class and the call can never match.
            StackTraceHelper.DoesStackTraceContainMethod(
                nameof(LuceneDev6012_6013_6014_Target),
                nameof(LuceneDev6012_6013_6014_Target.Flush));

            // No diagnostic: both arguments use nameof and the type declares the method.
            StackTraceHelper.DoesStackTraceContainMethod(
                nameof(LuceneDev6012_6013_6014_Target),
                nameof(LuceneDev6012_6013_6014_Target.Merge));
        }
    }
}
