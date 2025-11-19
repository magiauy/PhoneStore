const COOKIE_NAME = 'guzone_user_session';

function getExpiryDate(days) {
    const date = new Date();
    date.setTime(date.getTime() + days * 24 * 60 * 60 * 1000);
    return date.toUTCString();
}

export function setUserCookie(payload, days = 1) {
    if (!payload) {
        return;
    }

    const expires = getExpiryDate(days);
    document.cookie = `${COOKIE_NAME}=${encodeURIComponent(payload)}; expires=${expires}; path=/; SameSite=Lax`;
}

export function getUserCookie() {
    const name = `${COOKIE_NAME}=`;
    const decodedCookie = decodeURIComponent(document.cookie ?? '');
    const parts = decodedCookie.split(';');
    for (const part of parts) {
        const trimmed = part.trim();
        if (trimmed.startsWith(name)) {
            return trimmed.substring(name.length);
        }
    }

    return null;
}

export function clearUserCookie() {
    document.cookie = `${COOKIE_NAME}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; SameSite=Lax`;
}
