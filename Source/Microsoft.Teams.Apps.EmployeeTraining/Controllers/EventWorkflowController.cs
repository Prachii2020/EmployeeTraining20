// <copyright file="EventWorkflowController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.EmployeeTraining.Controllers
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.EmployeeTraining.Authentication;
    using Microsoft.Teams.Apps.EmployeeTraining.Helpers;
    using Microsoft.Teams.Apps.EmployeeTraining.Models;

    /// <summary>
    /// Exposes APIs related to event operations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(PolicyNames.MustBeLnDTeamMemberPolicy)]
    public class EventWorkflowController : BaseController
    {
        /// <summary>
        /// Logs errors and information
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Helper methods for CRUD operations on event.
        /// </summary>
        private readonly IEventWorkflowHelper eventWorkflowHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventWorkflowController"/> class.
        /// </summary>
        /// <param name="logger">The ILogger object which logs errors and information</param>
        /// <param name="telemetryClient">The Application Insights telemetry client</param>
        /// <param name="eventWorkflowHelper">Helper methods for CRUD operations on event.</param>
        public EventWorkflowController(
            ILogger<EventController> logger,
            TelemetryClient telemetryClient,
            IEventWorkflowHelper eventWorkflowHelper)
            : base(telemetryClient)
        {
            this.logger = logger;
            this.eventWorkflowHelper = eventWorkflowHelper;
        }

        /// <summary>
        /// Save new event as draft.
        /// </summary>
        /// <param name="eventEntity">Event details entered by user.</param>
        /// <param name="teamId">Team Id for which event will be created.</param>
        /// <returns>Boolean indicating insert operation result.</returns>
        [HttpPost("create-draft")]
        public async Task<IActionResult> CreateDraftAsync([FromBody] EventEntity eventEntity, [FromQuery]string teamId)
        {
            this.RecordEvent("Create draft- The HTTP POST call to create draft has been initiated");

            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("Team Id is either null or empty");
                this.RecordEvent("Create draft- The HTTP POST call to create draft has been failed");
                return this.BadRequest(new { message = "Team Id is either null or empty" });
            }

#pragma warning disable CA1062 // Null check is handled by data annotations at model level
            eventEntity.CreatedBy = this.UserAadId;
#pragma warning restore CA1062 // Null check is handled by data annotations at model level
            eventEntity.UpdatedBy = this.UserAadId;
            eventEntity.CreatedOn = DateTime.UtcNow;
            eventEntity.UpdatedOn = DateTime.UtcNow;
            eventEntity.TeamId = teamId;

            try
            {
                var result = await this.eventWorkflowHelper.CreateDraftEventAsync(eventEntity);

                if (!result)
                {
                    this.RecordEvent("Create draft- The HTTP POST call to create draft has been failed");
                    this.logger.LogInformation($"Failed to create draft event for user {this.UserAadId} and team {teamId}");
                }

                this.RecordEvent("Create draft- The HTTP POST call to create draft has been succeeded");

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this.RecordEvent("Create draft- The HTTP POST call to create draft has been failed");
                this.logger.LogError(ex, $"Error occurred while creating event draft for user {this.UserAadId} and team {teamId}");
                throw;
            }
        }

        /// <summary>
        /// Update draft event details.
        /// </summary>
        /// <param name="eventEntity">Event details entered by user.</param>
        /// <param name="teamId">Team Id for which event will be created.</param>
        /// <returns>Boolean indicating update operation result.</returns>
        [HttpPatch("update-draft")]
        public async Task<IActionResult> UpdateDraftAsync([FromBody] EventEntity eventEntity, [FromQuery] string teamId)
        {
            this.RecordEvent("Update draft- The HTTP PATCH call to update draft has been initiated");

            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("Team Id is either null or empty");
                this.RecordEvent("Update draft- The HTTP PATCH call to update draft has been failed");
                return this.BadRequest(new { message = "Team Id is either null or empty" });
            }

#pragma warning disable CA1062 // Null check is handled by data annotations at model level
            if (string.IsNullOrEmpty(eventEntity.EventId))
#pragma warning restore CA1062 // Null check is handled by data annotations at model level
            {
                this.logger.LogError("Event Id is null or empty");
                this.RecordEvent("Update draft- The HTTP PATCH call to update draft has been failed");
                return this.BadRequest(new { message = "Event Id is null or empty" });
            }

            eventEntity.TeamId = teamId;
            eventEntity.UpdatedBy = this.UserAadId;

            try
            {
                var updateResult = await this.eventWorkflowHelper.UpdateDraftEventAsync(eventEntity);

                if (updateResult == null)
                {
                    this.RecordEvent("Update draft- The HTTP PATCH call to update draft has been failed");
                    this.logger.LogError($"Event {eventEntity.EventId} not found for team {teamId}");
                    return this.NotFound(new { message = $"Event {eventEntity.EventId} not found for team {teamId}" });
                }

                if (!(bool)updateResult)
                {
                    this.RecordEvent("Update draft- The HTTP PATCH call to update draft has been failed");
                    this.logger.LogInformation($"Failed to update draft event {eventEntity.EventId} for user {this.UserAadId} and team {teamId}");
                }

                this.RecordEvent("Update draft- The HTTP PATCH call to update draft has been succeeded");

                return this.Ok(updateResult);
            }
            catch (Exception ex)
            {
                this.RecordEvent("Update draft- The HTTP PATCH call to update draft has been failed");
                this.logger.LogError(ex, $"Error occurred while updating draft event {eventEntity.EventId} for user {this.UserAadId} and team {teamId}");
                throw;
            }
        }

        /// <summary>
        /// Save new event as draft.
        /// </summary>
        /// <param name="eventEntity">Event details entered by user.</param>
        /// <param name="teamId">Team Id for which event will be created.</param>
        /// <returns>Boolean indicating insert operation result.</returns>
        [HttpPost("create-event")]
        public async Task<IActionResult> CreateEventAsync([FromBody] EventEntity eventEntity, [FromQuery] string teamId)
        {
            this.RecordEvent("Create event- The HTTP POST call to create event has been initiated");

            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("Team Id is either null or empty");
                this.RecordEvent("Create event- The HTTP POST call to create event has been failed");
                return this.BadRequest(new { message = "Team Id is either null or empty" });
            }

            var validationMessages = EventEntity.ValidateEventModel(eventEntity);
            if (validationMessages.Any())
            {
                this.logger.LogError("One or more validation failed for event details");
                this.RecordEvent("Create event- The HTTP POST call to create event has been failed");
                return this.BadRequest(new { errors = validationMessages });
            }

            eventEntity.CreatedBy = this.UserAadId;
            eventEntity.UpdatedBy = this.UserAadId;
            eventEntity.UpdatedOn = DateTime.UtcNow;
            eventEntity.TeamId = teamId;

            try
            {
                var result = await this.eventWorkflowHelper.CreateNewEventAsync(eventEntity, this.UserName);

                if (result == null)
                {
                    this.RecordEvent("Create event- The HTTP POST call to create event has been failed");
                    this.logger.LogInformation($"Event {eventEntity.EventId} could not be found for team {eventEntity.TeamId}");
                    return this.BadRequest(new { message = $"Event {eventEntity.EventId} not found for team {teamId}" });
                }

                if (!(bool)result)
                {
                    this.RecordEvent("Create event- The HTTP POST call to create event has been failed");
                    this.logger.LogInformation($"Unable to create new event {eventEntity.EventId} for team {teamId}");
                }

                this.RecordEvent("Create event- The HTTP POST call to create event has been succeeded");

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this.RecordEvent("Create event- The HTTP POST call to create event has been failed");
                this.logger.LogError(ex, $"Error occurred while creating event {eventEntity.EventId} not found for team {teamId}");
                throw;
            }
        }

        /// <summary>
        /// Handles request to update the event details
        /// </summary>
        /// <param name="eventEntity">The details of an event that needs to be updated</param>
        /// <param name="teamId">The logged-in user's team Id</param>
        /// <returns>Returns true if event details updated successfully. Else returns false.</returns>
        [HttpPatch("update-event")]
        public async Task<IActionResult> UpdateAsync([FromBody] EventEntity eventEntity, [FromQuery] string teamId)
        {
            this.RecordEvent("Update Event- The HTTP PATCH call to update event details has been initiated");

            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("Team Id is either null or empty");
                this.RecordEvent("Update event- The HTTP POST call to create event has been failed");
                return this.BadRequest(new { message = "Team Id is either null or empty" });
            }

            try
            {
                var validationMessages = EventEntity.ValidateEventModel(eventEntity, true);
                if (validationMessages.Any())
                {
                    this.logger.LogError("One or more validation failed for event details");
                    this.RecordEvent("Update event- The HTTP POST call to create event has been failed");
                    return this.BadRequest(new { errors = validationMessages });
                }

                eventEntity.UpdatedBy = this.UserAadId;
                eventEntity.UpdatedOn = DateTime.UtcNow;

                var result = await this.eventWorkflowHelper.UpdateEventAsync(eventEntity);

                if (result == null)
                {
                    this.RecordEvent("Update event- The HTTP PATCH call to update event has been failed");
                    this.logger.LogInformation($"Event {eventEntity.EventId} could not be found for team {eventEntity.TeamId}");
                    return this.BadRequest(new { message = $"Event {eventEntity.EventId} not found for team {teamId}" });
                }

                if (!(bool)result)
                {
                    this.RecordEvent("Update event- The HTTP POST call to create event has been failed");
                    this.logger.LogInformation($"Unable to update new event {eventEntity.EventId} for team {teamId}");
                }

                this.RecordEvent("Update event- The HTTP PATCH call to update event has been succeeded");

                return this.Ok(result);
            }
            catch (Exception ex)
            {
#pragma warning disable CA1062 // Validation is done at model level
                this.RecordEvent(string.Format(CultureInfo.InvariantCulture, "Update Event- The HTTP PATCH call to update event {0} has been failed", eventEntity.EventId));
#pragma warning restore CA1062 // Validation is done at model level
                this.logger.LogError(ex, string.Format(CultureInfo.InvariantCulture, "Error occurred while updating event {0}", eventEntity.EventId));
                throw;
            }
        }

        /// <summary>
        /// Delete draft event.
        /// </summary>
        /// <param name="teamId">Team Id for which event will be created.</param>
        /// <param name="eventId">Event Id for event which needs to be deleted.</param>
        /// <returns>Boolean indicating delete operation result.</returns>
        [HttpDelete("delete-draft")]
        public async Task<IActionResult> DeleteDraftAsync(string teamId, string eventId)
        {
            this.RecordEvent("Delete draft- The HTTP DELETE call to delete draft has been initiated");

            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("Team Id is either null or empty");
                this.RecordEvent("Delete draft- The HTTP DELETE call to delete draft has been failed");
                return this.BadRequest(new { message = "Team Id is either null or empty" });
            }

            if (string.IsNullOrEmpty(eventId))
            {
                this.logger.LogError("Event Id is null or empty");
                this.RecordEvent("Delete draft- The HTTP DELETE call to delete draft has been failed");
                return this.BadRequest(new { message = "Event Id is null or empty" });
            }

            try
            {
                var deleteResult = await this.eventWorkflowHelper.DeleteDraftEventAsync(teamId, eventId);

                if (deleteResult == null)
                {
                    this.RecordEvent("Delete draft- The HTTP DELETE call to delete draft has been failed");
                    this.logger.LogInformation($"Event {eventId} not found for team {teamId}");
                    return this.NotFound(new { message = $"Event {eventId} not found for team {teamId}" });
                }

                if ((bool)deleteResult)
                {
                    this.RecordEvent("Delete draft- The HTTP DELETE call to delete draft has been succeeded");
                }
                else
                {
                    this.RecordEvent("Delete draft- The HTTP DELETE call to delete draft has been failed");
                    this.logger.LogInformation($"Unable to delete draft event {eventId} for team {teamId}");
                }

                return this.Ok(deleteResult);
            }
            catch (Exception ex)
            {
                this.RecordEvent("Delete draft- The HTTP DELETE call to delete draft has been failed");
                this.logger.LogError(ex, $"Error occurred while deleting draft event {eventId} not found for team {teamId}");
                throw;
            }
        }

        /// <summary>
        /// Handles request to close event registrations
        /// </summary>
        /// <param name="teamId">The LnD team Id</param>
        /// <param name="eventId">The event Id of which registrations needs to be closed</param>
        /// <returns>Returns true if event registration closed successfully. Else returns false.</returns>
        [HttpPatch("CloseEventRegistrations")]
        public async Task<IActionResult> CloseEventRegistrationsAsync(string teamId, string eventId)
        {
            try
            {
                this.RecordEvent("Close Event Registration- The HTTP PATCH call to close event registrations has been initiated");

                bool isRegistrationClosedSuccessfully = await this.eventWorkflowHelper.CloseEventRegistrationsAsync(teamId, eventId, this.UserAadId);

                this.RecordEvent("Close Event Registration- The HTTP PATCH call to close event registrations has succeeded");

                if (isRegistrationClosedSuccessfully)
                {
                    return this.Ok(isRegistrationClosedSuccessfully);
                }

                this.RecordEvent(string.Format(CultureInfo.InvariantCulture, "Close Event Registration- The HTTP PATCH call to close registrations for event {0} has failed", eventId));
                return this.Ok(false);
            }
            catch (Exception ex)
            {
                this.RecordEvent(string.Format(CultureInfo.InvariantCulture, "Close Event Registration- The HTTP PATCH call to close registrations for event {0} has failed", eventId));
                this.logger.LogError(ex, string.Format(CultureInfo.InvariantCulture, "Error occured while closing registrations for event {0}", eventId));
                throw;
            }
        }

        /// <summary>
        /// Handles request to cancel an event
        /// </summary>
        /// <param name="teamId">The LnD team Id</param>
        /// <param name="eventId">The event Id that needs to be cancelled</param>
        /// <returns>Returns true if event cancelled successfully. Else returns false.</returns>
        [HttpPatch("CancelEvent")]
        public async Task<IActionResult> CancelEventAsync(string teamId, string eventId)
        {
            try
            {
                this.RecordEvent("Cancel Event- The HTTP PATCH call to cancel event has been initiated");

                bool isStatusUpdatedSuccessfully = await this.eventWorkflowHelper.UpdateEventStatusAsync(teamId, eventId, EventStatus.Cancelled, this.UserAadId);

                this.RecordEvent("Cancel Event- The HTTP PATCH call to cancel event has succeeded");

                if (isStatusUpdatedSuccessfully)
                {
                    return this.Ok(isStatusUpdatedSuccessfully);
                }

                this.RecordEvent(string.Format(CultureInfo.InvariantCulture, "Cancel Event- The HTTP PATCH call to cancel event {0} has failed", eventId));
                return this.Ok(false);
            }
            catch (Exception ex)
            {
                this.RecordEvent(string.Format(CultureInfo.InvariantCulture, "Cancel Event- The HTTP PATCH call to cancel event {0} has failed", eventId));
                this.logger.LogError(ex, string.Format(CultureInfo.InvariantCulture, "Error occured while updating event status to cancelled for event {0}", eventId));
                throw;
            }
        }

        /// <summary>
        /// Handles request to send reminder to the registered users for an event
        /// </summary>
        /// <param name="teamId">The LnD team Id</param>
        /// <param name="eventId">The event Id for which notification to send</param>
        /// <returns>Returns true if reminder sent successfully. Else returns false.</returns>
        [HttpPost("SendReminder")]
        public async Task<IActionResult> SendReminderAsync(string teamId, string eventId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("The team Id is null or empty");
                this.RecordEvent("Send Notification- The HTTP POST call to send notification has been failed");
                return this.BadRequest(new { message = "The valid team Id must be provided" });
            }

            if (string.IsNullOrEmpty(eventId))
            {
                this.logger.LogError("The event Id is null or empty");
                this.RecordEvent("Send Notification- The HTTP POST call to send notification has been failed");
                return this.BadRequest(new { message = "The valid event Id must be provided" });
            }

            try
            {
                this.RecordEvent("Send Notification- The HTTP POST call to send notification has been initiated");

                var notificationFailedUserdIds = await this.eventWorkflowHelper.SendReminderAsync(teamId, eventId);

                this.RecordEvent("Send Notification- The HTTP POST call to send notification has finished");

                if (notificationFailedUserdIds == null)
                {
                    this.RecordEvent("Send Notification- Failed to send notification");
                    return this.Ok(false);
                }

                if (notificationFailedUserdIds.Any())
                {
                    this.RecordEvent(string.Format(CultureInfo.InvariantCulture, "Send Notification- Failed to send notification to the user Ids {0}", string.Join(",", notificationFailedUserdIds)));
                }

                return this.Ok(true);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, string.Format(CultureInfo.InvariantCulture, "Error occured while sending notification for event {0}", eventId));
                this.RecordEvent(string.Format(CultureInfo.InvariantCulture, "Send Notification- The HTTP POST call to send notification failed for event {0}", eventId));
                throw;
            }
        }
    }
}
