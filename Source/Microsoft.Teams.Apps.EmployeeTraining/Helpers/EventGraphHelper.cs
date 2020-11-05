// <copyright file="EventGraphHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.EmployeeTraining.Helpers
{
    extern alias BetaLib;

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.EmployeeTraining.Models;
#pragma warning disable SA1135 // Referring BETA package of MS Graph SDK.
    using Beta = BetaLib.Microsoft.Graph;
#pragma warning restore SA1135 // Referring BETA package of MS Graph SDK.
    using DayOfWeek = Microsoft.Graph.DayOfWeek;
    using EventType = Microsoft.Teams.Apps.EmployeeTraining.Models.EventType;

    /// <summary>
    /// Implements the methods that are defined in <see cref="IEventGraphHelper"/>
    /// </summary>
    public class EventGraphHelper : IEventGraphHelper
    {
        /// <summary>
        /// Instance of graph service client for delegated requests.
        /// </summary>
        private readonly GraphServiceClient delegatedGraphClient;

        /// <summary>
        /// Instance of graph service client for application level requests.
        /// </summary>
        private readonly GraphServiceClient applicationGraphClient;

        /// <summary>
        /// Instance of BETA graph service client for application level requests.
        /// </summary>
        private readonly Beta.GraphServiceClient applicationBetaGraphClient;

        /// <summary>
        /// Instance of a class that provides methods for user related grapg API calls.
        /// </summary>
        private readonly IUserGraphHelper userGraphHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGraphHelper"/> class.
        /// </summary>
        /// <param name="tokenAcquisitionHelper">Helper to get user access token for specified Graph scopes.</param>
        /// <param name="httpContextAccessor">HTTP context accessor for getting user claims.</param>
        /// <param name="userGraphHelper">The user graph helper dependency injection</param>
        public EventGraphHelper(
            ITokenAcquisitionHelper tokenAcquisitionHelper,
            IHttpContextAccessor httpContextAccessor,
            IUserGraphHelper userGraphHelper)
        {
            httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            this.userGraphHelper = userGraphHelper;

            var oidClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
            var userObjectId = httpContextAccessor.HttpContext.User.Claims?
                .FirstOrDefault(claim => oidClaimType.Equals(claim.Type, StringComparison.OrdinalIgnoreCase))?.Value;

            if (!string.IsNullOrEmpty(userObjectId))
            {
                var jwtToken = AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString()).Parameter;

                this.delegatedGraphClient = GraphServiceClientFactory
                    .GetAuthenticatedGraphClient(async () =>
                    {
                        return await tokenAcquisitionHelper.GetUserAccessTokenAsync(userObjectId, jwtToken);
                    });

                this.applicationBetaGraphClient = GraphServiceClientFactory
                    .GetAuthenticatedBetaGraphClient(async () =>
                    {
                        return await tokenAcquisitionHelper.GetApplicationAccessTokenAsync();
                    });

                this.applicationGraphClient = GraphServiceClientFactory
                    .GetAuthenticatedGraphClient(async () =>
                    {
                        return await tokenAcquisitionHelper.GetApplicationAccessTokenAsync();
                    });
            }
        }

        /// <summary>
        /// Cancel calendar event.
        /// </summary>
        /// <param name="eventGraphId">Event Id received from Graph.</param>
        /// <param name="createdByUserId">User Id who created event.</param>
        /// <param name="comment">Cancellation comment.</param>
        /// <returns>True if event cancellation is successful.</returns>
        public async Task<bool> CancelEventAsync(string eventGraphId, string createdByUserId, string comment)
        {
            await this.applicationBetaGraphClient.Users[createdByUserId].Events[eventGraphId]
                .Cancel(comment).Request().PostAsync();

            return true;
        }

        /// <summary>
        /// Create teams event.
        /// </summary>
        /// <param name="eventEntity">Event details from user for which event needs to be created.</param>
        /// <returns>Created event details.</returns>
        public async Task<Event> CreateEventAsync(EventEntity eventEntity)
        {
            eventEntity = eventEntity ??
                throw new ArgumentNullException(nameof(eventEntity), "Event details cannot be null");

            var teamsEvent = new Event
            {
                Subject = eventEntity.Name,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = eventEntity.Type == (int)EventType.LiveEvent ?
                        $"{eventEntity.Description}<br/><br/><a href='{eventEntity.MeetingLink}'>{eventEntity.MeetingLink}</a>" :
                        eventEntity.Description,
                },
                Attendees = eventEntity.IsAutoRegister && eventEntity.Audience == (int)EventAudience.Private ?
                    await this.GetEventAttendeesTemplateAsync(eventEntity) :
                    new List<Attendee>(),
                OnlineMeetingUrl = eventEntity.Type == (int)EventType.LiveEvent ? eventEntity.MeetingLink : null,
                IsReminderOn = true,
                Location = eventEntity.Type == (int)EventType.InPerson ? new Location
                {
                    Address = new PhysicalAddress { Street = eventEntity.Venue },
                }
                    : null,
                AllowNewTimeProposals = false,
                IsOnlineMeeting = eventEntity.Type == (int)EventType.Teams,
                OnlineMeetingProvider = eventEntity.Type == (int)EventType.Teams ? OnlineMeetingProviderType.TeamsForBusiness : OnlineMeetingProviderType.Unknown,
            };

            teamsEvent.Start = new DateTimeTimeZone
            {
                DateTime = eventEntity.StartDate?.ToString("s", CultureInfo.InvariantCulture),
                TimeZone = TimeZoneInfo.Utc.Id,
            };
            teamsEvent.End = new DateTimeTimeZone
            {
                DateTime = eventEntity.StartDate.Value.Date.Add(
                new TimeSpan(eventEntity.EndTime.Hour, eventEntity.EndTime.Minute, eventEntity.EndTime.Second)).ToString("s", CultureInfo.InvariantCulture),
                TimeZone = TimeZoneInfo.Utc.Id,
            };

            if (eventEntity.NumberOfOccurrences > 1)
            {
                // Create recurring event.
                teamsEvent = this.GetRecurringEventTemplate(teamsEvent, eventEntity);
            }

            return await this.delegatedGraphClient.Me.Events.Request()
                .Header("Prefer", $"outlook.timezone=\"{TimeZoneInfo.Utc.Id}\"").AddAsync(teamsEvent);
        }

        /// <summary>
        /// Update teams event.
        /// </summary>
        /// <param name="eventEntity">Event details from user for which event needs to be updated.</param>
        /// <returns>Updated event details.</returns>
        public async Task<Event> UpdateEventAsync(EventEntity eventEntity)
        {
            eventEntity = eventEntity ??
                throw new ArgumentNullException(nameof(eventEntity), "Event details cannot be null");

            var teamsEvent = new Event
            {
                Subject = eventEntity.Name,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = eventEntity.Type == (int)EventType.LiveEvent ?
                        $"{eventEntity.Description}<br/><br/><a href='{eventEntity.MeetingLink}'>{eventEntity.MeetingLink}</a>" :
                        eventEntity.Description,
                },
                Attendees = eventEntity.IsAutoRegister && eventEntity.Audience == (int)EventAudience.Private ?
                    await this.GetEventAttendeesTemplateAsync(eventEntity) :
                    new List<Attendee>(),
                OnlineMeetingUrl = eventEntity.Type == (int)EventType.LiveEvent ? eventEntity.MeetingLink : null,
                IsReminderOn = true,
                Location = eventEntity.Type == (int)EventType.InPerson ? new Location
                {
                    Address = new PhysicalAddress { Street = eventEntity.Venue },
                }
                    : null,
                AllowNewTimeProposals = false,
                IsOnlineMeeting = eventEntity.Type == (int)EventType.Teams,
                OnlineMeetingProvider = eventEntity.Type == (int)EventType.Teams ? OnlineMeetingProviderType.TeamsForBusiness : OnlineMeetingProviderType.Unknown,
            };

            teamsEvent.Start = new DateTimeTimeZone
            {
                DateTime = eventEntity.StartDate?.ToString("s", CultureInfo.InvariantCulture),
                TimeZone = TimeZoneInfo.Utc.Id,
            };
            teamsEvent.End = new DateTimeTimeZone
            {
                DateTime = eventEntity.StartDate.Value.Date.Add(
                new TimeSpan(eventEntity.EndTime.Hour, eventEntity.EndTime.Minute, eventEntity.EndTime.Second)).ToString("s", CultureInfo.InvariantCulture),
                TimeZone = TimeZoneInfo.Utc.Id,
            };

            if (eventEntity.NumberOfOccurrences > 1)
            {
                // Create recurring event.
                teamsEvent = this.GetRecurringEventTemplate(teamsEvent, eventEntity);
            }

            return await this.applicationGraphClient.Users[eventEntity.CreatedBy].Events[eventEntity.GraphEventId].Request()
                .Header("Prefer", $"outlook.timezone=\"{TimeZoneInfo.Utc.Id}\"").UpdateAsync(teamsEvent);
        }

        /// <summary>
        /// Modify event details for recurring event creation.
        /// </summary>
        /// <param name="teamsEvent">Event details which will be sent to Graph API.</param>
        /// <param name="eventEntity">Event details from user for which event needs to be created.</param>
        /// <returns>Event details to be sent to Graph API.</returns>
        private Event GetRecurringEventTemplate(Event teamsEvent, EventEntity eventEntity)
        {
            // Create recurring event.
            teamsEvent.Recurrence = new PatternedRecurrence
            {
                Pattern = new RecurrencePattern
                {
                    // Exclude weekends
                    Type = RecurrencePatternType.Weekly,
                    Interval = 1,
                    FirstDayOfWeek = DayOfWeek.Monday,
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                },
                Range = new RecurrenceRange
                {
                    Type = RecurrenceRangeType.EndDate,
                    EndDate = new Date((int)eventEntity.EndDate?.Year, (int)eventEntity.EndDate?.Month, (int)eventEntity.EndDate?.Day),
                    StartDate = new Date((int)eventEntity.StartDate?.Year, (int)eventEntity.StartDate?.Month, (int)eventEntity.StartDate?.Day),
                },
            };

            return teamsEvent;
        }

        /// <summary>
        /// Get list of event attendees for creating teams event.
        /// </summary>
        /// <param name="eventEntity">Event details containing registered attendees.</param>
        /// <returns>List of attendees.</returns>
        private async Task<List<Attendee>> GetEventAttendeesTemplateAsync(EventEntity eventEntity)
        {
            var attendees = new List<Attendee>();

            if (string.IsNullOrEmpty(eventEntity.RegisteredAttendees) && string.IsNullOrEmpty(eventEntity.AutoRegisteredAttendees))
            {
                return attendees;
            }

            if (!string.IsNullOrEmpty(eventEntity.RegisteredAttendees))
            {
                var registeredAttendeesList = eventEntity.RegisteredAttendees.Trim().Split(";");
                if (registeredAttendeesList.Any())
                {
                    var userProfiles = await this.userGraphHelper.GetUsersAsync(registeredAttendeesList);

                    foreach (var userProfile in userProfiles)
                    {
                        attendees.Add(new Attendee
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = userProfile.UserPrincipalName,
                                Name = userProfile.DisplayName,
                            },
                            Type = AttendeeType.Required,
                        });
                    }
                }
            }

            if (!string.IsNullOrEmpty(eventEntity.AutoRegisteredAttendees))
            {
                var autoRegisteredAttendeesList = eventEntity.AutoRegisteredAttendees.Trim().Split(";");
                if (autoRegisteredAttendeesList.Any())
                {
                    var userProfiles = await this.userGraphHelper.GetUsersAsync(autoRegisteredAttendeesList);

                    foreach (var userProfile in userProfiles)
                    {
                        attendees.Add(new Attendee
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = userProfile.UserPrincipalName,
                                Name = userProfile.DisplayName,
                            },
                            Type = AttendeeType.Required,
                        });
                    }
                }
            }

            return attendees;
        }
    }
}