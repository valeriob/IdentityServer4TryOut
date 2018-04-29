declare module server {
	/** [TypeScriptModule("Server")] */
	interface PagingViewModel {
		Page: number;
		PageSize: number;
		TotalItems: number;
		OrderBy: string;
		OrderByDescending: boolean;
	}
}
