function transformShares(shares) {
	if (!shares || !Array.isArray(shares)) return [];

	return shares
		.filter((share) => {
			return !!(share && share.webClientEnabled && share.shareName);
		})
		.map((share) => {
			return {
				shareName: share.shareName,
				driveLetter: share.driveLetter || "",
				downloadEnabled: !!share.downloadEnabled,
				desktopClientEnabled: !!share.desktopClientEnabled,
				webClientEnabled: !!share.webClientEnabled,
				drivePath: ":sh:" + share.shareName + ":/",
			};
		});
}

function handleSharesLoaded(data, isRefetch) {
	if (isRefetch || hasRedirected) return;
	if (!data || !data.length) return;

	const drive = getCurrentDrive();
	if (drive) return;

	const first = data[0];
	if (!first || !first.drivePath) return;

	hasRedirected = true;
	const targetUrl = window.MwdHelpers.buildNavigationUrl(first.drivePath);
	if (targetUrl) Actions.navigate(targetUrl);
}
