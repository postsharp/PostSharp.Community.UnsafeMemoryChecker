using System;
using PostSharp.Community.UnsafeMemoryChecker;
using Xunit;

#if CHECK_UNSAFE_MEMORY
[assembly: CheckUnsafeMemory]
#endif

namespace PostSharp.Community.UnsafeMemoryChecker.Tests
{
    public unsafe class MainTests
    {
        [Fact]
        public void CanAccessSafeSegment()
        {
            int[] numbers = new int[40];
            fixed (int* item = &numbers[0])
            {
                UnsafeMemoryAccess.AddSafeSegment(item, 40*sizeof(int)); // mark the array as safe memory
                int* mid = item + 20;
                *mid = 42;
                Assert.Equal(42, numbers[20]);
                UnsafeMemoryAccess.RemoveSafeSegment(item);
            }
        }
        
        [Fact]
        public void CannotAccessUnsafeSegment()
        {
            int[] numbers = new int[40];
            fixed (int* item = &numbers[0])
            {
                UnsafeMemoryAccess.AddSafeSegment(item, 40*sizeof(int)); // mark the array as safe memory
                int* mid = item + 42;
                Assert.Throws<AccessViolationException>(() =>
                {
                    *mid = 42;
                });
                UnsafeMemoryAccess.RemoveSafeSegment(item);
            }
        }
    }
}