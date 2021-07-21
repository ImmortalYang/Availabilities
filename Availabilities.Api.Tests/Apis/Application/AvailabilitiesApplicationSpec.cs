using System;
using Availabilities.Apis.Application;
using Availabilities.Resources;
using Availabilities.Storage;
using FluentAssertions;
using Moq;
using Xunit;

namespace Availabilities.Api.Tests.Apis.Application
{
    [Trait("Category", "Unit")]
    public class AvailabilitiesApplicationSpec
    {
        private Mock<IStorage<Availability>> storage;
        private Mock<IAvailabilitiesEditor> editor;
        private IAvailabilitiesApplication application;
        
        public AvailabilitiesApplicationSpec()
        {
            this.storage = new Mock<IStorage<Availability>>();
            this.editor = new Mock<IAvailabilitiesEditor>();
            this.application = new AvailabilitiesApplication(this.storage.Object, this.editor.Object);
        }

        [Fact]
        public void WhenReserveAvailabilityAndSlotIsNull_ThenThrows()
        {
            this.application
                .Invoking(x => x.ReserveAvailability(null))
                .Should().Throw<ArgumentNullException>();
        }

        // [Fact]
        // public void WhenReserveAvailability_TheReturnInstructions()
        // {
        //     var slot = new TimeSlot(DateTime.UtcNow, 1);
        //     var instructions = new Instructions()
        //     {
        //         ActualSlot = slot,
        //         UpsertAvailabilities = new Availability[] {},
        //         DeleteAvailabilities = new Availability[] {}
        //     };
        //     var availabilities = this.storage.Object.List();
        //     this.editor.Setup(e => e.Reserve(slot, availabilities))
        //         .Returns(instructions);
        //
        //     var result = this.application.ReserveAvailability(slot);
        //     result.Should().Be(slot);
        //     this.editor.VerifyAll();
        // }
    }
}