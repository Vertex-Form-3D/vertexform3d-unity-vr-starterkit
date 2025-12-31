using System;

namespace Photon.Voice
{
    /// <summary>
    /// Object factory with optional info useful in combination with  <see cref="ObjectPool{TType, TInfo}"/>.
    /// </summary>
    /// <typeparam name="TType">Object type.</typeparam>
    /// <typeparam name="TInfo">Type of property used to check 2 objects identity (like integral length of array).</typeparam>
    public interface ObjectFactory<TType, TInfo> : IDisposable
    {
        TType New();
        TType New(TInfo info);
        bool Free(TType obj);
        bool Free(TType obj, TInfo info);
    }
}
