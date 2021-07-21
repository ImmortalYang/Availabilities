using System;
using System.Collections.Generic;
using System.Linq;
using Availabilities.Apis.Validators;
using Availabilities.Other;
using Availabilities.Resources;
using ServiceStack;

namespace Availabilities.Apis.Application
{
    public class AvailabilitiesEditor : IAvailabilitiesEditor
    {

        public Instructions Reserve(TimeSlot slot, IList<Availability> availabilities)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }
            
            if (availabilities == null)
            {
                throw new ArgumentNullException(nameof(availabilities));
            }

            if (availabilities.Count == 0)
            {
                throw new ResourceNotFoundException();
            }
            
            var actualSlot = new TimeSlot(slot.Start.ToNextOrCurrentQuarterHour(), slot.End.ToNextOrCurrentQuarterHour());
            
            if (actualSlot.Start < Validations.Availabilities.MinimumAvailability || actualSlot.End > Validations.Availabilities.MaximumAvailability)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            var containingAvailability = availabilities.FirstOrDefault(av => av.StartUtc <= actualSlot.Start && av.EndUtc >= actualSlot.End);

            if (containingAvailability == null)
            {
                throw new ResourceConflictException();
            }
            
            var instructions = new Instructions()
            {
                ActualSlot = actualSlot,
                UpsertAvailabilities = new List<Availability>(),
                DeleteAvailabilities = new List<Availability>()
            };

            if (actualSlot.Start == containingAvailability.StartUtc && actualSlot.End < containingAvailability.EndUtc)
            {
                containingAvailability.StartUtc = actualSlot.End;
                instructions.UpsertAvailabilities.Add(containingAvailability);
                return instructions;
            }
            
            if (actualSlot.Start > containingAvailability.StartUtc &&
                     actualSlot.End == containingAvailability.EndUtc)
            {
                containingAvailability.EndUtc = slot.Start;
                instructions.UpsertAvailabilities.Add(containingAvailability);
                return instructions;
            }
            
            if (actualSlot.Start > containingAvailability.StartUtc &&
                     actualSlot.End < containingAvailability.EndUtc)
            {
                instructions.UpsertAvailabilities.Add(new Availability()
                {
                    Id = containingAvailability.Id,
                    StartUtc = containingAvailability.StartUtc,
                    EndUtc = slot.Start
                });

                instructions.UpsertAvailabilities.Add(new Availability()
                {
                    StartUtc = actualSlot.End,
                    EndUtc = containingAvailability.EndUtc
                });
                
                return instructions;
            }
            
            if (actualSlot.Start == containingAvailability.StartUtc && actualSlot.End == containingAvailability.EndUtc)
            {
                instructions.DeleteAvailabilities.Add(containingAvailability);
                return instructions;
            }

            return instructions;
        }

        public Instructions Release(TimeSlot slot, IList<Availability> availabilities)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }
            
            if (availabilities == null)
            {
                throw new ArgumentNullException(nameof(availabilities));
            }

            if (availabilities.Count == 0)
            {
                throw new ResourceNotFoundException();
            }
            
            var actualSlot = new TimeSlot(slot.Start, slot.End);
            
            if (actualSlot.Start < Validations.Availabilities.MinimumAvailability || actualSlot.End > Validations.Availabilities.MaximumAvailability)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            var instructions = new Instructions()
            {
                ActualSlot = actualSlot,
                UpsertAvailabilities = new List<Availability>(),
                DeleteAvailabilities = new List<Availability>()
            };
            
            var containingAvailability = availabilities.FirstOrDefault(av => av.StartUtc <= actualSlot.Start && av.EndUtc >= actualSlot.End);

            if (containingAvailability != null)
            {
                return instructions;
            }

            var leftOverlappingAvailability =
                availabilities.FirstOrDefault(av => av.StartUtc < slot.Start && av.EndUtc >= slot.Start);
            var rightOverlappingAvailability =
                availabilities.FirstOrDefault(av => av.EndUtc > slot.End && av.StartUtc <= slot.End);

            if (leftOverlappingAvailability == null && rightOverlappingAvailability == null)
            {
                instructions.UpsertAvailabilities.Add(new Availability()
                {
                    StartUtc = slot.Start,
                    EndUtc = slot.End
                });
                return instructions;
            }

            if (leftOverlappingAvailability == null)
            {
                rightOverlappingAvailability.StartUtc = slot.Start;
                instructions.UpsertAvailabilities.Add(rightOverlappingAvailability);
                return instructions;
            }

            if (rightOverlappingAvailability == null)
            {
                leftOverlappingAvailability.EndUtc = slot.End;
                instructions.UpsertAvailabilities.Add(leftOverlappingAvailability);
                return instructions;
            }
            
            // left is not null and right is not null
            leftOverlappingAvailability.EndUtc = rightOverlappingAvailability.EndUtc;
            instructions.DeleteAvailabilities.Add(rightOverlappingAvailability);
            instructions.UpsertAvailabilities.Add(leftOverlappingAvailability);

            return instructions;
        }
    }
}