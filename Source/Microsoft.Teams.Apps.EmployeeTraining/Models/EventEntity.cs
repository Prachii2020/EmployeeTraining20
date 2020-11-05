// <copyright file="EventEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.EmployeeTraining.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.Search;

    /// <summary>
    /// This class is responsible to store the event details.
    /// </summary>
    public class EventEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the event Id GUID.
        /// </summary>
        [Key]
        public string EventId
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        /// <summary>
        /// Gets or sets team Id.
        /// </summary>
        [IsFilterable]
        public string TeamId
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }

        /// <summary>
        /// Gets or sets event Id received from Graph.
        /// </summary>
        public string GraphEventId { get; set; }

        /// <summary>
        /// Gets or sets activity Id of card sent in LnD team chat. This is used to update card when event details are updated.
        /// </summary>
        public string TeamCardActivityId { get; set; }

        /// <summary>
        /// Gets or sets an event name
        /// </summary>
        [Required]
        [MaxLength(100)]
        [IsSearchable]
        [IsFilterable]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets event description.
        /// </summary>
        [IsSearchable]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets event photo.
        /// </summary>
        public string Photo { get; set; }

        /// <summary>
        /// Gets or sets start date and time of an event.
        /// </summary>
        [IsFilterable]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets start time of an event.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Gets or sets end time of an event.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets end date and time of an event.
        /// </summary>
        [IsFilterable]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an event get registered automatically.
        /// </summary>
        public bool IsAutoRegister { get; set; }

        /// <summary>
        /// Gets or sets the event type. Ref: <see cref="EventType"/>
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the meeting link for an event.
        /// </summary>
        public string MeetingLink { get; set; }

        /// <summary>
        /// Gets or sets the venue for an event.
        /// </summary>
        [IsFilterable]
        public string Venue { get; set; }

        /// <summary>
        /// Gets or sets the event category Id.
        /// </summary>
        [IsFilterable]
        public string CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the event category name.
        /// </summary>
        [NotMapped]
        public string CategoryName { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of participants who can join the event.
        /// </summary>
        public int MaximumNumberOfParticipants { get; set; }

        /// <summary>
        /// Gets or sets the audience to which event is visible. Ref: <see cref="EventAudience"/>
        /// </summary>
        [IsFilterable]
        public int Audience { get; set; }

        /// <summary>
        /// Gets or sets registered attendees count for an event used for searching and filtering.
        /// </summary>
        [IsFilterable]
        [IsSortable]
        public int RegisteredAttendeesCount { get; set; }

        /// <summary>
        /// Gets or sets semicolon separated user object identifiers for users for whom event is mandatory.
        /// </summary>
        [IsFilterable]
        [IsSearchable]
        public string MandatoryAttendees { get; set; }

        /// <summary>
        /// Gets or sets semicolon separated user object identifiers for users for whom event is optional.
        /// </summary>
        [IsFilterable]
        [IsSearchable]
        public string OptionalAttendees { get; set; }

        /// <summary>
        /// Gets or sets semicolon separated user object identifiers for users who registered for the event.
        /// </summary>
        [IsFilterable]
        [IsSearchable]
        public string RegisteredAttendees { get; set; }

        /// <summary>
        /// Gets or sets semicolon separated user object identifiers for users who auto registered for the event.
        /// </summary>
        [IsFilterable]
        [IsSearchable]
        public string AutoRegisteredAttendees { get; set; }

        /// <summary>
        /// Gets or sets selected list of users or groups in JSON string (used to persist selected groups and user information while editing event).
        /// </summary>
        public string SelectedUserOrGroupListJSON { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether event has been deleted.
        /// </summary>
        [IsFilterable]
        public bool IsRemoved { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether event registration has been closed.
        /// </summary>
        [IsFilterable]
        public bool IsRegistrationClosed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the event is mandatory for logged-in user
        /// </summary>
        [NotMapped]
        public bool IsMandatoryForLoggedInUser { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a logged-in user registered for an event
        /// </summary>
        [NotMapped]
        public bool IsLoggedInUserRegistered { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a logged-in user can register for an event
        /// </summary>
        [NotMapped]
        public bool CanLoggedInUserRegister { get; set; }

        /// <summary>
        /// Gets or sets the status of an event. Ref: <see cref="EventStatus"/>
        /// </summary>
        [IsFilterable]
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the date and time on which the event has created.
        /// </summary>
        [IsSortable]
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the user name who created the event.
        /// </summary>
        [IsFilterable]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time on which the event details updated.
        /// </summary>
        public DateTime UpdatedOn { get; set; }

        /// <summary>
        /// Gets or sets the user name who updated the event details.
        /// </summary>
        public string UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the number of occurrences for event.
        /// </summary>
        [NotMapped]
        public int NumberOfOccurrences { get; set; }

        /// <summary>
        /// Gets or sets the total count of recent collaborators of logged-in user
        /// </summary>
        [NotMapped]
        public int LoggedInUserCollaboratorsCount { get; set; }

        /// <summary>
        /// Validate event details model.
        /// </summary>
        /// <param name="eventDetails">Event details which needs to be saved.</param>
        /// <param name="isUpdate">Set whether an event to be updated</param>
        /// <returns>Error message if any or null.</returns>
        public static List<string> ValidateEventModel(EventEntity eventDetails, bool isUpdate = false)
        {
            const short EventVenueMaxLetters = 200;
            const short EventDescriptionMaxLetters = 1000;

            eventDetails = eventDetails ?? throw new ArgumentNullException(nameof(eventDetails), "Event details is null");

            List<string> validationMessages = new List<string>();

            // Description validation
            if (string.IsNullOrEmpty(eventDetails.Description))
            {
                validationMessages.Add("Event description is required");
            }
            else if (eventDetails.Description.Length > EventDescriptionMaxLetters)
            {
                validationMessages.Add($"Event description length cannot be more than {EventDescriptionMaxLetters} letters");
            }

            // Photo URL validation
            if (string.IsNullOrEmpty(eventDetails.Photo))
            {
                validationMessages.Add("Event photo URL is required");
            }

            bool result = Uri.TryCreate(eventDetails.Photo, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!result)
            {
                validationMessages.Add("Event photo URL must be in valid URL format");
            }

            // Start date validation
            if (eventDetails.StartDate == null)
            {
                validationMessages.Add("Event start date is required");
            }
            else if (!(isUpdate && eventDetails.StartDate < DateTime.UtcNow))
            {
                if (eventDetails.StartDate < DateTime.UtcNow.AddDays(1).Date)
                {
                    validationMessages.Add("Event start date must be future date.");
                }

                // Event time validation
                if (eventDetails.StartTime == null)
                {
                    validationMessages.Add("Event start time is required");
                }
                else if (eventDetails.EndTime == null)
                {
                    validationMessages.Add("Event end time is required");
                }
                else
                {
                    if (eventDetails.EndTime < eventDetails.StartTime)
                    {
                        validationMessages.Add("Event end time must be greater than start time");
                    }
                }
            }

            // Event type validation
            if (eventDetails.Type < (int)EventType.InPerson || eventDetails.Type > (int)EventType.LiveEvent)
            {
                validationMessages.Add("Invalid event type value. Event type should be in-between 1 to 3");
            }

            // Meeting link validation
            if (eventDetails.Type == (int)EventType.LiveEvent)
            {
                if (string.IsNullOrEmpty(eventDetails.MeetingLink))
                {
                    validationMessages.Add("Meeting link is required");
                }

                result = Uri.TryCreate(eventDetails.MeetingLink, UriKind.Absolute, out uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (!result)
                {
                    validationMessages.Add("Meeting URL must be in valid URL format");
                }
            }

            // Venue validation
            if (eventDetails.Type == (int)EventType.InPerson)
            {
                if (string.IsNullOrEmpty(eventDetails.Venue))
                {
                    validationMessages.Add("Meeting link is required");
                }

                if (eventDetails.Venue.Length > EventVenueMaxLetters)
                {
                    validationMessages.Add($"Event venue cannot be more than {EventVenueMaxLetters} letters");
                }
            }

            // Event category validation
            if (string.IsNullOrEmpty(eventDetails.CategoryId))
            {
                validationMessages.Add("Event category is required");
            }

            // Max participants validation
            if (eventDetails.MaximumNumberOfParticipants < 1)
            {
                validationMessages.Add($"Invalid {nameof(eventDetails.MaximumNumberOfParticipants)} value.");
            }

            // Audience validation
            if (eventDetails.Audience < (int)EventAudience.Public || eventDetails.Audience > (int)EventAudience.Private)
            {
                validationMessages.Add($"Invalid {nameof(eventDetails.Audience)} value. It should be either {(int)EventAudience.Public} or {(int)EventAudience.Private}");
            }

            return validationMessages;
        }

        /// <summary>
        /// Gets the event attendees
        /// </summary>
        /// <returns>Returns the list of event attendees.</returns>
        public IEnumerable<string> GetAttendees()
        {
            var eventAttendees = new List<string>();

            if (this.RegisteredAttendeesCount > 0)
            {
                if (!string.IsNullOrEmpty(this.RegisteredAttendees))
                {
                    eventAttendees.AddRange(this.RegisteredAttendees.Split(";"));
                }

                if (!string.IsNullOrEmpty(this.AutoRegisteredAttendees))
                {
                    eventAttendees.AddRange(this.AutoRegisteredAttendees.Split(";"));
                }
            }

            return eventAttendees;
        }
    }
}