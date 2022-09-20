using System;
using System.Runtime.InteropServices;

namespace Tuntenfisch.Lighting2D.Common
{
    public struct ManagedObject<T> : IDisposable
    {
        #region Public Properties
        public T Object
        {
            get
            {
                return (T)m_handle.Target;
            }

            set
            {
                if (m_handle.IsAllocated)
                {
                    m_handle.Free();
                }
                m_handle = GCHandle.Alloc(value);
            }
        }
        #endregion

        #region Private Fields
        private GCHandle m_handle;
        #endregion

        #region Public Methods
        public void Dispose()
        {
            if (m_handle.IsAllocated)
            {
                m_handle.Free();
            }
        }
        #endregion
    }
}