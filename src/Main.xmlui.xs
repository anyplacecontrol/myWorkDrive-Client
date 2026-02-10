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
				drivePath: ":sh:" + share.shareName + ":/",
			};
		});
}

var hasRedirected = false;

function handleDriveOrSharesChange({ prevValue, newValue }) {
	if (!newValue || newValue.length !== 2) return;

	const drive = newValue[0];
	const shares = newValue[1];

	if (drive) {
		hasRedirected = false;
		return;
	}

	if (!shares || !shares.length) return;
	if (hasRedirected) return;

	hasRedirected = true;
	const targetUrl = window.MwdHelpers.buildNavigationUrl(shares[0].drivePath);
	if (targetUrl) Actions.navigate(targetUrl);
}

