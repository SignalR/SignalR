// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;

    static class HashCode
    {
        public static int Combine(int h1, int h2)
        {
            return ((h1 << 5) + h1) ^ h2;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        public static int Combine(int h1, int h2, int h3)
        {
            return Combine(Combine(h1, h2), h3);
        }

        public static int Combine(int h1, int h2, int h3, int h4)
        {
            return Combine(Combine(h1, h2), Combine(h3, h4));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        public static int Combine(int h1, int h2, int h3, int h4, int h5)
        {
            return Combine(Combine(h1, h2, h3, h4), h5);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        public static int Combine(int h1, int h2, int h3, int h4, int h5, int h6)
        {
            return Combine(Combine(h1, h2, h3, h4), Combine(h5, h6));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        public static int Combine(int h1, int h2, int h3, int h4, int h5, int h6, int h7)
        {
            return Combine(Combine(h1, h2, h3, h4), Combine(h5, h6, h7));
        }

        public static int Combine(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8)
        {
            return Combine(Combine(h1, h2, h3, h4), Combine(h5, h6, h7, h8));
        }
    }
}
