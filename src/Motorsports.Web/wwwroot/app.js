window.msSetCookie = (name, value) => {
    document.cookie = name + '=' + value + ';path=/;max-age=31536000;samesite=lax';
    if (name === 'theme') {
        document.documentElement.dataset.theme = value;
    }
};
window.msTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;
window.msLogout = () => {
    document.getElementById('logout-form').submit();
};
