// <copyright file="UserEventsHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.EmployeeTraining.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.EmployeeTraining.Cards;
    using Microsoft.Teams.Apps.EmployeeTraining.Models;
    using Microsoft.Teams.Apps.EmployeeTraining.Models.Enums;
    using Microsoft.Teams.Apps.EmployeeTraining.Repositories;
    using Microsoft.Teams.Apps.EmployeeTraining.Services;

    /// <summary>
    /// The helper class to manage the events operations done by the user
    /// </summary>
    public class UserEventsHelper : IUserEventsHelper
    {
        /// <summary>
        /// Provides the methods for event related operations on storage.
        /// </summary>
        private readonly IEventRepository eventRepository;

        /// <summary>
        /// Search service to filter and search events.
        /// </summary>
        private readonly IEventSearchService eventSearchService;

        /// <summary>
        /// Search service to filter and search events for end user.
        /// </summary>
        private readonly IUserEventSearchService userEventSearchService;

        /// <summary>
        /// Helper to use Microsoft Graph users api.
        /// </summary>
        private readonly IUserGraphHelper userGraphHelper;

        /// <summary>
        /// Helper to use Microsoft Graph events api.
        /// </summary>
        private readonly IEventGraphHelper eventGraphHelper;

        /// <summary>
        /// Helper to send notifications to user and team.
        /// </summary>
        private readonly INotificationHelper notificationHelper;

        /// <summary>
        /// Helper to bind category name by Id.
        /// </summary>
        private readonly ICategoryHelper categoryHelper;

        /// <summary>
        /// Team configuration repository for storing and updating team information.
        /// </summary>
        private readonly ILnDTeamConfigurationRepository lnDTeamConfigurationRepository;

        /// <summary>
        /// Represents a set of key/value application configuration properties for bot.
        /// </summary>
        private readonly IOptions<BotSettings> botOptions;

        /// <summary>
        /// The current culture's string localizer.
        /// </summary>
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserEventsHelper"/> class.
        /// </summary>
        /// <param name="eventRepository">Provides the methods for event related operations on storage.</param>
        /// <param name="eventSearchService">Search service to filter and search events.</param>
        /// <param name="userEventSearchService">Search service to filter and search events for end user.</param>
        /// <param name="userGraphHelper">Helper to use Microsoft Graph users api.</param>
        /// <param name="eventGraphHelper">Helper to use Microsoft Graph events api.</param>
        /// <param name="notificationHelper">Helper to send notifications to user and team.</param>
        /// <param name="categoryHelper">Helper to bind category name by Id.</param>
        /// <param name="lnDTeamConfigurationRepository">Team configuration repository for storing and updating team information.</param>
        /// <param name="botOptions">Represents a set of key/value application configuration properties for bot.</param>
        /// <param name="localizer">The current culture's string localizer.</param>
        public UserEventsHelper(
            IEventRepository eventRepository,
            IEventSearchService eventSearchService,
            IUserEventSearchService userEventSearchService,
            IUserGraphHelper userGraphHelper,
            IEventGraphHelper eventGraphHelper,
            INotificationHelper notificationHelper,
            ICategoryHelper categoryHelper,
            ILnDTeamConfigurationRepository lnDTeamConfigurationRepository,
            IOptions<BotSettings> botOptions,
            IStringLocalizer<Strings> localizer)
        {
            this.eventRepository = eventRepository;
            this.eventSearchService = eventSearchService;
            this.userEventSearchService = userEventSearchService;
            this.userGraphHelper = userGraphHelper;
            this.eventGraphHelper = eventGraphHelper;
            this.notificationHelper = notificationHelper;
            this.categoryHelper = categoryHelper;
            this.lnDTeamConfigurationRepository = lnDTeamConfigurationRepository;
            this.botOptions = botOptions;
            this.localizer = localizer;
        }

        /// <summary>
        /// Get event details.
        /// </summary>
        /// <param name="eventId">Event Id for which details needs to be fetched.</param>
        /// <param name="teamId">Team Id with which event is associated.</param>
        /// <param name="userObjectId">The user Id</param>
        /// <returns>Event details.</returns>
        public async Task<EventEntity> GetEventAsync(string eventId, string teamId, string userObjectId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return null;
            }

            if (string.IsNullOrEmpty(teamId))
            {
                return null;
            }

            if (string.IsNullOrEmpty(userObjectId))
            {
                return null;
            }

            var eventDetails = await this.eventRepository.GetEventDetailsAsync(eventId, teamId);

            if (eventDetails != null)
            {
                if (eventDetails.AutoRegisteredAttendees != null
                    && eventDetails.AutoRegisteredAttendees.Contains(userObjectId, StringComparison.OrdinalIgnoreCase))
                {
                    eventDetails.IsMandatoryForLoggedInUser = true;
                    eventDetails.IsLoggedInUserRegistered = true;
                }

                if (eventDetails.RegisteredAttendees != null
                    && eventDetails.RegisteredAttendees.Contains(userObjectId, StringComparison.OrdinalIgnoreCase))
                {
                    eventDetails.IsLoggedInUserRegistered = true;
                }

                if (eventDetails.MandatoryAttendees != null
                    && eventDetails.MandatoryAttendees.Contains(userObjectId, StringComparison.OrdinalIgnoreCase))
                {
                    eventDetails.IsMandatoryForLoggedInUser = true;
                }
            }

            return eventDetails;
        }

        /// <summary>
        /// Registers the user for an event
        /// </summary>
        /// <param name="teamId">The LnD team Id who created the event</param>
        /// <param name="eventId">The event Id</param>
        /// <param name="userAADObjectId">The user Id</param>
        /// <returns>Returns true if registration done successfully. Else returns false.</returns>
        public async Task<bool> RegisterToEventAsync(string teamId, string eventId, string userAADObjectId)
        {
            if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(userAADObjectId))
            {
                return false;
            }

            var eventDetails = await this.eventRepository.GetEventDetailsAsync(eventId, teamId);

            if (eventDetails == null
                || eventDetails.Status != (int)EventStatus.Active
                || eventDetails.IsRegistrationClosed
                || eventDetails.RegisteredAttendeesCount >= eventDetails.MaximumNumberOfParticipants
                || eventDetails.EndDate < DateTime.UtcNow)
            {
                return false;
            }

            if (eventDetails.Audience == (int)EventAudience.Private
                && eventDetails.MandatoryAttendees != null
                && !eventDetails.MandatoryAttendees.Contains(userAADObjectId, StringComparison.OrdinalIgnoreCase)
                && eventDetails.OptionalAttendees != null
                && !eventDetails.OptionalAttendees.Contains(userAADObjectId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if ((eventDetails.AutoRegisteredAttendees != null
                && eventDetails.AutoRegisteredAttendees.Contains(userAADObjectId, StringComparison.OrdinalIgnoreCase))
                || (eventDetails.RegisteredAttendees != null
                && eventDetails.RegisteredAttendees.Contains(userAADObjectId, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (string.IsNullOrEmpty(eventDetails.RegisteredAttendees))
            {
                eventDetails.RegisteredAttendees = userAADObjectId;
            }
            else
            {
                eventDetails.RegisteredAttendees = string.Join(";", eventDetails.RegisteredAttendees, userAADObjectId);
            }

            eventDetails.RegisteredAttendeesCount += 1;

            var isGraphEventUpdated = await this.UpdateGraphEvent(eventDetails);

            if (isGraphEventUpdated)
            {
                var isRegisteredSuccessfully = await this.eventRepository.UpsertEventAsync(eventDetails);

                if (isRegisteredSuccessfully)
                {
                    await this.UpdateEventNotificationInTeam(eventDetails);
                    await this.eventSearchService.RunIndexerOnDemandAsync();
                }

                return isRegisteredSuccessfully;
            }

            return false;
        }

        /// <summary>
        /// Unregisters the user for an event
        /// </summary>
        /// <param name="teamId">The LnD team Id who created the event</param>
        /// <param name="eventId">The event Id</param>
        /// <param name="userAADObjectId">The user Id</param>
        /// <returns>Returns true if the user successfully unregistered for an event. Else returns false.</returns>
        public async Task<bool> RemoveEventAsync(string teamId, string eventId, string userAADObjectId)
        {
            if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(userAADObjectId))
            {
                return false;
            }

            var eventDetails = await this.eventRepository.GetEventDetailsAsync(eventId, teamId);

            if (eventDetails == null
                || eventDetails.Status != (int)EventStatus.Active
                || eventDetails.EndDate < DateTime.UtcNow)
            {
                return false;
            }

            if (eventDetails.Audience == (int)EventAudience.Private
                && eventDetails.MandatoryAttendees != null
                && !eventDetails.MandatoryAttendees.Contains(userAADObjectId, StringComparison.OrdinalIgnoreCase)
                && eventDetails.OptionalAttendees != null
                && !eventDetails.OptionalAttendees.Contains(userAADObjectId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool isRegisteredUser = false;

            if (eventDetails.AutoRegisteredAttendees != null
                && eventDetails.AutoRegisteredAttendees.Contains(userAADObjectId, StringComparison.OrdinalIgnoreCase))
            {
                isRegisteredUser = true;

                var autoRegisteredAttendees = eventDetails.AutoRegisteredAttendees.Split(";");
                eventDetails.AutoRegisteredAttendees = string.Join(";", autoRegisteredAttendees.Where(x => x != userAADObjectId));
            }

            if (eventDetails.RegisteredAttendees != null
                && eventDetails.RegisteredAttendees.Contains(userAADObjectId, StringComparison.OrdinalIgnoreCase))
            {
                isRegisteredUser = true;

                var registeredAttendees = eventDetails.RegisteredAttendees.Split(";");
                eventDetails.RegisteredAttendees = string.Join(";", registeredAttendees.Where(x => x != userAADObjectId));
            }

            if (!isRegisteredUser)
            {
                return false;
            }

            eventDetails.RegisteredAttendeesCount -= 1;

            var isGraphEventUpdated = await this.UpdateGraphEvent(eventDetails);

            if (isGraphEventUpdated)
            {
                var isUnregisteredSuccessfully = await this.eventRepository.UpsertEventAsync(eventDetails);

                if (isUnregisteredSuccessfully)
                {
                    await this.UpdateEventNotificationInTeam(eventDetails);
                    await this.eventSearchService.RunIndexerOnDemandAsync();
                }

                return isUnregisteredSuccessfully;
            }

            return false;
        }

        /// <summary>
        /// Get user events as per user search text and filters
        /// </summary>
        /// <param name="searchString">Search string entered by user.</param>
        /// <param name="pageCount">>Page count for which post needs to be fetched.</param>
        /// <param name="eventSearchType">Event search operation type. Refer <see cref="EventSearchType"/> for values.</param>
        /// <param name="userObjectId">Logged in user's AAD object identifier.</param>
        /// <param name="createdByFilter">Semicolon separated user AAD object identifier who created events.</param>
        /// <param name="categoryFilter">Semicolon separated category Ids.</param>
        /// <param name="sortBy">0 for recent and 1 for popular events. Refer <see cref="SortBy"/> for values.</param>
        /// <returns>List of user events</returns>
        public async Task<IEnumerable<EventEntity>> GetEventsAsync(string searchString, int pageCount, int eventSearchType, string userObjectId, string createdByFilter, string categoryFilter, int sortBy)
        {
            var recentCollaboratorIds = Enumerable.Empty<string>();

            if (sortBy == (int)SortBy.PopularityByRecentCollaborators)
            {
                recentCollaboratorIds = await this.GetTopRecentCollaborators();
            }

            var paramsDto = new SearchParametersDto
            {
                SearchString = searchString,
                PageCount = pageCount,
                SearchScope = (EventSearchType)eventSearchType,
                UserObjectId = userObjectId,
                CreatedByFilter = createdByFilter,
                CategoryFilter = categoryFilter,
                SortByFilter = sortBy,
                RecentCollaboratorIds = recentCollaboratorIds,
            };

            var userEvents = await this.userEventSearchService.GetEventsAsync(paramsDto);

            return userEvents;
        }

        /// <summary>
        /// Update graph event
        /// </summary>
        /// <param name="eventToUpsert">The event to be updated</param>
        /// <returns>Returns boolean indicating whether update operation is successful</returns>
        private async Task<bool> UpdateGraphEvent(EventEntity eventToUpsert)
        {
            // Calculate occurrences as per start and end date.
            eventToUpsert.NumberOfOccurrences = this.GetWeekDaysCount(eventToUpsert.StartDate.Value, eventToUpsert.EndDate.Value);

            // Create event using MS Graph.
            var graphEventResult = await this.eventGraphHelper.UpdateEventAsync(eventToUpsert);

            if (graphEventResult == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sends notification in team after event in created
        /// </summary>
        /// <param name="eventDetails">Event details.</param>
        /// <returns>Returns true if notification sent successfully. Else returns false.</returns>
        private async Task<string> UpdateEventNotificationInTeam(EventEntity eventDetails)
        {
            if (eventDetails == null)
            {
                return null;
            }

            await this.categoryHelper.BindCategoryNameAsync(new List<EventEntity>() { eventDetails });
            var teamDetails = await this.lnDTeamConfigurationRepository.GetTeamDetailsAsync(eventDetails.TeamId);

            var createdByName = await this.userGraphHelper.GetUserAsync(eventDetails.CreatedBy);

            var notificationCard = EventDetailsCard.GetEventCreationCardForTeam(this.botOptions.Value.AppBaseUri, this.localizer, eventDetails, createdByName?.DisplayName);

            if (string.IsNullOrEmpty(eventDetails.TeamCardActivityId))
            {
                return await this.notificationHelper.SendNotificationInTeamAsync(teamDetails, notificationCard);
            }
            else
            {
                return await this.notificationHelper.SendNotificationInTeamAsync(teamDetails, notificationCard, true, eventDetails.TeamCardActivityId);
            }
        }

        /// <summary>
        /// Calculate weekdays for date range.
        /// </summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <returns>Number of week days.</returns>
        private int GetWeekDaysCount(DateTime startDate, DateTime endDate)
        {
            int ndays = 1 + Convert.ToInt32((endDate - startDate).TotalDays);
            int nsaturdays = (ndays + Convert.ToInt32(startDate.DayOfWeek, CultureInfo.CurrentCulture)) / 7;
            return ndays - (2 * nsaturdays)
                   - (startDate.DayOfWeek == DayOfWeek.Sunday ? 1 : 0)
                   + (endDate.DayOfWeek == DayOfWeek.Saturday ? 1 : 0);
        }

        /// <summary>
        /// Get top recent collaborators ordered by relevance score in descending order
        /// </summary>
        /// <returns>Returns the list of top collaborators</returns>
        private async Task<IEnumerable<string>> GetTopRecentCollaborators()
        {
            var recentCollaborators = await this.userGraphHelper.GetRecentCollaboratorsAsync();

            if (recentCollaborators != null && recentCollaborators.Any())
            {
                var collaboratorsWithTopScoredEmailAddress = recentCollaborators
                    .Select(collaborator => new { id = collaborator.Id, topScoredEmailAddress = collaborator.ScoredEmailAddresses?.OrderByDescending(scoredEmailAddress => scoredEmailAddress.RelevanceScore)?.First() });

                var topCollaborators = collaboratorsWithTopScoredEmailAddress
                    .Where(collaborator => collaborator.topScoredEmailAddress != null)
                    ?.OrderByDescending(collaborator => collaborator.topScoredEmailAddress.RelevanceScore)
                    .Select(collaborator => collaborator.id)
                    .Take(20);

                return topCollaborators;
            }

            return null;
        }
    }
}