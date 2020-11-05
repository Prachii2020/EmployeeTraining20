// <copyright file="IEvent.ts" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

import { EventStatus } from "../models/event-status";
import { EventAudience } from "../models/event-audience";

export interface IEvent {
    eventId: string,
    teamId: string,
    name: string,
    description: string,
    startDate: Date,
    venue: string,
    categoryId: string,
    registeredAttendeesCount: number,
    maximumNumberOfParticipants: number,
    status: EventStatus,
    audience?: EventAudience,
    startTime?: Date,
    endTime?: Date,
    graphEventId: string,
    photo: string,
    numberOfOccurrences: number,
    isAutoRegister: boolean,
    type: number,
    meetingLink: string,
    createdOn: Date,
    createdBy: string,
    isRegistrationClosed: boolean,
    isMandatoryForLoggedInUser?: boolean,
    isLoggedInUserRegistered?: boolean,
    endDate: Date,
    categoryName: string,
    mandatoryAttendees: string,
    optionalAttendees: string,
    registeredAttendees: string,
    updatedOn?: Date,
    selectedUserOrGroupListJSON?: string,
    autoRegisteredAttendees: string,
    canLoggedInUserRegister?: boolean
}