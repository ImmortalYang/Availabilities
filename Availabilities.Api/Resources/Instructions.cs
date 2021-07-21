using System.Collections.Generic;

namespace Availabilities.Resources
{
    public class Instructions
    {
        public TimeSlot ActualSlot;
        public IList<Availability> UpsertAvailabilities;
        public IList<Availability> DeleteAvailabilities;
    }
}