// <copyright file="EventRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.EmployeeTraining.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.EmployeeTraining.Models;
    using Microsoft.Teams.Apps.EmployeeTraining.Models.Configuration;

    /// <summary>
    /// The event repository class which manages events' data in Azure Table Storage
    /// </summary>
    public class EventRepository : BaseRepository<EventEntity>, IEventRepository
    {
        /// <summary>
        /// Represents the entity name which is used to store events.
        /// </summary>
        private const string EventEntityName = nameof(EventEntity);

        /// <summary>
        /// Initializes a new instance of the <see cref="EventRepository"/> class.
        /// </summary>
        /// <param name="options">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">To send logs to the logger service.</param>
        public EventRepository(
            IOptions<StorageSetting> options, ILogger<EventRepository> logger)
            : base(options?.Value.ConnectionString, EventEntityName, logger)
        {
        }

        /// <summary>
        /// Get all events of a team.
        /// </summary>
        /// <param name="teamId">The team Id of which events to be fetched.</param>
        /// <returns>A task that represents a collection of events.</returns>
        public async Task<IEnumerable<EventEntity>> GetEventsAsync(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                throw new ArgumentException("The team Id should have a valid value", nameof(teamId));
            }

            await this.EnsureInitializedAsync();
            return await this.GetAllAsync(teamId);
        }

        /// <summary>
        /// Get event details
        /// </summary>
        /// <param name="eventId">Event Id for a event.</param>
        /// <param name="teamId">The team Id of which events needs to be fetched</param>
        /// <returns>A collection of events</returns>
        public async Task<EventEntity> GetEventDetailsAsync(string eventId, string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                throw new ArgumentException("The team Id should have a valid value", nameof(teamId));
            }

            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("The event Id should have a valid value", nameof(eventId));
            }

            await this.EnsureInitializedAsync();
            return await this.GetAsync(teamId, eventId);
        }

        /// <summary>
        /// This method inserts a new event in Azure Table Storage if it is not already exists. Else updates the existing one.
        /// </summary>
        /// <param name="eventDetails">The details of an event that needs to be created or updated</param>
        /// <returns>Returns true if event created or updated successfully. Else, returns false.</returns>
        public async Task<bool> UpsertEventAsync(EventEntity eventDetails)
        {
            if (eventDetails == null)
            {
                throw new ArgumentException("The event details should be provided", nameof(eventDetails));
            }

            await this.EnsureInitializedAsync();
            return await this.CreateOrUpdateAsync(eventDetails);
        }

        /// <summary>
        /// Deletes a draft event
        /// </summary>
        /// <param name="eventEntity">Event object which needs to be deleted.</param>
        /// <returns>Returns true if an event has deleted successfully. Else returns false.</returns>
        public async Task<bool> DeleteEventAsync(EventEntity eventEntity)
        {
            eventEntity = eventEntity ?? throw new ArgumentException("The draft event cannot be empty or null", nameof(eventEntity));

            await this.EnsureInitializedAsync();
            return await this.DeleteAsync(eventEntity);
        }

        /// <summary>
        /// Gets event details
        /// </summary>
        /// <param name="eventId">The event Id of which details need to be retrieved</param>
        /// <param name="teamId">The logged-in user's team Id</param>
        /// <returns>Returns event details</returns>
        public async Task<EventEntity> GetEventAsync(string eventId, string teamId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("The event Id must be provided in order to get event details");
            }

            await this.EnsureInitializedAsync();
            return await this.GetAsync(teamId, eventId);
        }
    }
}