// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Globalization;
    using System.Threading;

    class IOThreadScheduler
    {
        const int MaximumCapacity = 0x8000;

        static class Bits
        {
            public const int HiShift = 32 / 2;

            public const int HiOne = 1 << HiShift;
            public const int LoHiBit = HiOne >> 1;
            public const int HiHiBit = LoHiBit << HiShift;
            public const int LoCountMask = LoHiBit - 1;
            public const int HiCountMask = LoCountMask << HiShift;
            public const int LoMask = LoCountMask | LoHiBit;
            public const int HiMask = HiCountMask | HiHiBit;
            public const int HiBits = LoHiBit | HiHiBit;

            public static int Count(int slot)
            {
                return ((slot >> HiShift) - slot + 2 & LoMask) - 1;
            }

            public static int CountNoIdle(int slot)
            {
                return (slot >> HiShift) - slot + 1 & LoMask;
            }

            public static int IncrementLo(int slot)
            {
                return slot + 1 & LoMask | slot & HiMask;
            }

            // This method is only valid if you already know that (gate & HiBits) != 0.
            public static bool IsComplete(int gate)
            {
                return (gate & HiMask) == gate << HiShift;
            }
        }

        static IOThreadScheduler current = new IOThreadScheduler(32, 32);
        readonly ScheduledOverlapped overlapped;

        readonly Slot[] slots;

        readonly Slot[] slotsLowPri;

        int headTail = -2 << Bits.HiShift;

        int headTailLowPri = -1 << Bits.HiShift;

        IOThreadScheduler(int capacity, int capacityLowPri)
        {
            this.slots = new Slot[capacity];
            this.slotsLowPri = new Slot[capacityLowPri];
            this.overlapped = new ScheduledOverlapped();
        }

        public static void ScheduleCallbackNoFlow(Action<object> callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            bool queued = false;
            while (!queued)
            {
                try { }
                finally
                {
                    queued = IOThreadScheduler.current.ScheduleCallbackHelper(callback, state);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        public static void ScheduleCallbackLowPriNoFlow(Action<object> callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            bool queued = false;
            while (!queued)
            {
                try { }
                finally
                {
                    queued = IOThreadScheduler.current.ScheduleCallbackLowPriHelper(callback, state);
                }
            }
        }

        bool ScheduleCallbackHelper(Action<object> callback, object state)
        {
            int slot = Interlocked.Add(ref this.headTail, Bits.HiOne);

            bool wasIdle = Bits.Count(slot) == 0;
            if (wasIdle)
            {
                slot = Interlocked.Add(ref this.headTail, Bits.HiOne);
            }

            if (Bits.Count(slot) == -1)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_HeadTailOverflow));
            }

            bool wrapped;
            bool queued = this.slots[slot >> Bits.HiShift & SlotMask].TryEnqueueWorkItem(callback, state, out wrapped);

            if (wrapped)
            {
                IOThreadScheduler next =
                    new IOThreadScheduler(Math.Min(this.slots.Length * 2, MaximumCapacity), this.slotsLowPri.Length);
                Interlocked.CompareExchange(ref IOThreadScheduler.current, next, this);
            }

            if (wasIdle)
            {
                this.overlapped.Post(this);
            }

            return queued;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        bool ScheduleCallbackLowPriHelper(Action<object> callback, object state)
        {
            int slot = Interlocked.Add(ref this.headTailLowPri, Bits.HiOne);

            bool wasIdle = false;
            if (Bits.CountNoIdle(slot) == 1)
            {
                int ht = this.headTail;

                if (Bits.Count(ht) == -1)
                {
                    int interlockedResult = Interlocked.CompareExchange(ref this.headTail, ht + Bits.HiOne, ht);
                    if (ht == interlockedResult)
                    {
                        wasIdle = true;
                    }
                }
            }

            if (Bits.CountNoIdle(slot) == 0)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_LowPriorityHeadTailOverflow));
            }

            bool wrapped;
            bool queued = this.slotsLowPri[slot >> Bits.HiShift & SlotMaskLowPri].TryEnqueueWorkItem(
                callback, state, out wrapped);

            if (wrapped)
            {
                IOThreadScheduler next =
                    new IOThreadScheduler(this.slots.Length, Math.Min(this.slotsLowPri.Length * 2, MaximumCapacity));
                Interlocked.CompareExchange(ref IOThreadScheduler.current, next, this);
            }

            if (wasIdle)
            {
                this.overlapped.Post(this);
            }

            return queued;
        }

        void CompletionCallback(out Action<object> callback, out object state)
        {
            int slot = this.headTail;
            int slotLowPri;
            while (true)
            {
                bool wasEmpty = Bits.Count(slot) == 0;
                if (wasEmpty)
                {
                    slotLowPri = this.headTailLowPri;
                    while (Bits.CountNoIdle(slotLowPri) != 0)
                    {
                        if (slotLowPri == (slotLowPri = Interlocked.CompareExchange(ref this.headTailLowPri,
                            Bits.IncrementLo(slotLowPri), slotLowPri)))
                        {
                            this.overlapped.Post(this);
                            this.slotsLowPri[slotLowPri & SlotMaskLowPri].DequeueWorkItem(out callback, out state);
                            return;
                        }
                    }
                }

                if (slot == (slot = Interlocked.CompareExchange(ref this.headTail, Bits.IncrementLo(slot), slot)))
                {
                    if (!wasEmpty)
                    {
                        this.overlapped.Post(this);
                        this.slots[slot & SlotMask].DequeueWorkItem(out callback, out state);
                        return;
                    }
                    slotLowPri = this.headTailLowPri;

                    if (Bits.CountNoIdle(slotLowPri) != 0)
                    {
                        slot = Bits.IncrementLo(slot);
                        if (slot == Interlocked.CompareExchange(ref this.headTail, slot + Bits.HiOne, slot))
                        {
                            slot += Bits.HiOne;
                            continue;
                        }
                    }

                    break;
                }
            }

            callback = null;
            state = null;
        }

        bool TryCoalesce(out Action<object> callback, out object state)
        {
            int slot = this.headTail;
            int slotLowPri;
            while (true)
            {
                if (Bits.Count(slot) > 0)
                {
                    if (slot == (slot = Interlocked.CompareExchange(ref this.headTail, Bits.IncrementLo(slot), slot)))
                    {
                        this.slots[slot & SlotMask].DequeueWorkItem(out callback, out state);
                        return true;
                    }
                    continue;
                }

                slotLowPri = this.headTailLowPri;
                if (Bits.CountNoIdle(slotLowPri) > 0)
                {
                    if (slotLowPri == (slotLowPri = Interlocked.CompareExchange(ref this.headTailLowPri,
                        Bits.IncrementLo(slotLowPri), slotLowPri)))
                    {
                        this.slotsLowPri[slotLowPri & SlotMaskLowPri].DequeueWorkItem(out callback, out state);
                        return true;
                    }
                    slot = this.headTail;
                    continue;
                }

                break;
            }

            callback = null;
            state = null;
            return false;
        }

        int SlotMask
        {
            get
            {
                return this.slots.Length - 1;
            }
        }

        int SlotMaskLowPri
        {
            get
            {
                return this.slotsLowPri.Length - 1;
            }
        }

        ~IOThreadScheduler()
        {
            if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
            {
                Cleanup();
            }
        }

        void Cleanup()
        {
            if (this.overlapped != null)
            {
                this.overlapped.Cleanup();
            }
        }
        struct Slot
        {
            int gate;
            Action<object> heldCallback;
            object heldState;

            public bool TryEnqueueWorkItem(Action<object> callback, object state, out bool wrapped)
            {
                // Register our arrival and check the state of this slot.  If the slot was already full, we wrapped.
                int gateSnapshot = Interlocked.Increment(ref this.gate);
                wrapped = (gateSnapshot & Bits.LoCountMask) != 1;
                if (wrapped)
                {
                    if ((gateSnapshot & Bits.LoHiBit) != 0 && Bits.IsComplete(gateSnapshot))
                    {
                        Interlocked.CompareExchange(ref this.gate, 0, gateSnapshot);
                    }
                    return false;
                }

                this.heldState = state;
                this.heldCallback = callback;

                gateSnapshot = Interlocked.Add(ref this.gate, Bits.LoHiBit);

                if ((gateSnapshot & Bits.HiCountMask) == 0)
                {
                    return true;
                }

                this.heldState = null;
                this.heldCallback = null;

                if (gateSnapshot >> Bits.HiShift != (gateSnapshot & Bits.LoCountMask) ||
                    Interlocked.CompareExchange(ref this.gate, 0, gateSnapshot) != gateSnapshot)
                {
                    gateSnapshot = Interlocked.Add(ref this.gate, Bits.HiHiBit);
                    if (Bits.IsComplete(gateSnapshot))
                    {
                        Interlocked.CompareExchange(ref this.gate, 0, gateSnapshot);
                    }
                }

                return false;
            }

            public void DequeueWorkItem(out Action<object> callback, out object state)
            {
                int gateSnapshot = Interlocked.Add(ref this.gate, Bits.HiOne);

                if ((gateSnapshot & Bits.LoHiBit) == 0)
                {
                    callback = null;
                    state = null;
                    return;
                }

                if ((gateSnapshot & Bits.HiCountMask) == Bits.HiOne)
                {
                    callback = this.heldCallback;
                    state = this.heldState;
                    this.heldState = null;
                    this.heldCallback = null;

                    if ((gateSnapshot & Bits.LoCountMask) != 1 ||
                        Interlocked.CompareExchange(ref this.gate, 0, gateSnapshot) != gateSnapshot)
                    {
                        gateSnapshot = Interlocked.Add(ref this.gate, Bits.HiHiBit);
                        if (Bits.IsComplete(gateSnapshot))
                        {
                            Interlocked.CompareExchange(ref this.gate, 0, gateSnapshot);
                        }
                    }
                }
                else
                {
                    callback = null;
                    state = null;

                    if (Bits.IsComplete(gateSnapshot))
                    {
                        Interlocked.CompareExchange(ref this.gate, 0, gateSnapshot);
                    }
                }
            }
        }

        unsafe class ScheduledOverlapped
        {
            readonly NativeOverlapped* nativeOverlapped;
            IOThreadScheduler scheduler;

            public ScheduledOverlapped()
            {
                this.nativeOverlapped = (new Overlapped()).UnsafePack(new IOCompletionCallback(IOCallback), null);
            }

            void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlappedCallback)
            {
                IOThreadScheduler iots = this.scheduler;
                this.scheduler = null;

                Action<object> callback;
                object state;
                try { }
                finally
                {
                    iots.CompletionCallback(out callback, out state);
                }

                bool found = true;
                while (found)
                {
                    if (callback != null)
                    {
                        callback(state);
                    }

                    try { }
                    finally
                    {
                        found = iots.TryCoalesce(out callback, out state);
                    }
                }
            }

            public void Post(IOThreadScheduler iots)
            {
                this.scheduler = iots;
                ThreadPool.UnsafeQueueNativeOverlapped(this.nativeOverlapped);
            }

            public void Cleanup()
            {
                if (this.scheduler != null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_CleanupCalledOnAnOverlappedThatsInFlight));
                }

                Overlapped.Free(this.nativeOverlapped);
            }
        }
    }
}
