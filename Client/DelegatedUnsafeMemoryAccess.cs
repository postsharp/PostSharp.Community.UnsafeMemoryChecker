using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#pragma warning disable 1591

namespace PostSharp.Community.UnsafeMemoryChecker
{
    /// <summary>
    /// Hosts information about memory segments that can be accessed without triggering errors. Do not refer to this
    /// class directly in your code. Use <c>PostSharp.Community.UnsafeMemoryChecker.UnsafeMemoryAccess</c> instead.
    /// </summary>
    public static unsafe class DelegatedUnsafeMemoryAccess
    {
        private const int memoryPadding = 64;
        static readonly List<Segment> safeSegments = new List<Segment>(65536);
        static readonly List<Segment> tombstones = new List<Segment>(65536);
        private static readonly byte[] paddingMagicBytes = {0xDE, 0xAD, 0xBE, 0xEF};
        private static readonly GCHandle paddingMagicHandle = GCHandle.Alloc( paddingMagicBytes, GCHandleType.Pinned );
        private static readonly byte* paddingMagicPtr = (byte*) paddingMagicHandle.AddrOfPinnedObject();
        public static int GetAllocSize( int size )
        {
            return size +2 * memoryPadding;
        }

        public static int GetLiveSize( int size )
        {
            return size - 2 * memoryPadding;
        }

        public static int GetLiveOffset()
        {
            return memoryPadding;
        }

        public static IntPtr GetLiveAddress( IntPtr allocAddress )
        {
            return allocAddress + memoryPadding;
        }

        public static void AddSafeSegment( void* allocAddress, int allocSize,
             string memberName,
            string sourceFilePath ,
            int sourceLineNumber )
        {
            AddSafeSegment( (IntPtr) allocAddress, allocSize, memberName, sourceFilePath, sourceLineNumber );
        }


        private static void AddSafeSegment( IntPtr allocAddress, int allocSize,
           string memberName ,
             string sourceFilePath ,
           int sourceLineNumber)
        {
            AddSafeSegment( allocAddress, allocSize, allocAddress, allocSize, memberName, sourceFilePath, sourceLineNumber );
        }

        
        public static void AddSafeSegment( IntPtr allocAddress, int allocSize, IntPtr liveAddress, int liveSize,
             string memberName ,
            string sourceFilePath ,
             int sourceLineNumber )
        {
            lock ( safeSegments )
            {
                Segment segment = new Segment( allocAddress, allocSize, liveAddress, liveSize, memberName, sourceFilePath, sourceLineNumber );
                PadSegment( segment );
                int index = GetNearestLesserSegment( safeSegments, liveAddress );

                // Check internal overlap.

                CheckOverlapWithExisting(safeSegments, segment);

                if ( index == safeSegments.Count - 1 )
                {
                    safeSegments.Add( segment );
                }
                else
                {
                    safeSegments.Insert( index + 1, segment );
                }
            }
        }


        public static void RemoveSafeSegment( IntPtr address,
           string memberName ,
            string sourceFilePath ,
           int sourceLineNumber)
        {
            lock ( safeSegments )
            {
                int index = safeSegments.BinarySearch( new Segment( address, 0 ) );

                if ( index < 0 )
                    throw new InvalidOperationException( "Could not find the segment." );

                if ( safeSegments[index].AllocAddress != address )
                    throw new InvalidOperationException( "Matched segment alloc address invalid." );

                Segment segment = safeSegments[index];
                safeSegments.RemoveAt( index );
                CheckSegmentPadding( segment );
                segment = segment.Dealloc( memberName, sourceFilePath, sourceLineNumber );

                int tombIndex = GetNearestLesserSegment( tombstones, address );

                if ( tombIndex >= tombstones.Count - 1 )
                {
                    tombstones.Add( segment );
                }
                else
                {
                    tombstones.Insert( tombIndex + 1, segment );
                }
            }
        }


        public static void CheckAddress( void* address, int size )
        {
            lock ( safeSegments )
            {
                int index = GetNearestLesserSegment( safeSegments, (IntPtr) address );
                
                if ( index < 0 || (ulong) address + (uint)size > (ulong) safeSegments[index].LiveAddress + (uint)safeSegments[index].LiveSize )
                {
                    int tombIndex = GetNearestLesserSegment( tombstones, (IntPtr) address );

                    throw new AccessViolationException(
                        $"Block {(ulong) address:x}-{(ulong) address + (uint) size:x} is not in a safe segment. Closest live segment: {(index < 0 ? null : (Segment?) safeSegments[index])}. Closest tombstones segment: {(tombIndex < 0 ? null : (Segment?) tombstones[tombIndex])}");
                }
            }
        }
        
        private static void CheckOverlapWithExisting(List<Segment> segments, Segment newSegment)
        {
            int prevSegment = GetNearestLesserSegment(segments, newSegment.AllocAddress );
            int nextSegment = GetNearestHigherSegment(segments, newSegment.AllocAddress + newSegment.AllocSize );

            for(int i = prevSegment; i < nextSegment; i++)
            {
                if (i < 0 || i >= segments.Count)
                    continue;

                if (Overlaps(segments[i], newSegment))
                {
                    throw new AccessViolationException(
                        $"New segment {newSegment} overlaps with live segment {segments[i]}");
                }
            }
        }

        private static bool Overlaps(Segment a, Segment b)
        {
            if ((byte*)a.AllocAddress < (byte*)b.AllocAddress + b.AllocSize && (byte*)b.AllocAddress < (byte*)a.AllocAddress + a.AllocSize)
                return true;

            return false;
        }

        private static void PadSegment( Segment segment )
        {
            for (byte* ptr = (byte*)segment.AllocAddress; ptr < (byte*)segment.LiveAddress; ptr++)
            {
                *ptr = paddingMagicPtr[(ulong) ptr%4];
            }

            for (byte* ptr = (byte*)segment.LiveAddress + segment.LiveSize; ptr < (byte*)segment.AllocAddress + segment.AllocSize; ptr++)
            {
                *ptr = paddingMagicPtr[(ulong) ptr%4];
            }
        }

        private static void CheckSegmentPadding( Segment segment )
        {
            for ( byte* ptr = (byte*) segment.AllocAddress; ptr < (byte*)segment.LiveAddress; ptr++ )
            {
                if ( *ptr != paddingMagicPtr[(ulong) ptr%4] )
                {
                    throw new Exception($"Diagnostic padding of block {segment} was changed.");
                }
            }

            for ( byte* ptr = (byte*)segment.LiveAddress + segment.LiveSize; ptr < (byte*)segment.AllocAddress + segment.AllocSize; ptr++ )
            {
                if ( *ptr != paddingMagicPtr[(ulong) ptr%4] )
                {
                    throw new Exception($"Diagnostic padding of block {segment} was changed.");
                }
            }
        }

        private static int GetNearestLesserSegment( List<Segment> segments, IntPtr address )
        {
            int index = segments.BinarySearch( new Segment( address, 0 ) );

            if ( index < 0 )
            {
                return ~index - 1;
            }

            return index;
        }

        private static int GetNearestHigherSegment(List<Segment> segments, IntPtr address)
        {
            int index = segments.BinarySearch(new Segment(address, 0));

            if (index < 0)
            {
                return ~index;
            }

            return index;
        }

        struct Segment : IComparable<Segment>
        {
            public readonly IntPtr AllocAddress;
            public readonly int AllocSize;
            public readonly IntPtr LiveAddress;
            public readonly int LiveSize;
            public readonly string AllocMemberName;
            public readonly string AllocSourceFilePath;
            public readonly int AllocSourceLineNumber;
            public string DeallocMemberName;
            public string DeallocSourceFilePath;
            public int DeallocSourceLineNumber;
            public bool Deallocated;

            public Segment( IntPtr allocAddress, int allocSize )
            {
                AllocAddress = allocAddress;
                AllocSize = allocSize;
                LiveAddress = allocAddress;
                LiveSize = allocSize;
                AllocMemberName = null;
                AllocSourceFilePath = null;
                AllocSourceLineNumber = 0;
                DeallocMemberName = null;
                DeallocSourceFilePath = null;
                DeallocSourceLineNumber = 0;
                Deallocated = false;
            }

            public Segment( IntPtr allocAddress, int allocSize, IntPtr liveAddress, int liveSize, string memberName, string sourceFilePath, int sourceLineNumber )
            {
                AllocAddress = allocAddress;
                AllocSize = allocSize;
                LiveAddress = liveAddress;
                LiveSize = liveSize;
                AllocMemberName = memberName;
                AllocSourceFilePath = sourceFilePath;
                AllocSourceLineNumber = sourceLineNumber;
                DeallocMemberName = null;
                DeallocSourceFilePath = null;
                DeallocSourceLineNumber = 0;
                Deallocated = false;
            }

            public Segment(Segment allocatedSegment, string deallocMemberName, string deallocSourceFilePath, int deallocSourceLineNumber)
            {
                AllocAddress = allocatedSegment.AllocAddress;
                AllocSize = allocatedSegment.AllocSize;
                LiveAddress = allocatedSegment.LiveAddress;
                LiveSize = allocatedSegment.LiveSize;
                AllocMemberName = allocatedSegment.AllocMemberName;
                AllocSourceFilePath = allocatedSegment.AllocSourceFilePath;
                AllocSourceLineNumber = allocatedSegment.AllocSourceLineNumber;
                DeallocMemberName = deallocMemberName;
                DeallocSourceFilePath = deallocSourceFilePath;
                DeallocSourceLineNumber = deallocSourceLineNumber;
                Deallocated = true;
            }

            public Segment Dealloc( string memberName, string sourceFilePath, int sourceLineNumber )
            {
                return new Segment(this, memberName, sourceFilePath, sourceLineNumber);
            }

            public int CompareTo( Segment other )
            {
                return AllocAddress.ToInt64().CompareTo( other.AllocAddress.ToInt64() );
            }

            public override string ToString()
            {
                if (!Deallocated)
                {
                    if (AllocAddress != LiveAddress || AllocSize != LiveSize)
                        return string.Format("({0:x}){1:x}-{2:x}({3:x})[{4};{5}:{6}]", (ulong)AllocAddress, (ulong)LiveAddress,
                            (ulong)((byte*)LiveAddress + LiveSize), (ulong)((byte*)AllocAddress + AllocSize),
                            AllocMemberName, AllocSourceFilePath, AllocSourceLineNumber);
                    return string.Format("{0:x}-{1:x}[{2};{3}:{4}]", (ulong)LiveAddress, (ulong)((byte*)LiveAddress + LiveSize),
                        AllocMemberName, AllocSourceFilePath, AllocSourceLineNumber);
                }

                if (AllocAddress != LiveAddress || AllocSize != LiveSize)
                    return string.Format("({0:x}){1:x}-{2:x}({3:x})[{4};{5}:{6}]->[{7};{8}:{9}]", (ulong)LiveAddress, (ulong)((byte*)LiveAddress + LiveSize),
                        (ulong)((byte*)LiveAddress + LiveSize), (ulong)((byte*)AllocAddress + AllocSize),
                        AllocMemberName, AllocSourceFilePath, AllocSourceLineNumber, DeallocMemberName,
                        DeallocSourceFilePath, DeallocSourceLineNumber);
                return string.Format("{0:x}-{1:x}[{2};{3}:{4}]->[{5};{6}:{7}]", (ulong)LiveAddress, (ulong)((byte*)LiveAddress + LiveSize),
                    AllocMemberName, AllocSourceFilePath, AllocSourceLineNumber, DeallocMemberName,
                    DeallocSourceFilePath, DeallocSourceLineNumber);
            }
        }

    }
}