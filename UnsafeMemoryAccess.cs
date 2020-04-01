// Copyright (c) SharpCrafters s.r.o. This file is not open source. It is released under a commercial
// source-available license. Please see the LICENSE.md file in the repository root for details.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PostSharp.Community.UnsafeMemoryChecker;

namespace PostSharp.Community.UnsafeMemoryChecker
{
    /// <summary>
    /// If enabled, checks memory accesses done via pointers in unsafe code and throws an exception if access to
    /// uncontrolled memory is attempted. Define the debug symbol CHECK_UNSAFE_MEMORY and and
    /// </summary>
    internal static unsafe class UnsafeMemoryAccess
    {
        public static int GetAllocSize( int size )
        {
            return DelegatedUnsafeMemoryAccess.GetAllocSize(size);
        }

        public static int GetLiveSize( int size )
        {
            return DelegatedUnsafeMemoryAccess.GetLiveSize(size);
        }

        public static int GetLiveOffset()
        {
            return DelegatedUnsafeMemoryAccess.GetLiveOffset();
        }

        public static IntPtr GetLiveAddress( IntPtr allocAddress )
        {
            return DelegatedUnsafeMemoryAccess.GetLiveAddress(allocAddress);
        }

        [Conditional( "CHECK_UNSAFE_MEMORY" )]
        public static void AddSafeSegment( void* allocAddress, int allocSize,
                                           [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                                           [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                                           [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0 )
        {
            DelegatedUnsafeMemoryAccess.AddSafeSegment(allocAddress, allocSize, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Marks a memory segment as being safe to write into.
        /// </summary>
        /// <param name="allocAddress">The address where the memory segment begins.</param>
        /// <param name="allocSize">The size of the contiguous memory segment, in bytes.</param>
        /// <param name="liveAddress"></param>
        /// <param name="liveSize"></param>
        /// <param name="memberName"></param>
        /// <param name="sourceFilePath"></param>
        /// <param name="sourceLineNumber"></param>
        [Conditional( "CHECK_UNSAFE_MEMORY" )]
        public static void AddSafeSegment( void* allocAddress, int allocSize, void* liveAddress, int liveSize,
                                           [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                                           [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                                           [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0 )
        {
            AddSafeSegment( (IntPtr) allocAddress, allocSize, (IntPtr) liveAddress, liveSize, memberName, sourceFilePath, sourceLineNumber );
        }

        [Conditional( "CHECK_UNSAFE_MEMORY" )]
        public static void AddSafeSegment( IntPtr allocAddress, int allocSize,
                                           [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                                           [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                                           [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0 )
        {
            AddSafeSegment( (IntPtr) allocAddress, allocSize, allocAddress, allocSize, memberName, sourceFilePath, sourceLineNumber );
        }

        [Conditional( "CHECK_UNSAFE_MEMORY" )]
        public static void AddSafeSegment( IntPtr allocAddress, int allocSize, IntPtr liveAddress, int liveSize,
                                           [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                                           [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                                           [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0 )
        {
            DelegatedUnsafeMemoryAccess.AddSafeSegment(allocAddress, allocSize, liveAddress, liveSize, memberName, sourceFilePath, sourceLineNumber);
        }

        [Conditional( "CHECK_UNSAFE_MEMORY" )]
        public static void RemoveSafeSegment( void* address,
                                              [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                                              [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                                              [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0 )
        {
            RemoveSafeSegment( (IntPtr) address, memberName, sourceFilePath, sourceLineNumber );
        }

        [Conditional( "CHECK_UNSAFE_MEMORY" )]
        public static void RemoveSafeSegment( IntPtr address,
                                              [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                                              [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                                              [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0 )
        {
            DelegatedUnsafeMemoryAccess.RemoveSafeSegment(address, memberName, sourceFilePath, sourceLineNumber);
        }

        [Conditional( "CHECK_UNSAFE_MEMORY" )]
        public static void CheckAddress( IntPtr address, int size )
        {
            CheckAddress( (void*)address, size );
        }

        [Conditional( "CHECK_UNSAFE_MEMORY" )]
        public static void CheckAddress( void* address, int size )
        {
            DelegatedUnsafeMemoryAccess.CheckAddress(address, size);
        }

        public static void StoreByte( byte* address, byte value )
        {
            CheckAddress( address, sizeof(byte) );
            *address = value;
        }

        public static void StoreInt32( int* address, int value )
        {
            CheckAddress( address, sizeof(int) );
            *address = value;
        }

        public static void StoreInt16( short* address, short value )
        {
            CheckAddress( address, sizeof(short) );
            *address = value;
        }

        public static void StoreInt64( long* address, long value )
        {
            CheckAddress( address, sizeof(long) );
            *address = value;
        }

        public static void StoreIntPtr( IntPtr* address, IntPtr value )
        {
            CheckAddress( address, sizeof(IntPtr) );
            *address = value;
        }

        public static void StoreSingle( float* address, float value )
        {
            CheckAddress( address, sizeof(float) );
            *address = value;
        }

        public static void StoreDouble( double* address, double value )
        {
            CheckAddress( address, sizeof(double) );
            *address = value;
        }
    }
}