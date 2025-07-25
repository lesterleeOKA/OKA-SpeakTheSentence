mergeInto(LibraryManager.library, {
    SetWebPageTitle: function (titlePtr) {
        var title = UTF8ToString(titlePtr);
        document.title = title;
    }
});

mergeInto(LibraryManager.library, {
    GetDeviceType: function() {
        var ua = navigator.userAgent || navigator.vendor || window.opera;
        // Portable: iPad, iPhone, iPod, Android, generic tablet/phone
        if (
            /iPad|iPhone|iPod|Android|Mobile|Tablet/.test(ua) ||
            // iPadOS 13+ pretends to be a Mac, but has touch support
            (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
        ) {
            return 1; // Portable device
        }
        // Desktop: Windows or Mac
        if (/Windows NT/.test(ua) || /Macintosh/.test(ua)) {
            return 2; // Windows or MacBook
        }
        return 0; // Other
    }
});