// <copyright file="IEventRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.EmployeeTraining.Repositories
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.EmployeeTraining.Models;

    /// <summary>
    /// Interface for event repository which helps in retrieving, storing and updating event details.
    /// </summary>
    public interface IEventRepository
    {
        /// <summary>
        /// Get all events of a team.
        /// </summary>
        /// <param name="teamId">The team Id of which events to be fetched.</param>
        /// <returns>A task that represents a collection of events.</returns>
        Task<IEnumerable<EventEntity>> GetEventsAsync(string teamId);

        /// <summary>
        /// Get event details
        /// </summary>
        /// <param name="eventId">Event Id for a event.</param>
        /// <param name="teamId">The team Id of which events needs to be fetched</param>
        /// <returns>A collection of events</returns>
        Task<EventEntity> GetEventDetailsAsync(string eventId, string teamId);

        /// <summary>
        /// Create or update an event
        /// </summary>
        /// <param name="eventDetails">The details of an event that need to be created or updated</param>
        /// <returns>Returns true if an event has created or updated successfully. Else returns false.</returns>
        Task<bool> UpsertEventAsync(EventEntity eventDetails);

        /// <summary>
        /// Deletes an event
        /// </summary>
        /// <param name="eventEntity">Event object which needs to be deleted.</param>
        /// <returns>Returns true if an event has deleted successfully. Else returns false.</returns>
        Task<bool> DeleteEventAsync(EventEntity eventEntity);
    }
}