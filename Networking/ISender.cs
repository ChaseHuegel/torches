using Library.Collections;
using Library.Types;
using Library.Util;

namespace Networking;

public interface ISender<T>
{
    Result Send(T message);

    Result Send(T message, Session target);

    Result Send(T message, IFilter<Session> targetFilter);
}
