## ![](icon.png) &nbsp; PostSharp.Community.UnsafeMemoryChecker 
Throws an exception if your unsafe code attempts to write to memory you don't control.

**Your concern:** You're using unsafe code and pointer arithmetic and it could happen to you that your code writes to memory that you're not supposed to write into. If you're lucky, this will trigger a memory access violation and crash. But it could also happen that you will only notice the error much later.

**This add-in's help:** With this add-in, you're only allowed to write to memory segments that you designate "safe". An attempt to write to memory other than safe segments throws an exception at the point of writing.

*This is an add-in for [PostSharp](https://postsharp.net). It modifies your assembly during compilation by using IL weaving.*

![CI badge](https://github.com/postsharp/PostSharp.Community.UnsafeMemoryChecker/workflows/Full%20Pipeline/badge.svg)

#### Example
##### Writing into uncontrolled memory:
Your code:
```csharp
[assembly: CheckUnsafeMemory] // + build symbol: CHECK_UNSAFE_MEMORY
int number = 42;
int* pointer = &number;
pointer += 400;
*pointer = 84; // <- This could be a problem...
```
What gets compiled:
```csharp
int number = 42;
int* pointer = &number;
pointer += 400;
UnsafeMemoryAccess.StoreInt32(pointer, 84); // <- throws AccessViolationException
```
##### Writing into safe memory:

Your code:
```csharp
[assembly: CheckUnsafeMemory] // + build symbol: CHECK_UNSAFE_MEMORY
int[] numbers = new int[40];
fixed (int* item = &numbers[0])
{
    UnsafeMemoryAccess.AddSafeSegment(item, 40*sizeof(int)); // mark the array as safe memory
    int* mid = item + 20;
    *mid = 42;
    Console.WriteLine(numbers[20]); // writes 42
}
```
What gets compiled:
```csharp
int[] numbers = new int[40];
fixed (int* item = &numbers[0])
{
    UnsafeMemoryAccess.AddSafeSegment(item, 40*sizeof(int));
    int* mid = item + 20;
    UnsafeMemoryAccess.StoreInt32(mid, 42); // ok
    Console.WriteLine(numbers[20]); 
}
```
#### Installation 
1. Install the NuGet package: `PM> Install-Package PostSharp.Community.UnsafeMemoryChecker`
2. Get a free PostSharp Community license at https://www.postsharp.net/get/free
3. When you compile for the first time, you'll be asked to enter the license key.

Instrumentation with UnsafeMemoryChecker, if enabled, is going to reduce your runtime performance. Therefore,
you should only add it when you want to check for unsafe memory accesses, not in a production build. 

The checks will happen if you define the build symbol CHECK_UNSAFE_MEMORY. 

The instrumentation will happen if you add `[assembly: CheckUnsafeMemory]` to your assembly.

We recommend that you add:
```csharp
#if CHECK_UNSAFE_MEMORY
[assembly: CheckUnsafeMemory]
#endif
```
to your assembly, and then define the build symbol CHECK_UNSAFE_MEMORY when you want this add-in to have an effect.
#### How to use

1. Before you access memory using a pointer, you must mark that memory segment as safe using 
`UnsafeMemoryAccess.AddSafeSegment`.
2. When that memory is no longer safe to write into, unmark it with `UnsafeMemoryAccess.RemoveSafeSegment`.

#### Copyright notices
Published under the MIT license.

* Copyright © PostSharp Technologies
* Icon by <a href="https://www.flaticon.com/authors/vectors-market" title="Vectors Market">Vectors Market</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a>.