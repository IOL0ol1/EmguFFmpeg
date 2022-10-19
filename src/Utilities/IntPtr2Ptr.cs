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
        /// get pointer to pointer (&amp;ptr).
        /// </summary>
        private void** _ptr2ptr;

        /// <summary>
        /// create pointer to <paramref name="ptr"/>.
        /// </summary>
        /// <param name="ptr"></param>
        public IntPtr2Ptr(IntPtr ptr)
        {
            _ptr = (void*)ptr;
            fixed (void** pptr = &_ptr)
            {
                _ptr2ptr = pptr;
            }
        }

        /// <summary>
        /// create a pointer to <see langword="null"/>.
        /// </summary>
        /// <returns></returns>
        public static IntPtr2Ptr Ptr2Null => new IntPtr2Ptr(IntPtr.Zero);

        public static implicit operator void**(IntPtr2Ptr ptr2Ptr)
        {
            if (ptr2Ptr == null) return null;
            return ptr2Ptr._ptr2ptr;
        }
    }


}
