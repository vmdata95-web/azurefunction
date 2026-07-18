using System;

namespace Application.Common.Interfaces
{
    public interface ITimeProvider
    {
        DateTime GetLocalTime();
    }
}
