using System;

namespace FFmpegSharp
{
    /// <summary>
    /// pointer to pointer (void**)
    /// </summary>
    public unsafe class IntPtr2Ptr
    {
        private void* _ptr;

        /// <summary>
        /// create pointer to <paramref name="ptr"/>.
        /// </summary>
        /// <param name="ptr"></param>
        public IntPtr2Ptr(IntPtr ptr)
        {
            _ptr = (void*)ptr;
            fixed (void** pptr = &_ptr)
            {
                Ptr2Ptr = (IntPtr)pptr;
            }
        }

        /// <summary>
        /// create a pointer to <see langword="null"/>.
        /// </summary>
        /// <returns></returns>
        public static IntPtr2Ptr Ptr2Null => new IntPtr2Ptr(IntPtr.Zero);

        /// <summary>
        /// get pointer (ptr).
        /// </summary>
        public IntPtr Ptr => (IntPtr)_ptr;

        /// <summary>
        /// get pointer to pointer (&amp;ptr).
        /// </summary>
        public IntPtr Ptr2Ptr { get; private set; }

        public static implicit operator IntPtr(IntPtr2Ptr ptr2ptr)
        {
            if (ptr2ptr == null) return IntPtr.Zero;
            return ptr2ptr.Ptr2Ptr;
        }

        public static implicit operator void**(IntPtr2Ptr ptr2Ptr)
        {
            if (ptr2Ptr == null) return null;
            return (void**)ptr2Ptr.Ptr2Ptr;
        }
    }


}
