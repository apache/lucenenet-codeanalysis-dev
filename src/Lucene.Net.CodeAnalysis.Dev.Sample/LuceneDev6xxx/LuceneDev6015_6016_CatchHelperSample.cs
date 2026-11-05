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

using System;
using System.IO;

namespace Lucene
{
    // Stand-in for the real Lucene.ExceptionExtensions class in the Lucene.Net assembly.
    internal static class ExceptionExtensions
    {
        public static bool IsException(this Exception e) => true;

        public static bool IsIOException(this Exception e) => e is IOException or ObjectDisposedException;
    }
}

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev6xxx
{
    public class LuceneDev6015_6016_CatchHelperSample
    {
        public void TooSpecific()
        {
            try
            {
                DoWork();
            }
            // Catches a more specific type than System.Exception: triggers LuceneDev6015 (Error).
            catch (IOException e) when (e.IsIOException())
            {
                throw new InvalidOperationException("Failed", e);
            }
        }

        public void NoFilter()
        {
            try
            {
                DoWork();
            }
            // No when clause: triggers LuceneDev6016 (Warning).
            catch (IOException e)
            {
                throw new InvalidOperationException("Failed", e);
            }
        }

        public void Correct()
        {
            try
            {
                DoWork();
            }
            // No diagnostic.
            catch (Exception e) when (e.IsIOException())
            {
                throw new InvalidOperationException("Failed", e);
            }
        }

        private static void DoWork()
        {
        }
    }
}
