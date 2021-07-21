using System;
using System.Collections.Generic;
using Availabilities.Apis.Application;
using Availabilities.Apis.Validators;
using Availabilities.Other;
using Availabilities.Resources;
using FluentAssertions;
using Moq;
using Xunit;

namespace Availabilities.Api.Tests.Apis.Application
{
    [Trait("Category", "Unit")]
    public class AvailabilitiesEditorSpec
    {
        private readonly IAvailabilitiesEditor editor;

        private readonly TimeSlot aQuarterFromNow = new TimeSlot(DateTime.UtcNow, TimeSpan.FromMinutes(15));

        private readonly Availability initialAvailability;

        private readonly IList<Availability> initialAvailabilities;
        
        public AvailabilitiesEditorSpec()
        {
            this.editor = new AvailabilitiesEditor();
            this.initialAvailability = new Availability
            {
                StartUtc = Validations.Availabilities.MinimumAvailability,
                EndUtc = Validations.Availabilities.MaximumAvailability
            };
            this.initialAvailabilities = new List<Availability>() {this.initialAvailability};
        }

        #region ReserveSpec
        [Fact]
        public void WhenReserveTimeslotIsNull_TheThrows()
        {
            this.editor.Invoking(x => x.Reserve(null, this.initialAvailabilities))
                .Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void WhenReserveAvailabilitiesIsNull_TheThrows()
        {
            this.editor.Invoking(x => x.Reserve(this.aQuarterFromNow, null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WhenReserveNoAvailability_TheThrows()
        {
            this.editor.Invoking(x => x.Reserve(this.aQuarterFromNow, new List<Availability>()))
                .Should().Throw<ResourceNotFoundException>();
        }

        [Fact]
        public void WhenReserveOutOfRange_ThenThrows()
        {
            var leftOverflowSlot = new TimeSlot(Validations.Availabilities.MinimumAvailability.AddMinutes(-1 * Validations.Bookings.MinimumBookingLengthInMinutes), 1);
            
            this.editor.Invoking(x => x.Reserve(leftOverflowSlot, this.initialAvailabilities))
                .Should().Throw<ArgumentOutOfRangeException>();

            var rightOverflowSlot = new TimeSlot(Validations.Availabilities.MaximumAvailability, 1);
            this.editor.Invoking(x => x.Reserve(rightOverflowSlot, this.initialAvailabilities))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void WhenNoContainingAvailability_TheThrows()
        {
            var startOfDay = DateTime.Now.Date;
            var slot = new TimeSlot(startOfDay, TimeSpan.FromDays(1));

            var availabilities = new []
            {
                new Availability()
                {
                    StartUtc = startOfDay.AddDays(-1),
                    EndUtc = startOfDay
                },
                new Availability()
                {
                    StartUtc = startOfDay.AddDays(1),
                    EndUtc = startOfDay.AddDays(2)
                }
            };

            this.editor.Invoking(x => x.Reserve(slot, availabilities))
                .Should().Throw<ResourceConflictException>();
        }

        [Fact]
        public void WhenSlotIsAvailable_TheSlotIsReturned()
        {
            var slot = new TimeSlot(DateTime.Now.Date, TimeSpan.FromDays(1));

            var instructions = this.editor.Reserve(slot, this.initialAvailabilities);
            Assert.Equal(instructions.ActualSlot.Start, slot.Start);
            Assert.Equal(instructions.ActualSlot.End, slot.End);
        }

        [Fact]
        public void WhenAvailabilityHasRightPaddingToSlot_TheUpsertInstructionReturned()
        {
            var slot = new TimeSlot(this.initialAvailability.StartUtc, TimeSpan.FromHours(1));

            var instructions = this.editor.Reserve(slot, this.initialAvailabilities);
            Assert.Empty(instructions.DeleteAvailabilities);
            Assert.Equal(1, instructions.UpsertAvailabilities.Count);
            Assert.Equal(slot.End, instructions.UpsertAvailabilities[0].StartUtc);
            Assert.Equal(this.initialAvailabilities[0].EndUtc, instructions.UpsertAvailabilities[0].EndUtc);
        }

        [Fact]
        public void WhenAvailabilityHasLeftPaddingToSLot_TheUpsertInstructionReturned()
        {
            var slot = new TimeSlot(this.initialAvailability.EndUtc.AddDays(-1), this.initialAvailability.EndUtc);

            var instructions = this.editor.Reserve(slot, this.initialAvailabilities);
            Assert.Empty(instructions.DeleteAvailabilities);
            Assert.Equal(1, instructions.UpsertAvailabilities.Count);
            Assert.Equal(this.initialAvailability.StartUtc, instructions.UpsertAvailabilities[0].StartUtc);
            Assert.Equal(slot.Start, instructions.UpsertAvailabilities[0].EndUtc);
        }

        [Fact]
        public void WhenAvailabilityHasLeftAndRightPaddingToSlot_TheUpsertAndDeleteInstructionsReturned()
        {
            var slot = new TimeSlot(DateTime.UtcNow.Date, TimeSpan.FromMinutes(30));

            var instructions = this.editor.Reserve(slot, this.initialAvailabilities);
            Assert.Empty(instructions.DeleteAvailabilities);
            Assert.Equal(2, instructions.UpsertAvailabilities.Count);
            Assert.Equal(this.initialAvailability.StartUtc, instructions.UpsertAvailabilities[0].StartUtc);
            Assert.Equal(slot.Start, instructions.UpsertAvailabilities[0].EndUtc);
            Assert.Equal(slot.End, instructions.UpsertAvailabilities[1].StartUtc);
            Assert.Equal(this.initialAvailability.EndUtc, instructions.UpsertAvailabilities[1].EndUtc);
        }

        [Fact]
        public void WhenAvailabilityHasNoPaddingToSlot_TheDeleteInstructionReturned()
        {
            var slot = new TimeSlot(this.initialAvailability.StartUtc, this.initialAvailability.EndUtc);

            var instructions = this.editor.Reserve(slot, this.initialAvailabilities);
            Assert.Empty(instructions.UpsertAvailabilities);
            Assert.Equal(1, instructions.DeleteAvailabilities.Count);
            Assert.Equal(this.initialAvailability, instructions.DeleteAvailabilities[0]);
        }
        #endregion

        #region ReleaseSpec
        [Fact]
        public void WhenReleaseTimeslotIsNull_TheThrows()
        {
            this.editor.Invoking(x => x.Release(null, this.initialAvailabilities))
                .Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void WhenReleaseAvailabilitiesIsNull_TheThrows()
        {
            this.editor.Invoking(x => x.Release(this.aQuarterFromNow, null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WhenReleaseNoAvailability_TheThrows()
        {
            this.editor.Invoking(x => x.Release(this.aQuarterFromNow, new List<Availability>()))
                .Should().Throw<ResourceNotFoundException>();
        }
        
        [Fact]
        public void WhenReleaseOutOfRange_ThenThrows()
        {
            var leftOverflowSlot = new TimeSlot(Validations.Availabilities.MinimumAvailability.AddMinutes(-1), 1);
            
            this.editor.Invoking(x => x.Release(leftOverflowSlot, this.initialAvailabilities))
                .Should().Throw<ArgumentOutOfRangeException>();

            var rightOverflowSlot = new TimeSlot(Validations.Availabilities.MaximumAvailability, 1);
            this.editor.Invoking(x => x.Release(rightOverflowSlot, this.initialAvailabilities))
                .Should().Throw<ArgumentOutOfRangeException>();
        }
        
        [Fact]
        public void WhenHasContainingAvailability_TheNothingReleased()
        {
            var slot = new TimeSlot(DateTime.UtcNow.Date, TimeSpan.FromHours(1));

            var instructions = this.editor.Release(slot, this.initialAvailabilities);
            Assert.Empty(instructions.DeleteAvailabilities);
            Assert.Empty(instructions.UpsertAvailabilities);
        }

        [Fact]
        public void WhenNoOverlappingAvailabilities_TheOneSlotIsReleased()
        {
            var startOfDay = DateTime.UtcNow.Date;
            var leftAvailability = new Availability()
            {
                StartUtc = startOfDay.AddDays(-2),
                EndUtc = startOfDay.AddDays(-1)
            };
            
            var rightAvailability = new Availability()
            {
                StartUtc = startOfDay.AddDays(1),
                EndUtc = startOfDay.AddDays(2)
            };
            
            var slot = new TimeSlot(startOfDay, TimeSpan.FromHours(1));

            var instructions = this.editor.Release(slot, new [] { leftAvailability, rightAvailability });
            Assert.Equal(1, instructions.UpsertAvailabilities.Count);
            Assert.Empty(instructions.DeleteAvailabilities);
            Assert.Equal(slot.Start, instructions.UpsertAvailabilities[0].StartUtc);
            Assert.Equal(slot.End, instructions.UpsertAvailabilities[0].EndUtc);
        }

        [Fact]
        public void WhenNoLeftOverlappingAvailabilityAndHasRightOverlappingAvailability_TheRightOverlappingAvailabilityIsExtendedToSlotStart()
        {
            var startOfDay = DateTime.UtcNow.Date;
            
            var leftAvailability = new Availability()
            {
                StartUtc = startOfDay.AddDays(-2),
                EndUtc = startOfDay.AddDays(-1)
            };

            var rightAvailability = new Availability()
            {
                StartUtc = startOfDay.AddDays(1),
                EndUtc = startOfDay.AddDays(2)
            };
            
            var slot = new TimeSlot(startOfDay, TimeSpan.FromDays(1));

            var instructions = this.editor.Release(slot, new[] {leftAvailability, rightAvailability});
            Assert.Equal(1, instructions.UpsertAvailabilities.Count);
            Assert.Empty(instructions.DeleteAvailabilities);
            Assert.Equal(slot.Start, instructions.UpsertAvailabilities[0].StartUtc);
            Assert.Equal(rightAvailability.EndUtc, instructions.UpsertAvailabilities[0].EndUtc);
        }

        [Fact]
        public void WhenNoRightOverlappingAvailabilityAndHasLeftOverlappingAvailability_TheLeftOverlappingAvailabilityIsExtendedToSlotEnd()
        {
            var startOfDay = DateTime.UtcNow.Date;
            
            var leftAvailability = new Availability()
            {
                StartUtc = startOfDay.AddDays(-1),
                EndUtc = startOfDay
            };

            var rightAvailability = new Availability()
            {
                StartUtc = startOfDay.AddDays(1),
                EndUtc = startOfDay.AddDays(2)
            };
            
            var slot = new TimeSlot(startOfDay, TimeSpan.FromHours(1));
            
            var instructions = this.editor.Release(slot, new[] {leftAvailability, rightAvailability});
            Assert.Equal(1, instructions.UpsertAvailabilities.Count);
            Assert.Empty(instructions.DeleteAvailabilities);
            Assert.Equal(leftAvailability.StartUtc, instructions.UpsertAvailabilities[0].StartUtc);
            Assert.Equal(slot.End, instructions.UpsertAvailabilities[0].EndUtc);
        }

        [Fact]
        public void WhenHasLeftAndRightOverlappingAvailabilities_TheMergedIntoOneAvailability()
        {
            var startOfDay = DateTime.UtcNow.Date;
            
            var leftAvailability = new Availability()
            {
                StartUtc = startOfDay.AddDays(-1),
                EndUtc = startOfDay
            };

            var rightAvailability = new Availability()
            {
                StartUtc = startOfDay.AddDays(1),
                EndUtc = startOfDay.AddDays(2)
            };
            
            var slot = new TimeSlot(startOfDay, startOfDay.AddDays(1));
            
            var instructions = this.editor.Release(slot, new[] {leftAvailability, rightAvailability});
            Assert.Equal(1, instructions.UpsertAvailabilities.Count);
            Assert.Equal(1, instructions.DeleteAvailabilities.Count);
            Assert.Contains(rightAvailability, instructions.DeleteAvailabilities);
            Assert.Equal(leftAvailability.StartUtc, instructions.UpsertAvailabilities[0].StartUtc);
            Assert.Equal(rightAvailability.EndUtc, instructions.UpsertAvailabilities[0].EndUtc);
        }

        #endregion
    }
    
}