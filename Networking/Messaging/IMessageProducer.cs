using Library.Collections;
using Library.Types;
using Library.Util;

namespace Networking.Messaging;

public interface IMessageProducer<T>
{
    Result Send(T message, Session target);

    Result Send(T message, IFilter<Session> targetFilter);
}
