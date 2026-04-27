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
using System.Globalization;

namespace Lucene.Net.CodeAnalysis.Dev.Sample.LuceneDev2xxx;

public class LuceneDev2000_2008_NumericCultureSample
{
    public void TriggerAll()
    {
        // 2000: BCL Parse / TryParse without IFormatProvider
        var i = int.Parse("1");
        double.TryParse("1.5", out _);
        long.Parse("42".AsSpan());

        // 2001: BCL ToString without IFormatProvider
        var s1 = i.ToString();
        var s2 = i.ToString("D");
        Span<char> buffer = stackalloc char[16];
        i.TryFormat(buffer, out _);

        // 2002: System.Convert without IFormatProvider
        var c1 = Convert.ToInt32("3");
        var c2 = Convert.ToString(7);

        // 2003: string.Format without IFormatProvider, with numeric arg
        var f1 = string.Format("{0}", i);

        // 2005: implicit numeric concatenation
        var concat = "id=" + i;
        var concat2 = "" + (i + 1);

        // 2006: implicit numeric interpolation
        var interp = $"value={i}";

        // 2007: explicit IFormatProvider, but not InvariantCulture
        var nonInvariant = i.ToString(CultureInfo.CurrentCulture);

        // 2008 (off by default): explicit InvariantCulture
        var invariant = i.ToString(CultureInfo.InvariantCulture);

        // The following should NOT trigger:
        // - non-numeric Parse
        var g = Guid.Parse("00000000-0000-0000-0000-000000000000");
        // - FormattableString.Invariant interpolation
        var ok = FormattableString.Invariant($"value={i}");
    }

    // 2001 exemption: parameterless ToString() inside a ToString() override should NOT trigger.
    public class WithToStringOverride
    {
        public int Value { get; set; }

        public override string ToString()
        {
            int x = Value;
            return x.ToString();
        }
    }
}

public class LuceneDev2004_J2NSample
{
    public void TriggerJ2N()
    {
        // 2004: J2N numeric methods without IFormatProvider.
        // J2N.Numerics.Int32 has static ToString(int) and ToString(int, string) without IFormatProvider.
        var s = J2N.Numerics.Int32.ToString(42);
        var s2 = J2N.Numerics.Int32.ToString(42, "D");
    }
}
