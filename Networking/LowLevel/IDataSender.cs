using Library.Collections;
using Library.Types;
using Library.Util;

namespace Networking.LowLevel;

public interface IDataSender
{
    Result Send(byte[] data, Session target);

    Result Send(byte[] data, IFilter<Session> targetFilter);
}