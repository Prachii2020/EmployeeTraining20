// <copyright file="IUserGraphHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.EmployeeTraining.Helpers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Provides helper methods to make Microsoft Graph API calls related to users
    /// </summary>
    public interface IUserGraphHelper
    {
        /// <summary>
        /// Get top 100 recent collaborators for a user.
        /// </summary>
        /// <returns>List of users.</returns>
        Task<IEnumerable<Person>> GetRecentCollaboratorsAsync();

        /// <summary>
        /// Get user display name.
        /// </summary>
        /// <param name="userObjectId">AAD Object id of user.</param>
        /// <returns>A task that returns user information.</returns>
        Task<User> GetUserAsync(string userObjectId);

        /// <summary>
        /// Get users information from graph API.
        /// </summary>
        /// <param name="userObjectIds">Collection of AAD Object ids of users.</param>
        /// <returns>A task that returns collection of user information.</returns>
        Task<IEnumerable<User>> GetUsersAsync(IEnumerable<string> userObjectIds);

        /// <summary>
        /// Get top 10 users according to user search query.
        /// </summary>
        /// <param name="searchText">Search query entered by user.</param>
        /// <returns>List of users.</returns>
        Task<List<User>> SearchUsersAsync(string searchText);
    }
}
