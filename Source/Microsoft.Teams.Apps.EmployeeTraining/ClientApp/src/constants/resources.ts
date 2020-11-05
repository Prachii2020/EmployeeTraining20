import { EventAudience } from "../models/event-audience";
import { EventType } from "../models/event-type";
import { IPostType } from "../models/IPostType";
import { SortBy } from "../models/sort-by";

export interface IConstantDropdownItem {
	name: string;
	id: number;
}

export default class Resources {
	public static readonly dark: string = "dark";
	public static readonly contrast: string = "contrast";
	public static readonly eventNameMaxLength: number = 100;
	public static readonly eventDescriptionMaxLength: number = 1000;
	public static readonly eventVenueMaxLength: number = 200;
	public static readonly userEventsMobileFilteredCategoriesLocalStorageKey: string = "user-events-filtered-categories";
	public static readonly userEventsMobileFilteredUsersLocalStorageKey: string = "user-events-filtered-users";
	public static readonly userEventsMobileSortByFilterLocalStorageKey: string = "user-events-sortby";
	public static readonly validUrlRegExp: RegExp = /^http(s)?:\/\/(www\.)?[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$/;

	public static readonly audienceType: Array<IConstantDropdownItem> = [
		{ name: "Public", id: EventAudience.Public },
		{ name: "Private", id: EventAudience.Private },
	];

	public static readonly sortBy: Array<IPostType> = [
		{ name: "Newest", id: SortBy.Recent, color: "" },
		{ name: "Popularity", id: SortBy.Popularity, color: "" }
	];

	public static readonly eventType: Array<IConstantDropdownItem> = [
		{ name: "In person", id: EventType.InPerson },
		{ name: "Teams", id: EventType.Teams },
		{ name: "Live event", id: EventType.LiveEvent },
	];
}