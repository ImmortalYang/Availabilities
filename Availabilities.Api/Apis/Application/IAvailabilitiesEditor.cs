using System.Collections.Generic;
using Availabilities.Resources;

namespace Availabilities.Apis.Application
{
    public interface IAvailabilitiesEditor
    {
        Instructions Reserve(TimeSlot slot, IList<Availability> availabilities);
        Instructions Release(TimeSlot slot, IList<Availability> availabilities);
    }
}